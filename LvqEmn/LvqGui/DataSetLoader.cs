using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using EmnExtensions.Filesystem;
using System.Globalization;
using System.Diagnostics;

namespace LVQeamon
{
	public static class DataSetLoader
	{
		static char[] dimSep = new[] { ',' };

		public static T[,] ToRectangularArray<T>(this T[][] jaggedArray)
		{
			int outerLen = jaggedArray.Length;
			Debug.Assert(outerLen > 0);
			int innerLen = jaggedArray[0].Length;

			T[,] retval = new T[outerLen, innerLen];
			for (int i = 0; i < outerLen; i++)
			{
				T[] row = jaggedArray[i];
				Debug.Assert(row.Length == innerLen);
				for (int j = 0; j < innerLen; j++)
					retval[i, j] = row[j];
			}
			return retval;
		}

		public static List<double[,]> LoadDataset(FileInfo datafile, FileInfo labelfile)
		{
			var dataVectors = 
				from dataline in datafile.GetLines()
				select (
					from dataDim in dataline.Split(dimSep)
					select double.Parse(dataDim, CultureInfo.InvariantCulture)
					).ToArray();

			int dimensionality = dataVectors.First().Length;

			var itemLabels = 
					from labelline in labelfile.GetLines()
					select int.Parse(labelline, CultureInfo.InvariantCulture);

			var dataVectorsByClass = 
			 	dataVectors
				.Zip(itemLabels, (vector, label) => new { Data = vector, Label = label })
				.ToLookup(tuple => tuple.Label, tuple => tuple.Data);

			return
				(from classVectors in dataVectorsByClass
				 orderby classVectors.Key //just to be nice
				 select classVectors.ToArray().ToRectangularArray()
				).ToList();
		}
	}
}
