using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SQLite;
using System.Data.Common;
namespace SqliteExperiment
{
    class TestDBConnector
    {
        SQLiteConnection conn;
        SQLiteCommand selectAll;
        SQLiteCommand insertIt;
        public TestDBConnector()
        {
            conn = new SQLiteConnection(Properties.Settings.Default.testDBConnStr);
            conn.Open();
            selectAll = conn.CreateCommand();
            selectAll.CommandText = @"SELECT fileUID, path, haha FROM SimpleTable";
//            insertIt = conn.CreateCommand();
//            insertIt.CommandText = @"SELECT fileUID, path, haha FROM SimpleTable";
        }
        public bool IsConnected { get { return conn.State == System.Data.ConnectionState.Open; } }
        public void TestIt(Action<string> printer)
        {
            using (SQLiteDataReader reader = selectAll.ExecuteReader())
            {
                while (reader.Read())
                {
                    printer(String.Format("id = {0}, path = {1}, haha={2}", reader[0], reader[1], reader[2]));
                }

            }
        }
    }
}
