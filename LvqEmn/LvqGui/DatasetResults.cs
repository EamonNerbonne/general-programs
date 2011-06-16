using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using EmnExtensions.Algorithms;
using EmnExtensions.Text;
using LvqLibCli;

namespace LvqGui {
	public class DatasetResults {
		public readonly FileInfo resultsFile;
		public readonly double trainedIterations;
		public readonly LvqModelSettingsCli unoptimizedSettings;
		DatasetResults(FileInfo fi, double iters, LvqModelSettingsCli settings) { unoptimizedSettings = settings; trainedIterations = iters; resultsFile = fi; }

		public LvqModelSettingsCli GetOptimizedSettings(uint? paramSeed = null, uint? instSeed = null) {
			string[] fileLines = File.ReadAllLines(resultsFile.FullName);
			double[] lr0range = ExtractLrs(fileLines.First(line => line.StartsWith("lr0range:")));
			double[] lrPrange = ExtractLrs(fileLines.First(line => line.StartsWith("lrPrange:")));
			double[] lrBrange = ExtractLrs(fileLines.First(line => line.StartsWith("lrBrange:")));
			string[] resultLines = fileLines.SkipWhile(line => !line.StartsWith(".")).Skip(1).Where(line => !line.StartsWith("Search Complete!") && !string.IsNullOrWhiteSpace(line)).ToArray();
			var lr = resultLines.Select(resLine => ParseLine(resLine, lr0range, lrPrange, lrBrange)).OrderBy(resVal => resVal.Errors.training).First();
			LvqModelSettingsCli retval = unoptimizedSettings.Copy();
			retval.LR0 = lr.Lr0;
			retval.LrScaleP = lr.LrP;
			retval.LrScaleB = lr.LrB;
			retval.ParamsSeed = paramSeed ?? retval.ParamsSeed;
			retval.InstanceSeed = instSeed ?? retval.InstanceSeed;
			return retval;
		}
		struct Lrs { public double Lr0, LrP, LrB; public TestLr.ErrorRates Errors;}
		private static Lrs ParseLine(string resultLine, double[] lr0range, double[] lrPrange, double[] lrBrange) {
			var resLrThenErr = resultLine.Split(':');
			double[] lrs = resLrThenErr[0].Split('p', 'b').Select(double.Parse).ToArray();
			
			var errsThenCumulLr0 = resLrThenErr[1].Split(';');

			Tuple<double,double>[] errs = errsThenCumulLr0.Take(3).Select(errStr => errStr.Split('~').Select(double.Parse).ToArray() ).Select(errval=> Tuple.Create(errval[0],errval.Skip(1).FirstOrDefault())).ToArray();
			return new Lrs {
				Lr0 = ClosestMatch(lr0range, lrs[0]),
				LrP = ClosestMatch(lrPrange, lrs[1]),
				LrB = ClosestMatch(lrBrange, lrs[2]),
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

		static readonly Regex resultsFilenameRegex = new Regex(@"^(?<iters>[0-9]?e[0-9])+\-(?<shorthand>[^ ]*?)( \([0-9+]\))?\.txt$");

		static IEnumerable<DatasetResults> FromDataset(LvqDatasetCli dataset) {
			return
				from datasetResultsDir in GetDatasetResultDir(dataset)
				from resultFile in datasetResultsDir.GetFiles("*.txt")
				let parsedResults = ProcFile(resultFile)
				where parsedResults != null
				select parsedResults;
		}

		public static DatasetResults ProcFile(FileInfo resultFile) {
			var match = resultsFilenameRegex.Match(resultFile.Name);
			if (!match.Success) return null;

			return new DatasetResults(resultFile, Double.Parse(match.Groups["iters"].Value.StartsWith("e") ? "1" + match.Groups["iters"].Value : match.Groups["iters"].Value), CreateLvqModelValues.SettingsFromShorthand(match.Groups["shorthand"].Value));
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
				 WithModelAndPrototypes(settingsNoLr , result.unoptimizedSettings.ModelType, result.unoptimizedSettings.PrototypesPerClass).ToShorthand()
				group result by Tuple.Create(result.trainedIterations, result.resultsFile.Directory.Name) into resGroup
				where resGroup.Select(res => new { res.unoptimizedSettings.ModelType, res.unoptimizedSettings.PrototypesPerClass })
												.SetEquals(TestLr.ModelTypes.SelectMany(mt => TestLr.PrototypesPerClassOpts.Select(ppc => new { ModelType = mt, PrototypesPerClass = ppc })))
				orderby resGroup.Key.Item1 descending, resGroup.Key.Item2 == dataset.DatasetLabel descending
				select resGroup.ToArray();

			return matchingFiles.FirstOrDefault();
		}

		static LvqModelSettingsCli WithoutLrOrSeeds(LvqModelSettingsCli p_settings) {
			var retval = p_settings.Copy();
			retval.LR0 = 0;
			retval.LrScaleB = 0;
			retval.LrScaleP = 0;
			retval.ParamsSeed = 0;
			retval.InstanceSeed = 0;
			return retval;
		}
		static LvqModelSettingsCli WithModelAndPrototypes(LvqModelSettingsCli p_settings,LvqModelType modelType, int protos) {
			var retval = p_settings.Copy();
			retval.ModelType = modelType;
			retval.PrototypesPerClass = protos;
			return retval;
		}

		static readonly Regex labelRegex = new Regex(@"^(?<prefix>.*?[,\[])(?<instseed>[0-9A-Fa-f]+)(?<suffix>\].*)$");
		static Tuple<string, string, string> splitLabel(string label) {
			Match m = labelRegex.Match(label);
			if (!m.Success) return null;
			return Tuple.Create(m.Groups["prefix"].Value, m.Groups["instseed"].Value, m.Groups["suffix"].Value);
		}

		static IEnumerable<DirectoryInfo> GetDatasetResultDir(LvqDatasetCli dataset) {
			if (dataset == null) return Enumerable.Empty<DirectoryInfo>();
			var split = splitLabel(dataset.DatasetLabel);

			return TestLr.resultsDir.GetDirectories(split.Item1 + "*" + split.Item3)
				.Where(di => { var diSplit = splitLabel(di.Name); return diSplit.Item1 == split.Item1 && diSplit.Item3 == split.Item3; });
		}
	}
}