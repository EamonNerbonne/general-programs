using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using EmnExtensions.Filesystem;
using EmnExtensions.MathHelpers;
using EmnExtensions.Wpf.WpfTools;
using LvqLibCli; //using LvqFloat = System.Single;

namespace LvqGui.CreatorGui
{
    public sealed class LoadedDatasetSettings : DatasetCreatorBase<LoadedDatasetSettings>
    {
        protected override string RegexText => @"^
                (?<Filename>.*?)
                (\,(?<TestFilename>.*?))?
                \-(?<DimCount>[0-9]+)D
                (?<ExtendDataByCorrelation>x?)
                (?<NormalizeDimensions>(?<NormalizeByScaling>S?)|n)
                \-(?<ClassCount>[0-9]+)
                \,(?<PointCount>[0-9]+)
                (\[(?<InstanceSeed_>[0-9a-fA-F]+)\])?
                (\^(?<Folds>\d+))?\s*$";

        public string Filename, TestFilename;
        public int DimCount, ClassCount, PointCount;


        protected override string GetShorthand() => Filename + (TestFilename != null ? "," + TestFilename : "") + "-" + DimCount + "D" + (ExtendDataByCorrelation ? "x" : "") + (!NormalizeDimensions ? "" : NormalizeByScaling ? "S" : "n") + "-"
            + ClassCount + "," + PointCount
            + (InstanceSeed == defaults.InstanceSeed ? "" : "[" + InstanceSeed.ToString("x") + "]")
            + (Folds == defaults.Folds ? "" : "^" + Folds);


        public override LvqDatasetCli CreateDataset()
        {
            var trainFile = LoadDatasetImpl.dataDir.GetFiles(Filename).FirstOrDefault();
            if (trainFile == null) {
                return null;
            }

            var testFile = TestFilename == null ? null : LoadDatasetImpl.dataDir.GetFiles(TestFilename).FirstOrDefault();
            if (testFile == null && TestFilename != null) {
                return null;
            }

            var trainSet = LoadDatasetImpl.LoadData(trainFile, testFile, this);
            return trainSet;
        }
    }

    public static class LoadDatasetImpl
    {
        public static readonly DirectoryInfo dataDir = FSUtil.FindDataDir(new[] { @"data\datasets\", @"uni\Thesis\datasets\" }, typeof(LoadDatasetImpl));

        public static LvqDatasetCli Load(int folds, string name, uint rngInst)
        {
            var dataFile = dataDir.GetFiles(name).FirstOrDefault();
            return LoadData(dataFile, null, new() { InstanceSeed = rngInst, Folds = folds });
        }


        static readonly char[] dimSep = { ',' };
        static readonly char[] spaceSep = { ' ' };

        static T[,] ToRectangularArray<T>(this T[][] jaggedArray)
        {
            var outerLen = jaggedArray.Length;

            if (outerLen == 0) {
                throw new FileFormatException("No data!");
            }

            var innerLen = jaggedArray[0].Length;

            var retval = new T[outerLen, innerLen];
            for (var i = 0; i < outerLen; i++) {
                var row = jaggedArray[i];
                if (row.Length != innerLen) {
                    throw new FileFormatException("Vectors are of inconsistent lengths");
                }

                for (var j = 0; j < innerLen; j++) {
                    retval[i, j] = row[j];
                }
            }

            return retval;
        }


        public static LvqDatasetCli LoadData(FileInfo dataFile, FileInfo testFile, LoadedDatasetSettings settings)
        {
            settings = settings.Clone();
            settings.Filename = dataFile.Name;
            if (settings.Folds != 0 && settings.TestFilename != null) {
                throw new ArgumentException("Cannot use n-fold crossvalidation and a separate test-set simultaneously");
            }

            var trainingdata = LoadRawData(dataFile);

            //var testdata = testFile == null ? null : LoadRawData(testFile);
            var testdata = testFile == null ? Tuple.Create(default(double[,]), default(int[]), default(string[])) : LoadRawData(testFile);

            var pointArray = trainingdata.Item1;
            var labelArray = trainingdata.Item2;

            var colorSeedLong = labelArray.Select((label, i) => label * (long)(i + 1)).Sum();
            var colorSeed = (int)(colorSeedLong + (colorSeedLong >> 32));

            settings.DimCount = pointArray.GetLength(1);
            settings.ClassCount = trainingdata.Item3.Length;
            settings.PointCount = pointArray.GetLength(0);


            var testPointArray = testdata.Item1;
            var testLabelArray = testdata.Item2;
            if (testPointArray != null) {
                if (settings.DimCount != testPointArray.GetLength(1)) {
                    throw new InvalidOperationException("training file " + dataFile.Name + " has " + settings.DimCount +
                        " dimensions, but test file " + testFile.Name + " has " +
                        testPointArray.GetLength(1)
                    );
                }

                if (!trainingdata.Item3.SequenceEqual(testdata.Item3)) {
                    throw new InvalidOperationException("training file " + dataFile.Name + " has classes: " + string.Join(",", trainingdata.Item3) +
                        " classes, but test file " + testFile.Name + " has " + string.Join(",", testdata.Item3)
                    );
                }
            }


            return LvqDatasetCli.ConstructFromArray(
                rngInstSeed: settings.InstanceSeed,
                label: settings.Shorthand,
                extend: settings.ExtendDataByCorrelation,
                normalizeDims: settings.NormalizeDimensions,
                normalizeByScaling: settings.NormalizeByScaling,
                folds: settings.Folds,
                colors: WpfTools.MakeDistributedColors(settings.ClassCount, new(colorSeed)),
                classes: trainingdata.Item3,
                points: pointArray,
                pointLabels: labelArray,
                testpoints: testPointArray,
                testpointLabels: testLabelArray
            );
        }

        static Tuple<double[,], int[], string[]> LoadRawData(FileInfo dataFile)
        {
            var labelFile = new FileInfo(dataFile.Directory + @"\" + dataFile.Name.Replace(".data", ".label"));
            return labelFile.Exists ? LoadDatasetHelper(dataFile, labelFile) : LoadDatasetHelper(dataFile);
        }

        public static int ReadInt32BigEndian(this BinaryReader reader)
        {
            var val = (uint)reader.ReadInt32();
            return
                (int)
                ((val & 0xff000000) >> 24 | (val & 0x00ff0000) >> 8 | (val & 0x0000ff00) << 8 | (val & 0x000000ff) << 24);
        }

        public static object ReadGzIdx(FileInfo file)
        {
            using (var stream = new GZipStream(file.OpenRead(), CompressionMode.Decompress, false))
            using (var reader = new BinaryReader(stream)) {
                var magic = reader.ReadInt32BigEndian();

                if ((magic & 0xffff0000) != 0) {
                    throw new FileFormatException("Incorrect magic number; should start with two 0 bytes");
                }

                if ((magic & 0xff00) != 0x800) {
                    throw new FileFormatException("Incorrect magic number; should be 0x8?? - only supports unsigned byte data");
                }

                var dimCount = magic & 0xff;
                if (dimCount == 0 || dimCount > 4) {
                    throw new FileFormatException("number of dimensions isn't in range [1,4], that's probably corrupt.");
                }

                var dim0 = reader.ReadInt32BigEndian();
                var dimsRest = Enumerable.Range(1, dimCount - 1).Select(_ => reader.ReadInt32BigEndian()).Aggregate(1, (product, num) => {
                        checked {
                            return product * num;
                        }
                    }
                );

                if (dimCount == 1) {
                    var labels = reader.ReadBytes(dim0);
                    var iLabels = new int[labels.Length];
                    labels.CopyTo(iLabels, 0); //this converts from byte to int!
                    return iLabels;
                }

                var data = new double[dim0, dimsRest];
                for (var i = 0; i < dim0; ++i) {
                    for (var j = 0; j < dimsRest; ++j) {
                        data[i, j] = reader.ReadByte();
                    }
                }

                return data;
            }
        }

        public static Tuple<double[,], int[], string[]> LoadDatasetHelper(FileInfo datafile, FileInfo labelfile)
        {
            var dataVectors =
                datafile.Extension.ToLowerInvariant() == ".gz"
                    ? (double[,])ReadGzIdx(datafile)
                    : (from dataline in datafile.GetLines()
                        select (
                            from dataDim in dataline.Split(dimSep)
                            select double.Parse(dataDim, CultureInfo.InvariantCulture)
                        ).ToArray()
                    ).ToArray().ToRectangularArray();

            var itemLabels =
                labelfile.Extension.ToLowerInvariant() == ".gz"
                    ? (int[])ReadGzIdx(labelfile)
                    : (
                        from labelline in labelfile.GetLines()
                        select int.Parse(labelline, CultureInfo.InvariantCulture)
                    ).ToArray();

            if (dataVectors.GetLength(0) != itemLabels.Length) {
                throw new FileFormatException("Labels have different length(" + itemLabels.Length + " than data(" + dataVectors.GetLength(0) + ")!");
            }

            var denseLabelsAndCount = MakeLabelsDense(itemLabels);

            return Tuple.Create(dataVectors, denseLabelsAndCount.Item1, denseLabelsAndCount.Item2);
        }

        static Tuple<int[], string[]> MakeLabelsDense(int[] itemLabels)
        {
            var denseLabelLookup =
                itemLabels
                    .Distinct()
                    .OrderBy(label => label)
                    .Select((OldLabel, Index) => new { OldLabel, NewLabel = Index })
                    .ToDictionary(a => a.OldLabel, a => a.NewLabel);

            return Tuple.Create(
                itemLabels
                    .Select(oldlabel => denseLabelLookup[oldlabel])
                    .ToArray(), denseLabelLookup.OrderBy(kv => kv.Value).Select(kv => kv.Key.ToString(CultureInfo.InvariantCulture)).ToArray()
            );
        }

        static IEnumerable<string> LoadLines(Stream stream, string extension)
        {
            using (var decompS = extension == ".gz" ? new GZipStream(stream, CompressionMode.Decompress, false) : null)
            using (var reader = new StreamReader(decompS ?? stream)) {
                for (var line = reader.ReadLine(); line != null; line = reader.ReadLine()) {
                    yield return line;
                }
            }
        }

        static string[] LoadLines(FileInfo file)
        {
            using (var stream = file.OpenRead()) {
                return LoadLines(stream, file.Extension).ToArray();
            }
        }

        public static Tuple<double[,], int[], string[]> LoadDatasetHelper(FileInfo dataAndLabelFile)
        {
            var lines = LoadLines(dataAndLabelFile);
            var commasplit = lines.Take(10).All(line => line.Contains(','));

            var splitLines =
                from dataline in lines
                select commasplit ? dataline.Split(dimSep) : dataline.Split(spaceSep, StringSplitOptions.RemoveEmptyEntries);

            var lastColClass = splitLines.Take(10).All(splitLine => Regex.IsMatch(splitLine[^1], @"^[a-zA-Z]\w*$"));
            var firstColClass = !lastColClass && splitLines.Take(10).All(splitLine => Regex.IsMatch(splitLine[0], @"^[a-zA-Z]\w*$"));


            var labelledVectors =
                (from splitLine in splitLines
                    select new {
                        Label = splitLine[firstColClass ? 0 : splitLine.Length - 1],
                        Data = (
                            from dataDim in firstColClass ? splitLine.Skip(1) : splitLine.Take(splitLine.Length - 1)
                            select double.Parse(dataDim, CultureInfo.InvariantCulture)
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
            var minLabel = labelSet.Min();
            var maxLabel = labelSet.Max();
            var labelCount = labelSet.Count;
            if (labelCount != maxLabel + 1 || minLabel != 0) {
                throw new FileFormatException("Class labels must be consecutive integers starting at 0");
            }

            var origLabelLookup = denseLabelLookup.OrderBy(kv => kv.Value).Select(kv => kv.Key).ToArray();

            return Tuple.Create(dataVectors.ToRectangularArray(), itemIntLabels, origLabelLookup);
        }
    }
}
