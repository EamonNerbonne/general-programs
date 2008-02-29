using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EamonExtensionsLinq;
using SongDataLib;
using System.IO;

namespace StringPerformanceTests
{
	class DataCreator
	{
		static void OnSongLoad(ISongData data, double est) {
			var songdata = data as SongData;
			if(songdata == null) return;
			foreach(var str in new string[] { songdata.performer, songdata.title, songdata.album, songdata.comment }.Where(s => s != null && s.Length > 0))
				writer.WriteLine(str);
		}
		static TextWriter writer;
		public static void WriteTo(FileInfo fi) {
			var config = new SongDatabaseConfigFile(false);
			using(var s = fi.OpenWrite()) {
				using(writer = new StreamWriter(s)) {
					config.Load(OnSongLoad);
				}
			}
		}
	}

	class FastString
	{
		private readonly int hashcode;
		private readonly string str;
		public FastString(string str) {
			hashcode = str.GetHashCode();
			this.str = str;
		}
		public override int GetHashCode() {
			return hashcode;
		}
		public override bool Equals(object obj) {
			if(!(obj is FastString)) return false;
			FastString other = (FastString)obj;
			if(hashcode != other.hashcode) return false;
			return str == other.str;
		}
		public static explicit operator string(FastString fstr) {
			return fstr.str;
		}
		public static explicit operator FastString(string fstr) {
			return new FastString(fstr);
		}
		public static bool operator ==(FastString a, FastString b) {
			return a.hashcode == b.hashcode && a.str == b.str;
		}
		public static bool operator !=(FastString a, FastString b) {
			return a.hashcode != b.hashcode || a.str != b.str;
		}
	}


	class StringPerformanceTests
	{
		public static byte[] ReadFile(FileInfo fi) {
			byte[] retval = new byte[fi.Length];
			using(var readstream = fi.OpenRead()) {
				int pos=0;
				int read=0;
				while(pos != retval.Length) {
					read = readstream.Read(retval, pos, retval.Length-pos);
					if(read <= 0) break;
					pos += read;
				}
				if(pos != retval.Length) {
					Array.Resize(ref retval, pos);
				}
			}
			return retval;
		}

		static byte[] RawData;
		static string RawString;
		static string[] RawLines;
		static string[] InternedLines;
		static FastString[] FastLines;
		static string Mylo;
		static string InternedMylo;
		static FastString FastMylo;

		const int DecodeRuns = 100;
		const int AddRuns = 500;
		const int LinqRuns = 20;
		const int ToLowerRuns = 20;
		const int NothingRuns = 100000000;

		static int MyloCount;



		static void Main(string[] args) {
			NiceTimer timer = new NiceTimer("Initializing...");
			FileInfo file = new FileInfo(args[0]);
			if(!file.Exists) {	DataCreator.WriteTo(file);	}
			
			Console.WriteLine("Loading file...");
			RawData = ReadFile(file);
			RawString = Encoding.UTF8.GetString(RawData);
			RawLines = RawString.Split('\n');
			InternedLines = RawLines.Select(line => string.Intern(line)).ToArray();
			Mylo = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes("Mylo"));
			InternedMylo = string.Intern("Mylo");
			FastMylo = (FastString)"Mylo";
			FastLines = RawLines.Select(line => (FastString)line).ToArray();
			MyloCount = 0; foreach(var s in RawLines) if(s == "Mylo") MyloCount++;
			Console.WriteLine("Read {0} KB, {1} chars, {2} lines.", RawData.Length >> 10,RawString.Length,RawLines.Length);


			NiceTimer.TimeAction("Nothing", NothingRuns, delegate() { });
			NiceTimer.TimeAction("LINQ Sum", LinqRuns, delegate() {				RawData.Sum(b => (int)b);			});
			NiceTimer.TimeAction("Foreach Summing", AddRuns, delegate() { int sum = 0; foreach(int i in RawData)sum += i; });


			NiceTimer.TimeAction("Decoding UTF8", DecodeRuns, delegate() {				Encoding.UTF8.GetString(RawData);			});
			NiceTimer.TimeAction("Encoding UTF8", DecodeRuns, delegate() { Encoding.UTF8.GetBytes(RawString); });

			NiceTimer.TimeAction("StreamReader/MemoryStream", DecodeRuns, delegate() { 				new StreamReader(new MemoryStream(RawData)).ReadToEnd();			});

//			NiceTimer.TimeAction("ToUpper()", ToLowerRuns, delegate() {				RawString.ToUpper();			});
			//NiceTimer.TimeAction("ToLowerInvariant()", ToLowerRuns, delegate() {				RawString.ToLowerInvariant();			});
			//NiceTimer.TimeAction("ToUpperInvariant()", ToLowerRuns, delegate() {				RawString.ToUpperInvariant();			});

			NiceTimer.TimeAction("ToLower()", ToLowerRuns, delegate() { RawString.ToLower(); });
			NiceTimer.TimeAction("per line-ToLower()", ToLowerRuns, delegate() { foreach(var s in RawLines) s.ToLower(); });
			NiceTimer.TimeAction("Normalize(KD)", 10, delegate() { RawString.Normalize(NormalizationForm.FormKD); });
			NiceTimer.TimeAction("per line-Normalize(KD)", 10, delegate() { foreach(var s in RawLines) s.Normalize(NormalizationForm.FormKD); });
			NiceTimer.TimeAction("EEL:MakeSafe", 10, delegate() { EamonExtensionsLinq.Text.Canonicalize.MakeSafe(RawString); });
			NiceTimer.TimeAction("per line-EEL:MakeSafe", 10, delegate() { foreach(var s in RawLines) EamonExtensionsLinq.Text.Canonicalize.MakeSafe(s); });
			NiceTimer.TimeAction("EEL:Basic Canonicalization", 10, delegate() { EamonExtensionsLinq.Text.Canonicalize.Basic(RawString); });
			NiceTimer.TimeAction("per line-EEL:Basic Canonicalization", 10, delegate() { foreach(var s in RawLines) EamonExtensionsLinq.Text.Canonicalize.Basic(s); });
			NiceTimer.TimeAction("SongUtil:Search Canonicalization", 10, delegate() { SongUtil.CanonicalizedSearchStr(RawString); });
			NiceTimer.TimeAction("per line-SongUtil:Search Canonicalization", 10, delegate() { foreach(var s in RawLines)SongUtil.CanonicalizedSearchStr(s); });

			NiceTimer.TimeAction("FastString Compare to FastMylo", 1000, delegate() { int ignore = 0; foreach(var s in FastLines) if(s == FastMylo) ignore++; if(ignore != MyloCount)throw new Exception(); });

			NiceTimer.TimeAction("GetHashCode", 500, delegate() { RawString.GetHashCode(); });
			NiceTimer.TimeAction("per line-GetHashCode", 500, delegate() { foreach(var s in RawLines) s.GetHashCode(); });
			NiceTimer.TimeAction("per line (interned)-GetHashCode", 500, delegate() { foreach(var s in InternedLines) s.GetHashCode(); });

			NiceTimer.TimeAction("Compare to 'Mylo'", 500, delegate() { int ignore = 0; foreach(var s in RawLines) if(s == "Mylo") ignore++; if(ignore != MyloCount)throw new Exception(); });
			NiceTimer.TimeAction("Intered Compare to 'Mylo'", 500, delegate() { int ignore = 0; foreach(var s in InternedLines) if(s == "Mylo") ignore++; if(ignore != MyloCount)throw new Exception(); });

			NiceTimer.TimeAction("Compare to Mylo", 500, delegate() { int ignore = 0; foreach(var s in RawLines) if(s == Mylo) ignore++; if(ignore != MyloCount)throw new Exception(); });
			NiceTimer.TimeAction("Intered Compare to Mylo", 500, delegate() { int ignore = 0; foreach(var s in InternedLines) if(s == Mylo) ignore++; if(ignore != MyloCount)throw new Exception(); });

			NiceTimer.TimeAction("Compare to InternedMylo", 500, delegate() { int ignore = 0; foreach(var s in RawLines) if(s == InternedMylo) ignore++; if(ignore != MyloCount)throw new Exception(); });
			NiceTimer.TimeAction("Intered Compare to InternedMylo", 500, delegate() { int ignore = 0; foreach(var s in InternedLines) if(s == InternedMylo) ignore++; if(ignore != MyloCount)throw new Exception(); });

		}
	}
}
