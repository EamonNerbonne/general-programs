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
        public struct NumberedFile
        {
            public int number;
            public FileInfo file;
        }
        public static IEnumerable<NumberedFile> AllTracksCached(DirectoryInfo dataDir, SimilarityFormat format) {
            return from file in new CachedDistanceMatrix(dataDir, format).distCacheDir.GetFiles()
                   let match =  fileNameRegex.Match(file.Name)
                   where match.Success
                   select new NumberedFile{
                       file = file, number = int.Parse(match.Groups["num"].Value)
                   };
        }
        public IEnumerable<NumberedFile> UnmappedTracks { get{
                return AllTracksCached(dataDir,format).Where(file => !Mapping.IsMapped(file.number));
            }
        }
        public static int TrackNumberOfFile(FileInfo file) { return int.Parse(fileNameRegex.Replace(file.Name, "${num}")); }

        public SymmetricDistanceMatrix Matrix { get; private set; }
        public ArbitraryTrackMapper Mapping { get; private set; }

        FileInfo fileCache { get { return new FileInfo(Path.Combine(dataDir.FullName, @".\DistanceMatrix" + SimilarityFormatConv.ToPathString(format) + ".bin")); } }
        DirectoryInfo distCacheDir {get { return dataDir.CreateSubdirectory("distCache" +SimilarityFormatConv.ToPathString(format)); }}


        SimilarityFormat format;
        DirectoryInfo dataDir;
        void WriteTo(BinaryWriter writer) {
            Mapping.WriteTo(writer);
            Matrix.WriteTo(writer);
        }
        CachedDistanceMatrix(DirectoryInfo dataDir, SimilarityFormat format) {
            this.format = format;
            this.dataDir = dataDir;
        }
        void Init() {
            var file = fileCache;
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
        public void Save() {
            var file = fileCache;
            using (var stream = file.Open(FileMode.Create, FileAccess.Write))
            using (var writer = new BinaryWriter(stream))
                this.WriteTo(writer);
        }

        static Regex fileNameRegex = new Regex(@"b(?<num>\d+)\.bin", RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        public static CachedDistanceMatrix LoadOrCache(DirectoryInfo dataDirectory, SimilarityFormat format) {
            var mat= new CachedDistanceMatrix(dataDirectory,format);
            mat.Init();
            return mat;
        }

        public void LoadDistFromAllCacheFiles(Action<double> progress) {
            int tot = UnmappedTracks.Count();
            int cur = 0;
            foreach (var file in UnmappedTracks) {
                LoadDistFromCacheFile(file);
                progress(cur++/(double)tot);
            }
        }
        void LoadDistFromCacheFile(NumberedFile nfile) {
            int fileTrackID = nfile.number;
            var file = nfile.file;
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



        public static FileInfo FileForTrack(DirectoryInfo dataDir, SimilarityFormat format, int track) {
            return new FileInfo(Path.Combine(new CachedDistanceMatrix(dataDir, format).distCacheDir.FullName, @".\b" + track + ".bin"));
        }
    }
}
