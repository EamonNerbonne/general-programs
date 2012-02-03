// ReSharper disable ParameterTypeCanBeEnumerable.Local
// ReSharper disable MemberCanBePrivate.Global
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using EmnExtensions;
using EmnExtensions.Text;
using LvqLibCli;

namespace LvqGui {
	public sealed class LrOptimizationResult {
		public readonly FileInfo resultsFile;
		public readonly double trainedIterations;
		public readonly LvqModelSettingsCli unoptimizedSettings;
		LrOptimizationResult(FileInfo fi, double iters, LvqModelSettingsCli settings) { unoptimizedSettings = settings; trainedIterations = iters; resultsFile = fi; }

		public LvqModelSettingsCli GetOptimizedSettings(uint? paramSeed = null, uint? instSeed = null) {
			IEnumerable<LrAndError> lrs = GetLrs();
			var bestLr = lrs.OrderBy(resVal => resVal.Errors.CanonicalError).First();
			return ConvertLrToSettings(bestLr, paramSeed, instSeed);
		}

		public LvqModelSettingsCli ConvertLrToSettings(LrAndError bestLr, uint? paramSeed = null, uint? instSeed = null) {
			LvqModelSettingsCli retval = unoptimizedSettings;
			retval.LR0 = bestLr.LR.Lr0;
			retval.LrScaleP = bestLr.LR.LrP;
			retval.LrScaleB = bestLr.LR.LrB;
			retval.ParamsSeed = paramSeed ?? retval.ParamsSeed;
			retval.InstanceSeed = instSeed ?? retval.InstanceSeed;
			return retval;
		}

		public IEnumerable<LrAndError> GetLrs() {
			string[] fileLines = File.ReadAllLines(resultsFile.FullName);
			if (fileLines.Length < 2) return Enumerable.Empty<LrAndError>();
			double[] lr0range = ExtractLrs(fileLines.First(line => line.StartsWith("lr0range:")));
			double[] lrPrange = ExtractLrs(fileLines.First(line => line.StartsWith("lrPrange:")));
			double[] lrBrange = ExtractLrs(fileLines.First(line => line.StartsWith("lrBrange:")));
			string[] resultLines = fileLines.SkipWhile(line => !line.StartsWith(".")).Skip(1).Where(line => !line.StartsWith("Search Complete!") && !string.IsNullOrWhiteSpace(line)).ToArray();
			return resultLines.Select(resLine => ParseLine(resLine, lr0range, lrPrange, lrBrange));
		}


		static LrAndError ParseLine(string resultLine, double[] lr0range, double[] lrPrange, double[] lrBrange) {
			var resLrThenErr = resultLine.Split(':');
			double[] lrs = resLrThenErr[0].Split('p', 'b').Select(double.Parse).ToArray();

			var errsThenCumulLr0 = resLrThenErr[1].Split(';');

			Tuple<double, double>[] errs = errsThenCumulLr0.Take(3).Select(errStr => errStr.Split('~').Select(double.Parse).ToArray()).Select(errval => Tuple.Create(errval[0], errval.Skip(1).FirstOrDefault())).ToArray();
			return new LrAndError {
				LR = new LearningRates {
					Lr0 = ClosestMatch(lr0range, lrs[0]),
					LrP = ClosestMatch(lrPrange, lrs[1]),
					LrB = ClosestMatch(lrBrange, lrs[2]),
				},
				Errors = new ErrorRates(errs[0].Item1, errs[0].Item2, errs[1].Item1, errs[1].Item2, errs[2].Item1, errs[2].Item2, double.Parse(errsThenCumulLr0[3].Trim(' ', '[', ']'))),
			};
		}

		static readonly char[] comma = new[] { ',' };
		static double[] ExtractLrs(string line) {
			return line.SubstringAfterFirst("{").SubstringUntil("}").Split(comma).Select(double.Parse).ToArray();
		}
		static double ClosestMatch(IEnumerable<double> haystack, double needle) {
			return haystack.Aggregate(new { Err = double.PositiveInfinity, Val = needle },
				(best, option) => Math.Abs(option - needle) < best.Err ? new { Err = Math.Abs(option - needle), Val = option } : best).Val;
		}

		static readonly Regex resultsFilenameRegex = new Regex(@"^(?<iters>[0-9]?e[0-9]+)\-(?<shorthand>[^ ]*?)\.txt$");

		public static IEnumerable<LrOptimizationResult> FromDataset(LvqDatasetCli dataset, string settingsStr) {
			return
				from creator in MaybeGetCreator(dataset)
				from parsedResults in FromDataset(creator, settingsStr)
				select parsedResults;
		}

		public static IEnumerable<LrOptimizationResult> FromDataset(IDatasetCreator dataset, string settingsStr=null) {
			return
				from datasetResultsDir in GetDatasetResultDir(dataset)
				from resultFile in datasetResultsDir.GetFiles("*" + (settingsStr==null?"":settingsStr+"*") + ".txt")
				where resultFile.Length > 0
				let parsedResults = ProcFile(resultFile)
				where parsedResults != null
				select parsedResults;
		}


		public static LrOptimizationResult ProcFile(FileInfo resultFile) {
			if (resultFile.Length == 0) return null;
			var itersAndSettings = ExtractItersAndSettings(resultFile.Name);
			if (!itersAndSettings.Item1) return null;
			return new LrOptimizationResult(resultFile, itersAndSettings.Item2, itersAndSettings.Item3);
		}

		public static Tuple<bool, double, LvqModelSettingsCli> ExtractItersAndSettings(string filename) {
			var match = resultsFilenameRegex.Match(filename);
			if (!match.Success) return Tuple.Create(false, default(double), default(LvqModelSettingsCli));
			double iters = double.Parse(match.Groups["iters"].Value.StartsWith("e") ? "1" + match.Groups["iters"].Value : match.Groups["iters"].Value);
			string shorthand = match.Groups["shorthand"].Value;
			LvqModelSettingsCli modelSettings = CreateLvqModelValues.ParseShorthand(shorthand);
			return Tuple.Create(true, iters, modelSettings);
		}

		/// <summary>
		/// Gets the lr-optimized result for the given dataset and settings with the largest number of iterations, or null if no results have been done for this settings+dataset combination.
		/// </summary>
		public static LrOptimizationResult GetBestResult(LvqDatasetCli dataset, LvqModelSettingsCli settings) {
			var lrIgnoredSettings = settings.WithDefaultLr().WithDefaultSeeds().Canonicalize();

			var matchingFiles =
				from result in FromDataset(dataset, lrIgnoredSettings.ToShorthand())
				where result.unoptimizedSettings.WithDefaultLr().WithDefaultSeeds().Canonicalize() == lrIgnoredSettings
				orderby result.trainedIterations descending, dataset.DatasetLabel == result.resultsFile.Directory.Name descending
				select result;

			return matchingFiles.FirstOrDefault();
		}

		static IEnumerable<IDatasetCreator> MaybeGetCreator(LvqDatasetCli dataset) {
			if (dataset == null) return Enumerable.Empty<IDatasetCreator>();
			return Enumerable.Repeat(CreateDataset.CreateFactory(dataset.DatasetLabel),1);
		}
		static IEnumerable<DirectoryInfo> GetDatasetResultDir(IDatasetCreator basicExample) {
			return from dir in LrOptimizer.resultsDir.GetDirectories()
				   where basicExample != null
				   let dirSplitName = CreateDataset.CreateFactory(dir.Name)
				   where dirSplitName != null && dirSplitName.GetType() == basicExample.GetType() && basicExample.LrTrainingShorthand() == dirSplitName.LrTrainingShorthand()
				   orderby dirSplitName.HasTestfile() == basicExample.HasTestfile() descending
				   , dirSplitName.Folds == basicExample.Folds descending
				   , dirSplitName.InstanceSeed == basicExample.InstanceSeed descending
				   select dir;
		}
	}
}