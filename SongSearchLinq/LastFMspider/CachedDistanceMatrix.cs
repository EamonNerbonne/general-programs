using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace LastFMspider
{
    public class CachedDistanceMatrix
    {
        public readonly SimCacheManager Settings;
        public IEnumerable<SimCacheManager.NumberedFile> UnmappedTracks { get{
                return Settings.AllTracksCached.Where(file => !Mapping.IsMapped(file.number));
            }
        }

        public SymmetricDistanceMatrix Matrix { get; private set; }
        public ArbitraryTrackMapper Mapping { get; private set; }



        void WriteTo(BinaryWriter writer) {
            Mapping.WriteTo(writer);
            Matrix.WriteTo(writer);
        }
        internal CachedDistanceMatrix(SimCacheManager settings) {
            Settings = settings;
            var file = Settings.DistanceMatrixCacheFile;
            if (file.Exists) {
                using (var stream = file.OpenRead())
                using (var reader = new BinaryReader(stream)) {
                    Mapping = new ArbitraryTrackMapper(reader);
                    Matrix = new SymmetricDistanceMatrix(reader);
                }
            } else {
                Mapping = new ArbitraryTrackMapper();
                Matrix = new SymmetricDistanceMatrix(0);
            }
        }
        void Save() {
            var file = Settings.DistanceMatrixCacheFile;
            using (var stream = file.Open(FileMode.Create, FileAccess.Write))
            using (var writer = new BinaryWriter(stream))
                this.WriteTo(writer);
        }


        public void LoadDistFromAllCacheFiles(Action<double> progress, bool saveAfterwards) {
            int tot = UnmappedTracks.Count();
            int cur = 0;
            foreach (var file in UnmappedTracks.ToArray()) {
                LoadDistFromCacheFile(file, false);
                progress(++cur/(double)tot);
            }
            if (saveAfterwards)
                Save();
        }
        void LoadDistFromCacheFile(SimCacheManager.NumberedFile nfile,bool reload) {
            int fileTrackID = nfile.number;
            var file = nfile.file;
            int fileMdsID;
            if (Mapping.IsMapped(fileTrackID)) {
                if (!reload)
                    throw new Exception("Trying to load alreadly loaded track.");
                else
                    fileMdsID = Mapping.GetMap(fileTrackID);
            } else { //new file
                fileMdsID = Mapping.Map(fileTrackID);
                if (Matrix.ElementCount != fileMdsID) 
                    throw new Exception("Matrix and Mapping running out of sync!");
                else 
                    Matrix.ElementCount = fileMdsID + 1;
                }
            try {
                float[] distFromFileTrack;
                using (var stream = file.OpenRead()) //this might throw due to another process just writing this cache file
                using (var reader = new BinaryReader(stream)) {
                    distFromFileTrack = new float[reader.ReadInt32()];
                    for (int i = 0; i < distFromFileTrack.Length; i++)
                        distFromFileTrack[i] = reader.ReadSingle();
                }
                foreach (var other in Mapping.CurrentMappings) {
                    int otherMdsID = other.Value;
                    int otherTrackID = other.Key;
                    if (otherMdsID != fileMdsID) //can't map to itself!
                        Matrix[fileMdsID, otherMdsID] = distFromFileTrack[otherTrackID];
                }
                Console.WriteLine("Loaded {0} (Strong) (Count = {1})", fileTrackID,Mapping.Count);

            } catch (Exception e) {
                Console.WriteLine("File couldn't be loaded due to exception: {0}\n\nException: {1}", file.Name, e);
                RemoveTrackID(fileTrackID);
            }
        }


        /// <summary>
        /// This code can remove any track from the mapping, although it's actually only used to remove the last track
        /// </summary>
        private void RemoveTrackID( int fileTrackID) {
            int fileMdsID = Mapping.GetMap(fileTrackID);
            //We want to remove this track from the Mapping and Matrix.
            //we can only remove the last file, however, so we'll overwrite this file with the last one and then remove the last file.
            //to do that, we'll find the "last" mapping and put it here.
            var victimMapping = Mapping.ExtractAndRemoveLast();
            int victimMdsId = Matrix.ElementCount - 1;
            int victimFileTrackId = victimMapping.Key;
            if (victimMdsId != victimMapping.Value)
                throw new Exception("Mapping and matrix running out of sync!");
            if (fileMdsID == victimMdsId) {
                //we're lucky and don't need to swap anything - just remove the last element from the matrix and be done.
                Matrix.ElementCount = Matrix.ElementCount - 1;
                return;
            } //otherwise, the mapping removed was the last one, but _actually_ we want to remove the fileMdsID mapping.

            int mdsIdToOverwrite = Mapping.ReplaceMapping(fileTrackID, victimFileTrackId);
            if (mdsIdToOverwrite != fileMdsID)
                throw new Exception("Mapping and matrix running out of sync!");

            //OK, so now the Mapping of fileTrackID is gone, and victimFileTrackId maps to fileMdsId 
            //instead, so we can stick the victim's 
            //distances (i.e. in the matrix in row victimMdsId) into fileMdsID's.

            for (int i = 0; i < Matrix.ElementCount - 1; i++) {
                if (i == fileMdsID) continue;//cannot set the distance of an object to itself.
                Matrix[fileMdsID, i] = Matrix[victimMdsId, i];
            }
            Matrix.ElementCount = Matrix.ElementCount - 1;
            //but, now the victim was rewritten and thus it's original distances can be removed.

            if (Matrix.ElementCount != Mapping.Count)
                throw new Exception("Mapping and matrix running out of sync!");
        }



    }
}
