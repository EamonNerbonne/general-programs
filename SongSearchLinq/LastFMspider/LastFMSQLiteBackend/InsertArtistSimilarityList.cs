
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Reflection;
using System.Diagnostics;
using System.Net;

namespace LastFMspider.LastFMSQLiteBackend
{
	public class InsertArtistSimilarityList : AbstractLfmCacheQuery
	{
		public InsertArtistSimilarityList(LastFMSQLiteCache lfm)
			: base(lfm) {
			lowerArtist = DefineParameter("@lowerArtist");
			lookupTimestamp = DefineParameter("@lookupTimestamp");
			statusCode = DefineParameter("@statusCode");
		}
		protected override string CommandText {
			get {
				return @"
INSERT INTO [SimilarArtistList] (ArtistID, LookupTimestamp,StatusCode) 
SELECT A.ArtistID, (@lookupTimestamp) AS LookupTimestamp, (@statusCode) AS StatusCode
FROM Artist A
WHERE A.LowercaseArtist = @lowerArtist;

SELECT L.ListID
FROM SimilarArtistList L, Artist A
WHERE A.LowercaseArtist = @lowerArtist
AND L.ArtistID = A.ArtistID
AND L.LookupTimestamp = @lookupTimestamp
";
			}
		}

		DbParameter lowerArtist, lookupTimestamp, statusCode;


		private static IEnumerable<T> DeNull<T>(IEnumerable<T> iter) { return iter == null ? Enumerable.Empty<T>() : iter; }
		public void Execute(ArtistSimilarityList simList) {
			lock (SyncRoot) {
				using (DbTransaction trans = Connection.BeginTransaction()) {
					lfmCache.InsertArtist.Execute(simList.Artist);
					lfmCache.UpdateArtistCasing.Execute(simList.Artist);
					int listID;
					lowerArtist.Value = simList.Artist.ToLatinLowercase();
					lookupTimestamp.Value = simList.LookupTimestamp.Ticks;
					statusCode.Value = simList.StatusCode;
					using (var reader = CommandObj.ExecuteReader()) {
						if (reader.Read()) { //might need to do reader.NextResult();
							listID = (int)(long)reader[0];
						} else {
							throw new Exception("Command failed???");
						}
					}

					foreach (var similarArtist in simList.Similar) {
						lfmCache.InsertArtistSimilarity.Execute(listID, similarArtist.Artist, similarArtist.Rating);
						lfmCache.UpdateArtistCasing.Execute(similarArtist.Artist);
					}
					trans.Commit();
				}
			}
		}



	}


	public delegate void DynamicAction(params object[] parameters);
	static class DynamicActionBuilder
	{
		public static void PerformAction0(Action a, object[] pars) { a(); }
		public static void PerformAction1<T1>(Action<T1> a, object[] pars) {
			a((T1)pars[0]);
		}
		public static void PerformAction2<T1, T2>(Action<T1, T2> a, object[] pars) {
			a((T1)pars[0], (T2)pars[1]);
		}
		//etc...

		public static DynamicAction MakeAction(object target, MethodInfo mi) {
			Type[] typeArgs =
				mi.GetParameters().Select(pi => pi.ParameterType).ToArray();
			string perfActName = "PerformAction" + typeArgs.Length;
			MethodInfo performAction =
				typeof(DynamicActionBuilder).GetMethod(perfActName);
			if (typeArgs.Length != 0)
				performAction = performAction.MakeGenericMethod(typeArgs);
			Type actionType = performAction.GetParameters()[0].ParameterType;
			Delegate action = Delegate.CreateDelegate(actionType, target, mi);
			return (DynamicAction)
				Delegate.CreateDelegate(typeof(DynamicAction), action, performAction);
		}
	}

	static class TestDab
	{
		public static void PrintTwo(int a, int b) {
			Console.WriteLine("{0} {1}", a, b);
			Trace.WriteLine(string.Format("{0} {1}", a, b));//for immediate window.
		}
		public static void PrintHelloWorld() {
			Console.WriteLine("Hello World!");
			Trace.WriteLine("Hello World!");//for immediate window.
		}

		public static void TestIt() {
			var dynFunc = DynamicActionBuilder.MakeAction(null,
				typeof(TestDab).GetMethod("PrintTwo"));
			dynFunc(3, 4);
			var dynFunc2 = DynamicActionBuilder.MakeAction(null,
				typeof(TestDab).GetMethod("PrintHelloWorld"));
			dynFunc2(3, 4); //3, 4 are ignored, you may want code to forbid this.
		}
	}
}
