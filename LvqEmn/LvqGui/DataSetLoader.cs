using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using EmnExtensions.Filesystem;
using System.Globalization;

namespace LvqGui {
	public static class DatasetLoader {
		static readonly char[] dimSep = new[] { ',' };

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

		public static Tuple<double[,], int[], int> LoadDataset(FileInfo datafile, FileInfo labelfile) {
			var dataVectors =
				(from dataline in datafile.GetLines()
				 select (
					 from dataDim in dataline.Split(dimSep)
					 select double.Parse(dataDim, CultureInfo.InvariantCulture)
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
	}
}
