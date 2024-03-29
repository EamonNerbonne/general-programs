// ReSharper disable ParameterTypeCanBeEnumerable.Local
// ReSharper disable MemberCanBePrivate.Global

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EmnExtensions.Text;
using LvqGui.CreatorGui;
using LvqLibCli;

namespace LvqGui
{
    public sealed class LrOptimizationResult
    {
        public readonly FileInfo resultsFile;
        public readonly double trainedIterations;
        public readonly LvqModelSettingsCli unoptimizedSettings;

        LrOptimizationResult(FileInfo fi, double iters, LvqModelSettingsCli settings)
        {
            unoptimizedSettings = settings;
            trainedIterations = iters;
            resultsFile = fi;
        }

        public LvqModelSettingsCli GetOptimizedSettings(uint? paramSeed = null, uint? instSeed = null)
        {
            var lrs = GetLrs();
            var bestLr = lrs.OrderBy(resVal => resVal.Errors.CanonicalError).First();
            return ConvertLrToSettings(bestLr, paramSeed, instSeed);
        }

        public LvqModelSettingsCli ConvertLrToSettings(LrAndError bestLr, uint? paramSeed = null, uint? instSeed = null)
        {
            var retval = unoptimizedSettings;
            retval.LR0 = bestLr.LR.Lr0;
            retval.LrScaleP = bestLr.LR.LrP;
            retval.LrScaleB = bestLr.LR.LrB;
            retval.ParamsSeed = paramSeed ?? retval.ParamsSeed;
            retval.InstanceSeed = instSeed ?? retval.InstanceSeed;
            return retval;
        }

        public IEnumerable<LrAndError> GetLrs()
        {
            var fileLines = File.ReadAllLines(resultsFile.FullName);
            if (fileLines.Length < 2) {
                return Enumerable.Empty<LrAndError>();
            }

            var lr0range = ExtractLrs(fileLines.First(line => line.StartsWith("lr0range:", StringComparison.Ordinal)));
            var lrPrange = ExtractLrs(fileLines.First(line => line.StartsWith("lrPrange:", StringComparison.Ordinal)));
            var lrBrange = ExtractLrs(fileLines.First(line => line.StartsWith("lrBrange:", StringComparison.Ordinal)));
            var resultLines = fileLines.SkipWhile(line => !line.StartsWith(".", StringComparison.Ordinal)).Skip(1).Where(line => !line.StartsWith("Search Complete!", StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(line)).ToArray();
            return resultLines.Select(resLine => LrAndError.ParseLine(resLine, lr0range, lrPrange, lrBrange));
        }

        static readonly char[] comma = { ',' };

        static double[] ExtractLrs(string line)
            => line.SubstringAfterFirst("{").SubstringUntil("}").Split(comma).Select(double.Parse).ToArray();

        public static IEnumerable<LrOptimizationResult> FromDataset(LvqDatasetCli dataset, string settingsStr)
            =>
                from creator in MaybeGetCreator(dataset)
                from parsedResults in FromDataset(creator, settingsStr)
                select parsedResults;

        public static IEnumerable<LrOptimizationResult> FromDataset(IDatasetCreator dataset, string settingsStr = null)
            =>
                from datasetResultsDir in GetDatasetResultDir(dataset)
                from resultFile in datasetResultsDir.GetFiles("*" + (settingsStr == null ? "" : settingsStr + "*") + ".txt")
                where resultFile.Length > 0
                let parsedResults = ProcFile(resultFile)
                where parsedResults != null
                select parsedResults;

        public static LrOptimizationResult ProcFile(FileInfo resultFile)
        {
            if (resultFile.Length == 0) {
                return null;
            }

            var itersAndSettings = LrGuesser.ExtractItersAndSettings(resultFile.Name);
            if (!itersAndSettings.Item1) {
                return null;
            }

            return new(resultFile, itersAndSettings.Item2, itersAndSettings.Item3);
        }

        /// <summary>
        /// Gets the lr-optimized result for the given dataset and settings with the largest number of iterations, or null if no results have been done for this settings+dataset combination.
        /// </summary>
        public static LrOptimizationResult GetBestResult(LvqDatasetCli dataset, LvqModelSettingsCli settings)
        {
            var lrIgnoredSettings = settings.WithCanonicalizedDefaults();

            var matchingFiles =
                from result in FromDataset(dataset, lrIgnoredSettings.ToShorthand())
                where result.unoptimizedSettings.WithCanonicalizedDefaults() == lrIgnoredSettings
                orderby result.trainedIterations descending, dataset.DatasetLabel == result.resultsFile.Directory.Name descending
                select result;

            return matchingFiles.FirstOrDefault();
        }

        static IEnumerable<IDatasetCreator> MaybeGetCreator(LvqDatasetCli dataset)
        {
            if (dataset == null) {
                return Enumerable.Empty<IDatasetCreator>();
            }

            return Enumerable.Repeat(CreateDataset.CreateFactory(dataset.DatasetLabel), 1);
        }

        static IEnumerable<DirectoryInfo> GetDatasetResultDir(IDatasetCreator basicExample)
            =>
                from dir in LrGuesser.resultsDir.GetDirectories()
                where basicExample != null
                let dirSplitName = CreateDataset.CreateFactory(dir.Name)
                where dirSplitName != null && dirSplitName.GetType() == basicExample.GetType() && basicExample.LrTrainingShorthand() == dirSplitName.LrTrainingShorthand()
                orderby dirSplitName.HasTestfile() == basicExample.HasTestfile() descending
                    , dirSplitName.Folds == basicExample.Folds descending
                    , dirSplitName.InstanceSeed == basicExample.InstanceSeed descending
                select dir;
    }
}
