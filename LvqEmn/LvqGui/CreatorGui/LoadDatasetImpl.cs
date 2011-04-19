using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using EmnExtensions.Filesystem;
using System.Globalization;

//using LvqFloat = System.Single;
using LvqFloat = System.Double;

namespace LvqGui {
	public static class LoadDatasetImpl {
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

		public static Tuple<LvqFloat[,], int[], int> LoadDataset(FileInfo datafile, FileInfo labelfile) {
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

		public static Tuple<LvqFloat[,], int[], int> LoadDataset(FileInfo dataAndLabelFile) {
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
