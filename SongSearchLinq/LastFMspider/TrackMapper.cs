using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.IO;

namespace LastFMspider
{
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
        public int CountDense { get { return sqliteFromDense.Length; } }
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
                    denseFromSqlite[sqliteId] = -1;
                }
            }
        }

        public int LookupDenseID(int sqliteID) {
            return denseFromSqlite[sqliteID];
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
