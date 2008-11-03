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
        public static IEnumerable<FileInfo> AllTracksCached(DirectoryInfo dataDir) { return distCacheDir(dataDir).GetFiles().Where(file => fileNameRegex.IsMatch(file.Name)); }
        static IEnumerable<FileInfo> TracksWeaklyCached(DirectoryInfo dataDir) { return AllTracksCached(dataDir).Where(file => file.Extension == ".dist"); }
        public static IEnumerable<FileInfo> TracksCached(DirectoryInfo dataDir) { return AllTracksCached(dataDir).Where(file => file.Extension == ".bin"); }
        public static int TrackNumberOfFile(FileInfo file) { return int.Parse(fileNameRegex.Replace(file.Name, "${num}")); }

        public SymmetricDistanceMatrix Matrix { get; private set; }
        public ArbitraryTrackMapper Mapping { get; private set; }

        static FileInfo fileCache(DirectoryInfo dataDir) { return new FileInfo(Path.Combine(dataDir.FullName, @".\DistanceMatrix.bin")); }
        static DirectoryInfo distCacheDir(DirectoryInfo dataDir) { return dataDir.CreateSubdirectory("distCache"); }


        DirectoryInfo dataDir;
        void WriteTo(BinaryWriter writer) {
            Mapping.WriteTo(writer);
            Matrix.WriteTo(writer);
        }
        CachedDistanceMatrix(DirectoryInfo dataDir) {
            this.dataDir = dataDir;
            var file = fileCache(dataDir);
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
            LoadWeaklyCachedFiles();
        }

        public void Save() {
            var file = fileCache(dataDir);
            using (var stream = file.Open(FileMode.Create, FileAccess.Write))
            using (var writer = new BinaryWriter(stream))
                this.WriteTo(writer);
        }

        static Regex fileNameRegex = new Regex(@"(e|t|b)(?<num>\d+)\.(dist|bin)", RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        public static CachedDistanceMatrix LoadOrCache(DirectoryInfo dataDirectory) {
            return new CachedDistanceMatrix(dataDirectory);
        }

        public void LoadDistFromAllCacheFiles() {
            foreach (var file in TracksCached(dataDir))
                LoadDistFromCacheFile(file);
        }
        public void LoadDistFromCacheFile(FileInfo file) {
            if (file.Extension != ".bin")
                throw new Exception("Can't dynamically load weakly cached files.");
            int fileTrackID = TrackNumberOfFile(file);
            if (Mapping.IsMapped(fileTrackID))
                return;//this file was already mapped in the cache!
            int fileMdsID = Mapping.Map(fileTrackID);
            if (Matrix.ElementCount <= fileMdsID) Matrix.ElementCount = fileMdsID + 1;
            try {
                float[] distFromFileTrack;
                using (var stream = file.OpenRead())
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
                ProcessError(e, file, fileMdsID, fileTrackID);
            }
        }

        /// <summary>
        /// This function precaches all "*.dist" files that don't have complete distance info.
        /// These must all be loaded in a chuck, since there tracks don't necessarily contain distance info to everything.
        /// </summary>
        void LoadWeaklyCachedFiles() {
            var files = TracksWeaklyCached(dataDir);
            int currentlyMapped = Mapping.Count; //new files will have a mapped ID at least this high!

            foreach (var file in files) {//we need to map first so that the ID's are known ahead of time.
                Mapping.Map(TrackNumberOfFile(file));
            }
            Matrix.ElementCount = Mapping.Count;//allocate room in matrix!
            foreach (var file in files) {
                int fileTrackID = TrackNumberOfFile(file);
                int fileMdsID = Mapping.Map(fileTrackID);
                if (fileMdsID < currentlyMapped) continue; //this files was already mapped in the cache!
                try {
                    foreach (var line in file.OpenText().ReadToEnd().Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)) {
                        var entries = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (entries.Length != 2) throw new Exception("Invalid format");
                        int otherTrackID = int.Parse(entries[0]);
                        float fileToOtherDist = float.Parse(entries[1]);

                        if (otherTrackID != fileTrackID && Mapping.IsMapped(otherTrackID))
                            Matrix[Mapping.Map(otherTrackID), fileMdsID] = fileToOtherDist;
                    }
                    Console.WriteLine("Loaded {0} (Weak) (Count = {1})", fileTrackID, Mapping.Count);
                } catch (Exception e) {
                    ProcessError(e, file, fileMdsID, fileTrackID);
                    throw new Exception("This might make the distance matrix sparse!  If you know what you're doing, recompile without this exception",e);
                }
            }
        }


        /// <summary>
        /// If for some reason an error occurs, we need to roll back the data structures, which is what ProcessError does.
        /// </summary>
        private void ProcessError(Exception e, FileInfo file, int fileMdsID, int fileTrackID) {
            Console.WriteLine("File couldn't be loaded due to exception: {0}\n\nException: {1}", file.Name, e);
            //OK, something went wrong duing loading.  We want to remove this track from the Mapping and Matrix.
            //to do that, we'll find the "last" mapping and put it here.
            var removedMapping = Mapping.ExtractAndRemoveLast();
            int mdsIdToMove = Matrix.ElementCount - 1;
            if (mdsIdToMove != removedMapping.Value)
                throw new Exception("Mapping and matrix running out of sync!");
            int mdsIdToOverwrite = Mapping.ReplaceMapping(fileTrackID, removedMapping.Key);
            if (mdsIdToOverwrite != fileMdsID)
                throw new Exception("Mapping and matrix running out of sync!");

            //OK, so now the Mapping of fileTrackID is gone, and so is the mapping of removedMapping.Key
            //instead, whatever trackID is removedMapping.Key now poins to fileMdsID, to we can stick it's 
            //distances (at removedMapping.Value == mdsIdToMove) into fileMdsID's.

            for (int i = 0; i < Matrix.ElementCount - 1; i++) {
                if (i == fileMdsID) continue;//cannot set the distance for the broken file itself.
                Matrix[fileMdsID, i] = Matrix[mdsIdToMove, i];
            }
            Matrix.ElementCount = Matrix.ElementCount - 1;
            //but, now whatever Mapping pointed to mdsIdToMove should now point to fileMdsId.

            if (Matrix.ElementCount != Mapping.Count)
                throw new Exception("Mapping and matrix running out of sync!");
        }



        public static FileInfo FileForTrack(DirectoryInfo dataDir, int track) {
            return new FileInfo(Path.Combine(distCacheDir( dataDir).FullName, @".\b" + track + ".bin"));
        }
    }
}
