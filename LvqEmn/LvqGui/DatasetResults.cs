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

		public LvqModelSettingsCli GetOptimizedSettings() {
			string[] fileLines = File.ReadAllLines(resultsFile.FullName);
			double[] lr0range = ExtractLrs(fileLines.First(line => line.StartsWith("lr0range:")));
			double[] lrPrange = ExtractLrs(fileLines.First(line => line.StartsWith("lrPrange:")));
			double[] lrBrange = ExtractLrs(fileLines.First(line => line.StartsWith("lrBrange:")));
			string[] resultLines = fileLines.SkipWhile(line => !line.StartsWith(".")).Skip(1).TakeWhile(line => !line.StartsWith("Search Complete!")).ToArray();
			double[] lrOfBestResult = resultLines.First().SubstringBefore(":").Split('p', 'b').Select(double.Parse).ToArray();

			double lr0 = ClosestMatch(lr0range, lrOfBestResult[0]);
			double lrp = ClosestMatch(lrPrange, lrOfBestResult[1]);
			double lrb = ClosestMatch(lrBrange, lrOfBestResult[2]);
			LvqModelSettingsCli retval = unoptimizedSettings.Copy();
			retval.LR0 = lr0;
			retval.LrScaleP = lrp;
			retval.LrScaleB = lrb;
			return retval;
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
			DirectoryInfo datasetResultsDir = GetDatasetResultDir(dataset);
			if (datasetResultsDir == null) return Enumerable.Empty<DatasetResults>();
			return
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
				orderby result.trainedIterations descending
				select result;

			return matchingFiles.FirstOrDefault();
		}

		/// <summary>
		/// Gets the set of dataset lr-optimized results for the given dataset and settings with the largest number of iterations, or null if not all modeltype/prototype combos are done.
		/// </summary>
		public static IEnumerable<DatasetResults> GetBestResults(LvqDatasetCli dataset, LvqModelSettingsCli settings) {
			var modelIgnoredSettings = WithoutModelAndPrototypes(WithoutLrOrSeeds(settings));

			var matchingFiles =
				from result in FromDataset(dataset)
				where WithoutModelAndPrototypes(WithoutLrOrSeeds(result.unoptimizedSettings)).ToShorthand() == modelIgnoredSettings.ToShorthand()
				group result by result.trainedIterations into resGroup
				where resGroup.Select(res => new { res.unoptimizedSettings.ModelType, res.unoptimizedSettings.PrototypesPerClass })
												.SetEquals(TestLr.ModelTypes.SelectMany(mt => TestLr.PrototypesPerClassOpts.Select(ppc => new { ModelType = mt, PrototypesPerClass = ppc })))
				orderby resGroup.Key descending
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
		static LvqModelSettingsCli WithoutModelAndPrototypes(LvqModelSettingsCli p_settings) {
			var retval = p_settings.Copy();
			retval.ModelType = LvqModelType.Gm;
			retval.PrototypesPerClass = 0;
			return retval;
		}

		static DirectoryInfo GetDatasetResultDir(LvqDatasetCli dataset) {
			if (dataset == null) return null;
			return TestLr.resultsDir.GetDirectories(dataset.DatasetLabel).FirstOrDefault();
		}
	}
}