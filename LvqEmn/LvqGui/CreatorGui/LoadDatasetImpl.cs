using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using EmnExtensions.Filesystem;
using EmnExtensions.MathHelpers;
using EmnExtensions.Wpf;
using LvqLibCli;
//using LvqFloat = System.Single;
using LvqFloat = System.Double;

namespace LvqGui {

	public class LoadedDatasetSettings : DatasetCreatorBase<LoadedDatasetSettings> {
		protected override string RegexText {
			get {
				return @"^
				(?<Filename>.*?)
				(\,(?<TestFilename>.*?))?
				\-(?<DimCount>[0-9]+)D
				(?<ExtendDataByCorrelation>x?)
				(?<NormalizeDimensions>n?)
				\-(?<ClassCount>[0-9]+)
				\,(?<PointCount>[0-9]+)
				(\[(?<InstanceSeed_>[0-9a-fA-F]+)\])?
				(\^(?<Folds>\d+))?\s*$";
			}
		}

		public string Filename, TestFilename;
		public int DimCount, ClassCount, PointCount;


		protected override string GetShorthand() {
			return Filename + (TestFilename != null ? "," + TestFilename : "") + "-" + DimCount + "D" + (ExtendDataByCorrelation ? "x" : "") + (NormalizeDimensions ? "n" : "") + "-"
				+ ClassCount + "," + PointCount
				+ (InstanceSeed == defaults.InstanceSeed ? "" : "[" + InstanceSeed.ToString("x") + "]")
				+ (Folds == defaults.Folds ? "" : "^" + Folds);
		}


		public override LvqDatasetCli CreateDataset() {
			var trainFile = LoadDatasetImpl.dataDir.GetFiles(Filename).FirstOrDefault();
			if (trainFile == null) return null;
			var testFile = TestFilename == null ? null : LoadDatasetImpl.dataDir.GetFiles(TestFilename).FirstOrDefault();
			if (testFile == null && TestFilename != null) return null;
			var trainSet = LoadDatasetImpl.LoadData(trainFile, this);
			trainSet.TestSet = testFile == null ? null : LoadDatasetImpl.LoadData(testFile, this);
			return trainSet;
		}
	}

	public static class LoadDatasetImpl {

		public static readonly DirectoryInfo dataDir = FSUtil.FindDataDir(new[] { @"data\datasets\", @"uni\Thesis\datasets\" }, typeof(LoadDatasetImpl));

		public static LvqDatasetCli Load(int folds, string name, uint rngInst) {
			var dataFile = dataDir.GetFiles(name + ".data").FirstOrDefault();
			return LoadData(dataFile, new LoadedDatasetSettings { InstanceSeed = rngInst, Folds = folds });
		}


		static readonly char[] dimSep = new[] { ',' };
		static readonly char[] spaceSep = new[] { ' ' };

		static T[,] ToRectangularArray<T>(this T[][] jaggedArray) {
			int outerLen = jaggedArray.Length;

			if (outerLen == 0)
				throw new FileFormatException("No data!");

			int innerLen = jaggedArray[0].Length;

			T[,] retval = new T[outerLen, innerLen];
			for (int i = 0; i < outerLen; i++) {
				T[] row = jaggedArray[i];
				if (row.Length != innerLen)
					throw new FileFormatException("Vectors are of inconsistent lengths");

				for (int j = 0; j < innerLen; j++)
					retval[i, j] = row[j];
			}
			return retval;
		}



		public static LvqDatasetCli LoadData(FileInfo dataFile, LoadedDatasetSettings settings) {
			settings = settings.Clone();
			settings.Filename = dataFile.Name;
			if (settings.Folds != 0 && settings.TestFilename != null)
				throw new ArgumentException("Cannot use n-fold crossvalidation and a separate test-set simultaneously");


			var labelFile = new FileInfo(dataFile.Directory + @"\" + Path.GetFileNameWithoutExtension(dataFile.Name) + ".label");
			var pointclouds = labelFile.Exists ? LoadDatasetHelper(dataFile, labelFile) : LoadDatasetHelper(dataFile);
			var pointArray = pointclouds.Item1;
			int[] labelArray = pointclouds.Item2;
			long colorSeedLong = labelArray.Select((label, i) => label * (long)(i + 1)).Sum();
			int colorSeed = (int)(colorSeedLong + (colorSeedLong >> 32));

			settings.DimCount = pointArray.GetLength(1);
			settings.ClassCount = pointclouds.Item3;
			settings.PointCount = pointArray.GetLength(0);


			return LvqDatasetCli.ConstructFromArray(
				rngInstSeed: settings.InstanceSeed,
				label: settings.Shorthand,
				extend: settings.ExtendDataByCorrelation,
				normalizeDims: settings.NormalizeDimensions,
				folds: settings.Folds,
				colors: WpfTools.MakeDistributedColors(settings.ClassCount, new MersenneTwister(colorSeed)),
				points: pointArray,
				pointLabels: labelArray,
				classCount: settings.ClassCount);
		}

		public static Tuple<LvqFloat[,], int[], int> LoadDatasetHelper(FileInfo datafile, FileInfo labelfile) {
			var dataVectors =
				(from dataline in datafile.GetLines()
				 select (
					 from dataDim in dataline.Split(dimSep)
					 select LvqFloat.Parse(dataDim, CultureInfo.InvariantCulture)
					 ).ToArray()
				).ToArray();

			var itemLabels = (
					from labelline in labelfile.GetLines()
					select int.Parse(labelline, CultureInfo.InvariantCulture)
					).ToArray();

			var denseLabelLookup =
				itemLabels
				.Distinct()
				.OrderBy(label => label)
				.Select((OldLabel, Index) => new { OldLabel, NewLabel = Index })
				.ToDictionary(a => a.OldLabel, a => a.NewLabel);

			itemLabels =
				itemLabels
				.Select(oldlabel => denseLabelLookup[oldlabel])
				.ToArray();

			var labelSet = new HashSet<int>(itemLabels);
			int minLabel = labelSet.Min();
			int maxLabel = labelSet.Max();
			int labelCount = labelSet.Count;
			if (labelCount != maxLabel + 1 || minLabel != 0)
				throw new FileFormatException("Class labels must be consecutive integers starting at 0");

			return Tuple.Create(dataVectors.ToRectangularArray(), itemLabels, labelCount);
		}

		public static Tuple<LvqFloat[,], int[], int> LoadDatasetHelper(FileInfo dataAndLabelFile) {
			bool commasplit = dataAndLabelFile.GetLines().Take(10).All(line => line.Contains(','));

			var splitLines =
				(from dataline in dataAndLabelFile.GetLines()
				 select commasplit ? dataline.Split(dimSep) : dataline.Split(spaceSep, StringSplitOptions.RemoveEmptyEntries));

			bool lastColClass = splitLines.Take(10).All(splitLine => Regex.IsMatch(splitLine[splitLine.Length - 1], @"^[a-zA-Z]\w*$"));
			bool firstColClass = !lastColClass && splitLines.Take(10).All(splitLine => Regex.IsMatch(splitLine[0], @"^[a-zA-Z]\w*$"));


			var labelledVectors =
				(from splitLine in splitLines
				 select new {
					 Label = splitLine[firstColClass ? 0 : splitLine.Length - 1],
					 Data = (
						 from dataDim in (firstColClass ? splitLine.Skip(1) : splitLine.Take(splitLine.Length - 1))
						 select LvqFloat.Parse(dataDim, CultureInfo.InvariantCulture)
						 ).ToArray()
				 }
				).ToArray();

			var itemLabels = (
					from labelline in labelledVectors
					select labelline.Label
					).ToArray();

			var dataVectors = (
					from labelline in labelledVectors
					select labelline.Data
					).ToArray();

			var denseLabelLookup =
				itemLabels
				.Distinct()
				.OrderBy(label => label)
				.Select((OldLabel, Index) => new { OldLabel, NewLabel = Index })
				.ToDictionary(a => a.OldLabel, a => a.NewLabel);

			var itemIntLabels =
				itemLabels
				.Select(oldlabel => denseLabelLookup[oldlabel])
				.ToArray();

			var labelSet = new HashSet<int>(itemIntLabels);
			int minLabel = labelSet.Min();
			int maxLabel = labelSet.Max();
			int labelCount = labelSet.Count;
			if (labelCount != maxLabel + 1 || minLabel != 0)
				throw new FileFormatException("Class labels must be consecutive integers starting at 0");

			return Tuple.Create(dataVectors.ToRectangularArray(), itemIntLabels, labelCount);
		}
	}
}
