using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmnExtensions;
using SongDataLib;
using LastFMspider;
using System.IO;
using System.Text.RegularExpressions;

namespace RealSimilarityMds
{
    class Program
    {
        static LastFmTools tools;
        static Regex fileNameRegex = new Regex(@"(e|t|b)(?<num>\d+)\.(dist|bin)", RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        static void Main(string[] args) {
            NiceTimer timer = new NiceTimer();
            timer.TimeMark("Loading");
            SongDatabaseConfigFile config = new SongDatabaseConfigFile(false);
            tools = new LastFmTools(config);
            //var sims = SimilarTracks.LoadOrCache(tools, SimilarTracks.DataSetType.Training);
            //sims.ConvertToDistanceFormat();
            timer.TimeMark("GC");
            System.GC.Collect();
            timer.TimeMark("Indexing files.");
            DirectoryInfo dir = new DirectoryInfo(@"C:\emn\");
            SymmetricDistanceMatrix mat = new SymmetricDistanceMatrix(0);
            int mdsIndexCount = 0;
            Dictionary<int,int> toMdsIndex = new Dictionary<int,int>(), toDenseIndex = new Dictionary<int,int>();
            var files = dir.GetFiles();

            foreach (var file in files) {
                if (fileNameRegex.IsMatch(file.Name)) {
                    int trackNum = int.Parse(fileNameRegex.Replace(file.Name, "${num}"));
                    toDenseIndex[mdsIndexCount] = trackNum;
                    toMdsIndex[trackNum] = mdsIndexCount;
                    mdsIndexCount++;
                }
            }

            mat.ElementCount = mdsIndexCount;

            timer.TimeMark("Loading files");
            foreach (var file in files) {
                if (fileNameRegex.IsMatch(file.Name)) {
                    int trackNum = int.Parse(fileNameRegex.Replace(file.Name, "${num}"));
                    int mdsNum = toMdsIndex[trackNum];
                    if (file.Name.EndsWith("bin")) {
                        float[] distFromTrack;
                        using (var stream = file.OpenRead())
                        using (var reader = new BinaryReader(stream)) {
                            distFromTrack = new float[reader.ReadInt32()];
                            for (int i = 0; i < distFromTrack.Length; i++)
                                distFromTrack[i] = reader.ReadSingle();
                        }
                        foreach (var other in toMdsIndex) {
                            int mdsI = other.Value;
                            int denseI = other.Key;
                            if(mdsI!=mdsNum)
                            mat[mdsNum, mdsI] = distFromTrack[denseI];
                        }
                    } else {
                        foreach (var line in file.OpenText().ReadToEnd().Split(new[]{'\n'},StringSplitOptions.RemoveEmptyEntries)) {
                            var entries=line.Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries);
                            if(entries.Length!=2) throw new Exception("Invalid format");
                            int denseI = int.Parse(entries[0]);
                            float dist = float.Parse(entries[1]);
                            
                            int mdsI;
                            if( toMdsIndex.TryGetValue(denseI,out mdsI) && mdsI != mdsNum)
                            mat[mdsI, mdsNum] = dist;
                        }
                    }
                }
            }
            timer.Done();

        }
    }
}
