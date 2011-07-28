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
	public sealed class DatasetResults {
		public readonly FileInfo resultsFile;
		public readonly double trainedIterations;
		public readonly LvqModelSettingsCli unoptimizedSettings;
		DatasetResults(FileInfo fi, double iters, LvqModelSettingsCli settings) { unoptimizedSettings = settings; trainedIterations = iters; resultsFile = fi; }

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

		public struct Lr { public double Lr0, LrP, LrB; public override string ToString() { return Lr0 + " p" + LrP + " b" + LrB; } }
		public struct LrAndError : IComparable<LrAndError>, IComparable {
			public Lr LR; public TestLr.ErrorRates Errors;
			public int CompareTo(LrAndError other) { return Errors.CompareTo(other.Errors); }
			public override string ToString() { return LR + " @ " + Errors; }

			public int CompareTo(object obj) { return CompareTo((LrAndError)obj); }
		}

		static LrAndError ParseLine(string resultLine, double[] lr0range, double[] lrPrange, double[] lrBrange) {
			var resLrThenErr = resultLine.Split(':');
			double[] lrs = resLrThenErr[0].Split('p', 'b').Select(double.Parse).ToArray();

			var errsThenCumulLr0 = resLrThenErr[1].Split(';');

			Tuple<double, double>[] errs = errsThenCumulLr0.Take(3).Select(errStr => errStr.Split('~').Select(double.Parse).ToArray()).Select(errval => Tuple.Create(errval[0], errval.Skip(1).FirstOrDefault())).ToArray();
			return new LrAndError {
				LR = new Lr {
					Lr0 = ClosestMatch(lr0range, lrs[0]),
					LrP = ClosestMatch(lrPrange, lrs[1]),
					LrB = ClosestMatch(lrBrange, lrs[2]),
				},
				Errors = new TestLr.ErrorRates(errs[0].Item1, errs[0].Item2, errs[1].Item1, errs[1].Item2, errs[2].Item1, errs[2].Item2, double.Parse(errsThenCumulLr0[3].Trim(' ', '[', ']'))),
			};
		}

		static readonly char[] comma = new[] { ',' };
		static double[] ExtractLrs(string line) {
			return line.SubstringAfter("{").SubstringBefore("}").Split(comma).Select(double.Parse).ToArray();
		}
		static double ClosestMatch(IEnumerable<double> haystack, double needle) {
			return haystack.Aggregate(new { Err = double.PositiveInfinity, Val = needle },
				(best, option) => Math.Abs(option - needle) < best.Err ? new { Err = Math.Abs(option - needle), Val = option } : best).Val;
		}

		static readonly Regex resultsFilenameRegex = new Regex(@"^(?<iters>[0-9]?e[0-9])+\-(?<shorthand>[^ ]*?)\.txt$");

		public static IEnumerable<DatasetResults> FromDataset(LvqDatasetCli dataset) {
			return
				from datasetResultsDir in GetDatasetResultDir(dataset)
				from resultFile in datasetResultsDir.GetFiles("*.txt")
				where resultFile.Length > 0
				let parsedResults = ProcFile(resultFile)
				where parsedResults != null
				select parsedResults;
		}

		public static DatasetResults ProcFile(FileInfo resultFile) {
			var match = resultsFilenameRegex.Match(resultFile.Name);
			if (!match.Success || resultFile.Length == 0) return null;

			return new DatasetResults(resultFile, Double.Parse(match.Groups["iters"].Value.StartsWith("e") ? "1" + match.Groups["iters"].Value : match.Groups["iters"].Value),
				CreateLvqModelValues.ParseShorthand(match.Groups["shorthand"].Value));
		}

		/// <summary>
		/// Gets the lr-optimized result for the given dataset and settings with the largest number of iterations, or null if no results have been done for this settings+dataset combination.
		/// </summary>
		public static DatasetResults GetBestResult(LvqDatasetCli dataset, LvqModelSettingsCli settings) {
			var lrIgnoredSettings = WithoutLrOrSeeds(settings);

			var matchingFiles =
				from result in FromDataset(dataset)
				where WithoutLrOrSeeds(result.unoptimizedSettings).ToShorthand() == lrIgnoredSettings.ToShorthand()
				orderby result.trainedIterations descending, dataset.DatasetLabel == result.resultsFile.Directory.Name descending
				select result;

			return matchingFiles.FirstOrDefault();
		}

		/// <summary>
		/// Gets the set of dataset lr-optimized results for the given dataset and settings with the largest number of iterations, or null if not all modeltype/prototype combos are done.
		/// </summary>
		public static DatasetResults[] GetBestResults(LvqDatasetCli dataset, LvqModelSettingsCli settings) {
			var settingsNoLr = WithoutLrOrSeeds(settings);

			var matchingFiles =
				from result in FromDataset(dataset)
				where WithoutLrOrSeeds(result.unoptimizedSettings).ToShorthand() ==
				 WithModelAndPrototypes(settingsNoLr, result.unoptimizedSettings.ModelType, result.unoptimizedSettings.PrototypesPerClass).ToShorthand()
				group result by Tuple.Create(result.trainedIterations, result.resultsFile.Directory.Name) into resGroup
				where resGroup.Select(res => new { res.unoptimizedSettings.ModelType, res.unoptimizedSettings.PrototypesPerClass })
						.ContainsAll(TestLr.ModelTypes.SelectMany(mt => TestLr.PrototypesPerClassOpts.Select(ppc => new { ModelType = mt, PrototypesPerClass = ppc })))
				orderby resGroup.Key.Item1 descending, resGroup.Key.Item2 == dataset.DatasetLabel descending
				select resGroup.ToArray();

			return matchingFiles.FirstOrDefault();
		}

		public static LvqModelSettingsCli WithoutLrOrSeeds(LvqModelSettingsCli p_settings) {
			p_settings.LR0 = 0;
			p_settings.LrScaleB = 0;
			p_settings.LrScaleP = 0;
			p_settings.ParamsSeed = 0;
			p_settings.InstanceSeed = 0;
			return p_settings;
		}
		static LvqModelSettingsCli WithModelAndPrototypes(LvqModelSettingsCli p_settings, LvqModelType modelType, int protos) {
			p_settings.ModelType = modelType;
			p_settings.PrototypesPerClass = protos;
			return p_settings;
		}

		static IEnumerable<DirectoryInfo> GetDatasetResultDir(LvqDatasetCli dataset) {
			if (dataset == null) return Enumerable.Empty<DirectoryInfo>();
			var split = CreateDataset.CreateFactory(dataset.DatasetLabel);

			return from dir in TestLr.resultsDir.GetDirectories()
				   let dirSplitName = CreateDataset.CreateFactory(dir.Name)
				   where dirSplitName != null && dirSplitName.GetType() == split.GetType() && split.LrTrainingShorthand() == dirSplitName.LrTrainingShorthand()
				   orderby dirSplitName.HasTestfile() == split.HasTestfile() descending
				   , dirSplitName.Folds == split.Folds descending
				   , dirSplitName.InstanceSeed == split.InstanceSeed descending
				   select dir;
		}
	}
}