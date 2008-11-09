using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace LastFMspider
{
    public class ArbitraryTrackMapper
    {
        int mdsIndexCount = 0;
        SortedList<int, int> toMdsIndex = new SortedList<int, int>(); //sure, sorted list inserts are slow - but if you're inserting here
        //you're also inserting into the Matrix which is O(n) unavoidably, and a Dictionary uses a lot of RAM.
        //lookups are probably even faster - O(log n) is faster than hashcodes since the log n factor will almost certainly never grow
        //beyond around 15 or so (and definitely no more than 30), whereas calculating a hashcode is constance but easily more expensive
        //finally, memory dereferencing is more expensive meaning that the O(log n) solution will be faster for all feasible data sizes.

        public int Map(int trackID) {
            int retval;
            if (toMdsIndex.TryGetValue(trackID, out retval)) {
                return retval;
            } else {
                toMdsIndex.Add(trackID, mdsIndexCount);
                return mdsIndexCount++;
            }
        }

        public int GetMap(int trackID) {
            return toMdsIndex[trackID];
        }

        public IEnumerable<int> CurrentlyMapped { get { return toMdsIndex.Keys ; } }
        public IEnumerable<KeyValuePair<int, int>> CurrentMappings { get { return toMdsIndex; } }
        public int Count { get { return mdsIndexCount; } }
        public bool IsMapped(int trackID) { return toMdsIndex.ContainsKey(trackID); }

        public KeyValuePair<int, int> ExtractAndRemoveLast() {
            var retval = CurrentMappings.First(mapping => mapping.Value == mdsIndexCount-1);
            toMdsIndex.Remove(retval.Key);
            mdsIndexCount--;
            return retval;

        }
        public int ReplaceMapping(int trackIDtoReplace, int newTrackID) {

            int mdsIndexToReplace = toMdsIndex[trackIDtoReplace];
            toMdsIndex.Remove(trackIDtoReplace);
            toMdsIndex[newTrackID] = mdsIndexToReplace;
            return mdsIndexToReplace;
        }

        public void WriteTo(BinaryWriter writer) {
            int[] inOrderID = new int[mdsIndexCount];
            foreach (var entry in toMdsIndex) {
                inOrderID[entry.Value] = entry.Key;
            }
            writer.Write(mdsIndexCount);
            foreach (int trackID in inOrderID)
                writer.Write(trackID);
        }

        public ArbitraryTrackMapper() { }
        public ArbitraryTrackMapper(BinaryReader reader) {
            mdsIndexCount = reader.ReadInt32();
            for (int mdsIndex = 0; mdsIndex < mdsIndexCount; mdsIndex++)
                toMdsIndex[reader.ReadInt32()] = mdsIndex;
        }
    }
}
