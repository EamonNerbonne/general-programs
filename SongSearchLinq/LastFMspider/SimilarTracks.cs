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
using EmnExtensions.DebugTools;

namespace LastFMspider
{
    public class SimilarTracks
    {

        public struct DenseSimilarTo : IComparable<DenseSimilarTo>
        {
            public int trackID;
            public float rating;
            internal void WriteTo(BinaryWriter writer) {
                writer.Write(trackID);
                writer.Write(rating);
            }

            public DenseSimilarTo(BinaryReader readFrom) {
                trackID = readFrom.ReadInt32();
                rating = readFrom.ReadSingle();
            }


            public int CompareTo(DenseSimilarTo other) {
                return trackID.CompareTo(other.trackID);
            }
        }
        public struct DenseSimilarity
        {
            public int TrackA, TrackB;
            public float Rating;
        }


        //this is the seed which determines whether a similarity is included in a given dataset.
        //since the data is in memory in a 
        const int dataSetSplitSeed = 42;




        public readonly SimCacheManager Settings;
        DenseSimilarTo[][] similarTo;



        public float? FindRating(int trackA, int trackB) {
            int idx = Array.BinarySearch(similarTo[trackA], new DenseSimilarTo { trackID = trackB });
            if (idx < 0) return null;
            else return similarTo[trackA][idx].rating;
        }

        public TrackMapper TrackMapper { get; private set; }

        internal static TrackMapper LoadOnlyTrackMapper(SimCacheManager settings) {
            var file = settings.SimCacheFile;
            if (file.Exists) {
                using (var stream = file.OpenRead())
                using (var reader = new BinaryReader(stream))
                    return new TrackMapper(reader); //the trackmapper is stored first in the stream so this just works
            } else { //no way around it, we need to load the whole thing first to create the trackmapper from scratch.
                SimilarTracks fulldata = LoadOrCache(settings);
                return fulldata.TrackMapper;
            }
        }

        internal static SimilarTracks LoadOrCache(SimCacheManager settings) {
            var file = settings.SimCacheFile;
            SimilarTracks retval;
            if (file.Exists) {
                using (var stream = file.OpenRead())
                using (var reader = new BinaryReader(stream))
                    retval = new SimilarTracks(reader, settings);
            } else if (settings.DataSetType == DataSetType.Complete) {
                var similarTrackRows = LoadSimilarTracksFromDB(settings, new NiceTimer());
                retval = new SimilarTracks(similarTrackRows, settings);
                System.GC.Collect();
            } else {
                SimilarTrackRow[] similarities = LoadOrCache( settings.WithDataSetType(DataSetType.Complete)).SimilaritiesSqlite.ToArray();
                //we use toarray to reduce peak memory usage - the garbage collector can now recycle the Complete set.

                var labelledlists = new Dictionary<DataSetType, List<SimilarTrackRow>>() {
                    {DataSetType.Training, new List<SimilarTrackRow>()},
                    {DataSetType.Test, new List<SimilarTrackRow>()},
                    {DataSetType.Verification, new List<SimilarTrackRow>()}};
                var train = labelledlists[DataSetType.Training];
                var test = labelledlists[DataSetType.Test];
                var verify = labelledlists[DataSetType.Verification];
                Random r = new Random(dataSetSplitSeed);
                foreach (SimilarTrackRow row in similarities) {
                    double randClass = r.NextDouble();
                    if (randClass < 0.1)
                        verify.Add(row);
                    else if (randClass < 0.2)
                        test.Add(row);
                    else
                        train.Add(row);
                }
                similarities = null;//allow the similarities array too to be recycled
                foreach (var list in labelledlists.Values) list.Capacity = list.Count;
                retval = null;
                foreach (var list in labelledlists) {
                    SimilarTracks current;
                    current = new SimilarTracks(list.Value, settings.WithDataSetType( list.Key));
                    if (settings.DataSetType == current.Settings.DataSetType) retval = current;
                }
                System.GC.Collect();
            }
            return retval;
        }

        public void WriteTo(BinaryWriter writer) {
            if (Settings.Format != SimilarityFormat.LastFmRating) throw new Exception("Serialization only wise in similarity format");
            TrackMapper.WriteTo(writer);//includes num of tracks
            foreach (DenseSimilarTo[] trackSims in similarTo) {
                writer.Write(trackSims.Length);//num of tracks this track is similar to.
                foreach (DenseSimilarTo similarity in trackSims) {
                    similarity.WriteTo(writer);
                }
            }
        }

        public int Count { get; private set; }

        /// <summary>
        /// Returns all similarities.  For a given similarity (a,b,rating) returns either (a,b,rating) or (b,a,rating), such that
        /// the _smaller_ ID is listed first.  This means that if new tracks are added to the index (which have larger sqliteID's and thus 
        /// larger denseID's) their similarities are listed last, and that adding new tracks DOES not change the order of old tracks in this
        /// listing.
        /// </summary>
        public IEnumerable<DenseSimilarity> SimilaritiesRemapped {
            get {
                int returned = 0;
                for (int TrackA = 0; TrackA < similarTo.Length; TrackA++) {
                    for (int i = 0; i < similarTo[TrackA].Length; i++) {
                        int TrackB = similarTo[TrackA][i].trackID;
                        if (TrackB >= TrackA) break;
                        returned++;
#if DEBUG
                        if (returned > Count)
                            Console.WriteLine("heuh?");
#endif
                        yield return new DenseSimilarity {
                            TrackA = TrackA,
                            TrackB = TrackB,
                            Rating = similarTo[TrackA][i].rating
                        };
                    }
                }
                if (returned != Count)
                    Console.WriteLine("he?");
            }
        }

        public IEnumerable<SimilarTrackRow> SimilaritiesSqlite { get { return SimilaritiesRemapped.Select<DenseSimilarity, SimilarTrackRow>(MapToSqlite); } }

        public DenseSimilarTo[] SimilarTo(int remappedID) {
            return similarTo[remappedID];
        }

        public SimilarTrackRow MapToSqlite(DenseSimilarity dense) {
            return new SimilarTrackRow {
                TrackA = TrackMapper.LookupSqliteID(dense.TrackA),
                TrackB = TrackMapper.LookupSqliteID(dense.TrackB),
                Rating = dense.Rating
            };
        }

        SimilarTracks(BinaryReader readFrom, SimCacheManager settings) {
            Settings = settings;
            TrackMapper = new TrackMapper(readFrom);
            similarTo = new DenseSimilarTo[TrackMapper.Count][];
            for (int i = 0; i < similarTo.Length; i++) {
                similarTo[i] = new DenseSimilarTo[readFrom.ReadInt32()];
                for (int j = 0; j < similarTo[i].Length; j++) {
                    similarTo[i][j] = new DenseSimilarTo(readFrom);
                }
            }
            Count = similarTo.Sum(sim => sim.Length) / 2;
            ConvertDataFormat(d => { });
        }




        static SimilarTrackRow[] LoadSimilarTracksFromDB(SimCacheManager settings, NiceTimer timer) {
            SimilarTrackRow[] similarTracks = null;
            var db = settings.Tools.SimilarSongs.backingDB;
            db.RawSimilarTracks.Execute(true,
                (n) => { similarTracks = new SimilarTrackRow[n]; return n; },
                (i, row) => { similarTracks[i] = row; });
            return similarTracks;
        }


        SimilarTracks(IList<SimilarTrackRow> similarTrackRows, SimCacheManager settings) {
            Settings = settings;
            int referencedTrackCount;
            BitArray isReferenced;
            ComputeReferencedTracks(similarTrackRows, out referencedTrackCount, out isReferenced);
            TrackMapper = new TrackMapper(isReferenced, referencedTrackCount);
            TrackMapper.BuildReverseMapping();
            MakeSimilaritiesDense(similarTrackRows);
            MapHandier(similarTrackRows);
            Count = similarTo.Sum(sim => sim.Length) / 2;
            
            using (var stream = Settings.SimCacheFile.Open(FileMode.Create, FileAccess.Write))
            using (var writer = new BinaryWriter(stream))
                WriteTo(writer);

            ConvertDataFormat(d => { });

            //no we convert
        }
        void ComputeReferencedTracks(IList<SimilarTrackRow> similarTracks, out int referencedCount, out BitArray isReferenced) {
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

        void MakeSimilaritiesDense(IList<SimilarTrackRow> sims) {
            for (int i = 0; i < sims.Count; i++) {
                var oldSim = sims[i];
                sims[i] = new SimilarTrackRow {
                    Rating = oldSim.Rating,
                    TrackA = TrackMapper.LookupDenseID(oldSim.TrackA),
                    TrackB = TrackMapper.LookupDenseID(oldSim.TrackB)
                };
            }
        }

        void MapHandier(IList<SimilarTrackRow> similarTracks) {
            int trackCount = TrackMapper.Count;
            int[] refCount = new int[trackCount];//initialized to 0;
            foreach (var sim in similarTracks) {
                refCount[sim.TrackA]++;
                refCount[sim.TrackB]++;
            }
            //note that since it's not guaranteed that the similarities are symmetric, this refCount is between 1x and 2x as big as needed!
            //however, it's still a good start.



            similarTo = new DenseSimilarTo[trackCount][];
            int[] trackWritePos = new int[trackCount];//initially 0;

            for (int i = 0; i < trackCount; i++) {
                similarTo[i] = new DenseSimilarTo[refCount[i]];
            }
            foreach (var sim in similarTracks) {
                similarTo[sim.TrackA][trackWritePos[sim.TrackA]++] = new DenseSimilarTo { trackID = sim.TrackB, rating = sim.Rating };
                similarTo[sim.TrackB][trackWritePos[sim.TrackB]++] = new DenseSimilarTo { trackID = sim.TrackA, rating = sim.Rating };
            }
#if DEBUG
            for (int i = 0; i < refCount.Length; i++) {
                if (trackWritePos[i] != refCount[i])
                    throw new Exception("Coding assumption violated: counted refs differ from written tracks");
            }
#endif

            refCount = null;
            trackWritePos = null;
            similarTracks = null;//no longer needed.

            for (int i = 0; i < similarTo.Length; i++) {
                DenseSimilarTo[] simToThis = similarTo[i];
                Array.Sort(simToThis, (Comparison<DenseSimilarTo>)((a, b) => a.trackID.CompareTo(b.trackID)));
                int writePos = -1;
                int last = -1;
                for (int readPos = 0; readPos < simToThis.Length; readPos++) {
                    DenseSimilarTo sim = simToThis[readPos];
                    if (sim.trackID == i) {//this _really_ shouldn't happen, but it seems we downloaded some info wrongly normalized.
                        continue;
                    }
                    if (sim.trackID != last) {
                        writePos++;
                        simToThis[writePos] = sim;
                    } else {
                        simToThis[writePos].rating = (simToThis[writePos].rating + sim.rating) / 2.0f;
                    }
                    last = sim.trackID;
                }
                Array.Resize(ref simToThis, writePos + 1);//!@#$$^#$^!  forgot the +1, that cost like 5 hours.
#if DEBUG
                last = -1;
                for (int j = 0; j < simToThis.Length; j++) {
                    if (last < simToThis[j].trackID)
                        last = simToThis[j].trackID;
                    else
                        throw new Exception("Coding Assumption violated: simToThis must be sorted");
                }
#endif
                similarTo[i] = simToThis;
            }

#if DEBUG
            for (int TrackA = 0; TrackA < similarTo.Length; TrackA++) {
                for (int i = 0; i < similarTo[TrackA].Length; i++) {
                    var simTo = similarTo[TrackA][i];
                    int TrackB = simTo.trackID;
                    
                    int TrackArev = Array.BinarySearch(similarTo[TrackB], new DenseSimilarTo { trackID=TrackA});
                    if(TrackArev<0)
                        Console.WriteLine("heee????");

                }
            }
#endif
        }

        void ConvertDataFormat(Action<double> progress) {
            switch (Settings.Format) {
                case SimilarityFormat.AvgRank: ConvertToRankFormat(progress,0); break;
                case SimilarityFormat.AvgRank2: ConvertToRankFormat(progress, 99); break;
                case SimilarityFormat.Log200:
                case SimilarityFormat.Log2000:
                    ConvertToDistanceFormat( progress); break;
                case SimilarityFormat.LastFmRating: break;
            }
        }


        void ConvertToDistanceFormat(Action<double> progress) {
            double maxRate;
            switch (Settings.Format) {
                case SimilarityFormat.Log200: maxRate = 200.0; break;
                case SimilarityFormat.Log2000: maxRate = 2000.0; break;
                default:
                    throw new Exception("Invalid conversion to " +Settings.Format);
            }

            double logMaxRate = (float)Math.Log(maxRate);
            Func<float, float> DistFromSim = (sim) => (float)(logMaxRate - Math.Log(sim));

            for (int trackA = 0; trackA < similarTo.Length; trackA++) {
                var simToA = similarTo[trackA];
                for (int j = 0; j < simToA.Length; j++)
                    simToA[j].rating = DistFromSim(simToA[j].rating);

                progress(trackA / (double)similarTo.Length);
            }
        }

        void ConvertToRankFormat(Action<double> progress,int offset) {

            for (int trackA = 0; trackA < similarTo.Length; trackA++) {
                var simToA = similarTo[trackA];
                int rank = 0;
                foreach (var s in simToA
                                    .Select((simTo, i) => new { simTo = simTo, i = i })
                                    .OrderByDescending(s => s.simTo.rating)
                                    ) {
                    int bInA = s.i;
                    int trackB = simToA[bInA].trackID;
                    rank++;
                    if (trackB < trackA) {// this guy's been processed!
                        var simToB = similarTo[trackB];
                        int aInB = Array.BinarySearch(simToB, new DenseSimilarTo { trackID = trackA });

                        simToB[aInB].rating = simToA[bInA].rating = (simToB[aInB].rating + rank+offset) / 2.0f;

                    } else {
                        simToA[bInA].rating = rank+offset;
                    }
                }
                progress(trackA / (double)similarTo.Length);

            }
        }


    }
}
