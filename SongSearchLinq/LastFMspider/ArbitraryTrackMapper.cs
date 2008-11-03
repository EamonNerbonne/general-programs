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
        Dictionary<int, int> toMdsIndex = new Dictionary<int, int>();
        public int Map(int trackID) {
            int retval;
            if (toMdsIndex.TryGetValue(trackID, out retval)) {
                return retval;
            } else {
                toMdsIndex.Add(trackID, mdsIndexCount);
                return mdsIndexCount++;
            }
        }
        public IEnumerable<int> CurrentlyMapped { get { return toMdsIndex.Keys; } }
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
