using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SimpleParsingBenchmark
{
	class Program
	{
		static IEnumerable<string> matches(string test) {
			int lastMatch=0;
			for(int i = 0; i < test.Length; i++)
				if(test[i] == ',') {
					yield return test.Substring(lastMatch, i - lastMatch);
					lastMatch = i + 1;
				}
		}

		static void Main(string[] args) {
			string query = "TSET_";
			string testStr = "THIS_IS_JUST_A_TEST_abcdefghijklmnopqrstuvwxyz0123456789!987654321zyxwvutsrqpomnlkjihgfedcba_TSET_A_TSUJ_SI_SIHT";
			for(int q = 0; q < 3; q++)
				testStr += testStr;

			int count = 500000;
			DateTime start = DateTime.Now;
			for(int i = 0; i < count; i++) {
				int last = 0, current = 0;
				while((current = testStr.IndexOf(query, current)) != -1) {
					string x = testStr.Substring(last, current - last);
					current = last = current + 1;
				}
			}
			Console.WriteLine(DateTime.Now - start);
			Regex r = new Regex(query, RegexOptions.Compiled|RegexOptions.CultureInvariant);
			start = DateTime.Now;
			
			for(int i = 0; i < count; i++) {
				int last = 0, current = 0;
				Match match = r.Match(testStr, current);
				while(match.Success) {
					current = match.Index;
					string y = testStr.Substring(last, current - last);
					current = last = current + 1;
					match = r.Match(testStr, current);
				}
			}
			Console.WriteLine(DateTime.Now - start);
			start = DateTime.Now;
			Regex r2 = new Regex("^((?<group>.*),)*([^,]*)$", RegexOptions.Compiled | RegexOptions.CultureInvariant|RegexOptions.ExplicitCapture);
			for(int i = 0; i < count; i++) {
				foreach(Capture cap in r2.Match(testStr).Groups["group"].Captures) {
					string z = cap.Value;
				}
			}
			Console.WriteLine(DateTime.Now - start);
			/*start = DateTime.Now;
			for(int i = 0; i < count; i++) {
				foreach(string str in matches(testStr)) {
					string z = str;
				}
			}
			Console.WriteLine(DateTime.Now - start);*/
		}
	}
}
