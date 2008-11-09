using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.IO;

namespace LastFMspider
{
    /// <summary>
    /// TrackMapper maps a potentially sparse input set of integers to [0..n) where n is the number of ints in the set.
    /// The mapping is order preserving, so if i > j, the map(i)>map(j)
    /// Since it is intended to be used on only slightly sparse input sets, the input set is passed as a BitArray, though
    ///  the code could certainly be rewritten to use another (ordered) collection, such as for example simply a sorted array.
    ///  
    /// TrackMapper provides two forward mapping functions from the sparse range to the dense range:
    /// 
    /// LookupDenseID is O(1) by using a lookup array.  However, this array uses O(max(id)) memory so is infeasible for heavily
    /// sparse mappings.  Since this reverse mapping isn't always reasonable, it must be explicitly requested first with the method
    /// BuildReverseMapping(), and O(max(id)) operation.  If the mapping wasn't built, throws NullReferenceException.
    ///
    /// FindDenseID is O(log n) and simply uses binary search to find the dense id with the sought-after sparse id.  If the reverse mapping
    /// is initialized, it uses that instead.
    /// 
    /// Both mappings return the denseId if found or the bitwise complement of the next larger denseId if not found 
    /// (i.e. just like Array.BinarySearch)
    /// </summary>
    public class TrackMapper
    {
        int[] sqliteFromDense;
        int[] denseFromSqlite;
        public TrackMapper(BitArray referencedTracks, int totalReferenced) {
            sqliteFromDense = new int[totalReferenced];
            int nextAvail = 0;
            for (int i = 0; i < referencedTracks.Length; i++)
                if (referencedTracks[i])
                    sqliteFromDense[nextAvail++] = i;
            if (totalReferenced != nextAvail) throw new ArgumentException("totalReferenced wrong!");
        }
        public int Count { get { return sqliteFromDense.Length; } }
        public int CountSqlite { get { return sqliteFromDense[sqliteFromDense.Length - 1] + 1; } }
        public TrackMapper(BinaryReader readFrom) {
            int count = readFrom.ReadInt32();
            sqliteFromDense = new int[count];
            for (int i = 0; i < count; i++) {
                sqliteFromDense[i] = readFrom.ReadInt32();
            }

        }

        public void WriteTo(BinaryWriter writeTo) {
            writeTo.Write(sqliteFromDense.Length);
            for (int i = 0; i < sqliteFromDense.Length; i++) {
                writeTo.Write(sqliteFromDense[i]);
            }
        }

        public void BuildReverseMapping() {
            if (denseFromSqlite != null) return;
            int count = sqliteFromDense[sqliteFromDense.Length - 1] + 1;
            denseFromSqlite = new int[count];
            int denseId = 0;

            for (int sqliteId = 0; sqliteId < count; sqliteId++) {
                if (sqliteFromDense[denseId] == sqliteId) {
                    denseFromSqlite[sqliteId] = denseId;
                    denseId++;
                    //note if denseId reaches sqliteFromDense.Length we should stop.
                    //However if it is now sqliteFromDense.Length then it was sqliteFromDense.Length - 1 
                    //and by construction we have count == sqliteFromDense[sqliteFromDense.Length-1]+1
                    //so sqliteId == sqliteFromDense[sqliteFromDense.Length-1] == count -1
                    //so the loop will end anyhow and we don't need to check for it.
                } else {
                    denseFromSqlite[sqliteId] = ~denseId;
                }
            }
        }

        public int LookupDenseID(int sqliteID) {
            return sqliteID > denseFromSqlite.Length ? ~denseFromSqlite.Length : denseFromSqlite[sqliteID];
        }

        public int LookupSqliteID(int denseID) {
            return sqliteFromDense[denseID];
        }

        public int FindDenseID(int sqliteID) {
            if (denseFromSqlite != null) return denseFromSqlite[sqliteID];
            else return Array.BinarySearch(sqliteFromDense, sqliteID);
        }
    }
}
