using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using EmnExtensions;
using LastFMspider.LastFMSQLiteBackend;
using System.Data.Common;
using System.Collections;
using System.Diagnostics;
using SongDataLib;

namespace LastFMspider
{
    public class SimilarTracks
    {
        struct SimilarTo
        {
            public int otherTrack;
            public float rating;

            internal void WriteTo(BinaryWriter writer) {
                writer.Write(otherTrack);
                writer.Write(rating);
            }

            public SimilarTo(BinaryReader readFrom) {
                otherTrack = readFrom.ReadInt32();
                rating = readFrom.ReadSingle();
            }

        }
        SimilarTo[][] similarTo;
        public TrackMapper TrackMapper { get; private set; }
        public static FileInfo SimCacheFile(SongDatabaseConfigFile config) {
            return new FileInfo(Path.Combine(config.DataDirectory.FullName, @".\sims.bin"));
        }
        public static SimilarTracks LoadOrCache(LastFmTools tools,bool reloadfromDB) {
            SongDatabaseConfigFile config=tools.ConfigFile;
            var file = SimCacheFile(config);
            SimilarTracks retval;
            if (file.Exists && !reloadfromDB) {
                using (var stream = file.OpenRead())
                using (var reader = new BinaryReader(stream))
                    retval = new SimilarTracks(reader);
            } else {
                retval = new SimilarTracks(tools, new NiceTimer());
                using (var stream = file.Open(FileMode.Create, FileAccess.Write))
                using (var writer = new BinaryWriter(stream))
                    retval.WriteTo(writer);
                System.GC.Collect();
            }
            return retval;
        }


        public SimilarTracks(LastFmTools tools, NiceTimer timer) {
            var similarTracks = LoadSimilarTracksFromDB(tools, timer);
            int referencedTrackCount;
            BitArray isReferenced;
            ComputeReferencedTracks(similarTracks, out referencedTrackCount, out isReferenced);
            TrackMapper = new TrackMapper(isReferenced, referencedTrackCount);
            TrackMapper.BuildReverseMapping();
            MakeSimilaritiesDense(similarTracks);
            MapHandier(similarTracks);
        }

        public void WriteTo(BinaryWriter writer) {
            TrackMapper.WriteTo(writer);//includes num of tracks
            foreach (SimilarTo[] trackSims in similarTo) {
                writer.Write(trackSims.Length);//num of tracks this track is similar to.
                foreach (SimilarTo similarity in trackSims) {
                    similarity.WriteTo(writer);
                }
            }
        }
        public SimilarTracks(BinaryReader readFrom) {

            TrackMapper = new TrackMapper(readFrom);
            similarTo = new SimilarTo[TrackMapper.CountDense][];
            for (int i = 0; i < similarTo.Length; i++) {
                similarTo[i] = new SimilarTo[readFrom.ReadInt32()];
                for (int j = 0; j < similarTo[i].Length; j++) {
                    similarTo[i][j] = new SimilarTo(readFrom);
                }
            }
        }


        SimilarTrackRow[] LoadSimilarTracksFromDB(LastFmTools tools, NiceTimer timer) {
            SimilarTrackRow[] similarTracks;
            var db = tools.SimilarSongs.backingDB;
            using (DbTransaction trans = db.Connection.BeginTransaction()) {
                timer.TimeMark("Counting tracks...");
                int simCount = db.CountSimilarities.Execute();
                int i = 0;
                timer.TimeMark("Alloc array...");
                similarTracks = new SimilarTrackRow[simCount];
                timer.TimeMark("Loading similar tracks table");
                foreach (SimilarTrackRow simTrack in db.RawSimilarTracks.Execute(true)) {
                    similarTracks[i] = simTrack;
                    i++;
                }
                Debug.Assert(i == simCount, "The counted number of similarity does not equal the number retrieved!");
                trans.Commit();
            }
            return similarTracks;
        }


        void ComputeReferencedTracks(SimilarTrackRow[] similarTracks, out int referencedCount, out BitArray isReferenced) {
            int maxTrackId = 0;
            foreach (var entry in similarTracks) {
                maxTrackId = Math.Max(maxTrackId, Math.Max(entry.TrackA, entry.TrackB));
            }
            isReferenced = new BitArray(maxTrackId + 1, false);
            referencedCount = 0;
            foreach (var entry in similarTracks) {
                if (!isReferenced[entry.TrackA]) {
                    referencedCount++;
                    isReferenced[entry.TrackA] = true;
                }
                if (!isReferenced[entry.TrackB]) {
                    referencedCount++;
                    isReferenced[entry.TrackB] = true;
                }
            }
        }


        void MakeSimilaritiesDense(SimilarTrackRow[] sims) {
            for (int i = 0; i < sims.Length; i++) {
                var oldSim = sims[i];
                sims[i] = new SimilarTrackRow {
                    Rating = oldSim.Rating,
                    TrackA = TrackMapper.LookupDenseID(oldSim.TrackA),
                    TrackB = TrackMapper.LookupDenseID(oldSim.TrackB)
                };
            }
        }

        void MapHandier(SimilarTrackRow[] similarTracks) {
            int trackCount = TrackMapper.CountDense;
            int[] refCount = new int[trackCount];//initialized to 0;
            foreach (var sim in similarTracks) {
                refCount[sim.TrackA]++;
                refCount[sim.TrackB]++;
            }
            //note that since it's not guaranteed that the similarities are symmetric, this refCount is between 1x and 2x as big as needed!
            //however, it's still a good start.



            similarTo = new SimilarTo[trackCount][];
            int[] trackWritePos = new int[trackCount];

            for (int i = 0; i < refCount.Length; i++) {
                similarTo[i] = new SimilarTo[refCount[i]];
            }
            foreach (var sim in similarTracks) {
                similarTo[sim.TrackA][trackWritePos[sim.TrackA]++] = new SimilarTo { otherTrack = sim.TrackB, rating = sim.Rating };
                similarTo[sim.TrackB][trackWritePos[sim.TrackB]++] = new SimilarTo { otherTrack = sim.TrackA, rating = sim.Rating };
            }
            similarTracks = null;//no longer needed.

            for (int i = 0; i < refCount.Length; i++) {
                SimilarTo[] simToThis = similarTo[i];
                Array.Sort(simToThis, (Comparison<SimilarTo>)((a, b) => a.otherTrack.CompareTo(b.otherTrack)));
                int writePos = 0;
                int last = -1;
                for (int readPos = 0; readPos < simToThis.Length; readPos++) {
                    SimilarTo sim = simToThis[readPos];
                    if (sim.otherTrack != last) {
                        simToThis[writePos] = sim;
                        writePos++;
                    } else {
                        simToThis[writePos].rating = (simToThis[writePos].rating + sim.rating) / 2.0f;
                    }
                    last = sim.otherTrack;

                }
                Array.Resize(ref simToThis, writePos);
                similarTo[i] = simToThis;
            }

        }

        public IEnumerable<SimilarTrackRow> Similarities {
            get {
                for (int TrackA = 0; TrackA < similarTo.Length; TrackA++) {
                    for (int i = 0; i < similarTo[TrackA].Length; i++) {
                        yield return new SimilarTrackRow {
                            TrackA = TrackA,
                            TrackB = similarTo[TrackA][i].otherTrack,
                            Rating = similarTo[TrackA][i].rating
                        };
                    }
                }
            }
        }

    }
}
