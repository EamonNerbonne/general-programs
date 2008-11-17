using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SongDataLib;
using LastFMspider;
using System.IO;

namespace SimilarityMdsLib
{
    public class MdsResults
    {
        public static FileInfo ResultsPath(MdsEngine.FormatAndOptions fopt, SimCacheManager settings) {
            return new FileInfo(System.IO.Path.Combine(settings.DataDirectory.FullName, @".\pos-" + fopt + ".pos"));
        }
        public static MdsResults LoadResults(MdsEngine.FormatAndOptions withOptions, SimCacheManager settings) {
            var file = ResultsPath(withOptions, settings);
            if (!file.Exists) return null;
            else return new MdsResults(file);
        }
        private MdsResults(FileInfo fi) {
            using (var stream = fi.OpenRead())
            using (var reader = new BinaryReader(stream)) {
                int pointCount = reader.ReadInt32();
                int dimCount = reader.ReadInt32();
                Embedding = new double[pointCount, dimCount];

                for (int pi = 0; pi < pointCount; pi++)
                    for (int dim = 0; dim < dimCount; dim++)
                        Embedding[pi, dim] = reader.ReadDouble();
                Mapper = new TrackMapper(reader);
            }

        }
        public readonly TrackMapper Mapper;
        public readonly double[,] Embedding;
    }
}
