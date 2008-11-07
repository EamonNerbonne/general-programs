using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace LastFMspider
{
    public enum DataSetType
    {
        Undefined, Complete, Training, Test, Verification
    }
    public enum SimilarityFormat
    {
        LastFmRating,
        Log200,
        Log2000,
        AvgRank,
        AvgRank2,
    };
    public static class SimilarityFormatConv
    {
        public static string ToPathString(SimilarityFormat format) {
            switch (format) {
                case SimilarityFormat.AvgRank: return "1";
                case SimilarityFormat.AvgRank2: return "2";
                case SimilarityFormat.Log200: return "200";
                case SimilarityFormat.Log2000: return "2000";
                default: throw new Exception(format.ToString() + " is not cached.");
            }
        }

    }


    public class SimCacheManager
    {
        public readonly SimilarityFormat Format;
        public readonly LastFmTools Tools;
        public readonly DataSetType DataSetType;

        public DirectoryInfo DataDirectory { get { return Tools.ConfigFile.DataDirectory; } }
        public FileInfo DistanceMatrixCacheFile { get { return new FileInfo(Path.Combine(DataDirectory.FullName, @".\DistanceMatrix" + SimilarityFormatConv.ToPathString(Format) + ".bin")); } }
        public DirectoryInfo DijkstraCacheDir { get { return DataDirectory.CreateSubdirectory("distCache" + SimilarityFormatConv.ToPathString(Format)); } }
        public int TrackNumberOfDijkstraFile(FileInfo file) { return int.Parse(fileNameRegex.Replace(file.Name, "${num}")); }
        public FileInfo DijkstraFileOfTrackNumber(int track) { return new FileInfo(Path.Combine(DijkstraCacheDir.FullName, @".\b" + track + ".bin")); }

        public IEnumerable<NumberedFile> AllTracksCached {
            get {
                return from file in DijkstraCacheDir.GetFiles()
                       let match = fileNameRegex.Match(file.Name)
                       where match.Success
                       select new NumberedFile {
                           file = file,
                           number = int.Parse(match.Groups["num"].Value)
                       };
            }
        }


        public SimCacheManager WithFormat(SimilarityFormat format) { return new SimCacheManager(format, Tools, DataSetType); }
        public SimCacheManager WithDataSetType(DataSetType dataSetType) { return new SimCacheManager(Format, Tools, dataSetType); }

        public SimCacheManager(SimilarityFormat format, LastFmTools tools, DataSetType dataSetType) {
            Format = format;
            Tools = tools;
            DataSetType = dataSetType;
        }

        public SimilarTracks LoadSimilarTracks() { return SimilarTracks.LoadOrCache(this); }
        public TrackMapper LoadTrackMapper() { return SimilarTracks.LoadOnlyTrackMapper(this); }
        public CachedDistanceMatrix LoadCachedDistanceMatrix() { return new CachedDistanceMatrix(this); }

        public FileInfo SimCacheFile { get { return new FileInfo(Path.Combine(DataDirectory.FullName, @".\sims-" + DataSetType.ToString() + ".bin")); } }


        public struct NumberedFile
        {
            public int number;
            public FileInfo file;
        }
        static Regex fileNameRegex = new Regex(@"b(?<num>\d+)\.bin", RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.ExplicitCapture);
    }
}
