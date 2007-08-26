using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Xml.Linq;
using TagLib;
using System.IO;
using SongDataLib;
using EamonExtensionsLinq;
using EamonExtensionsLinq.Filesystem;

namespace TagLibSharp_LINQ
{
	class Program
	{
		static string makesafe(string data) { return new string((data ?? "").Replace('\t', ' ').Replace('\n', ' ').Where(c => c >= ' ').ToArray()); }
		static string makesafe(string[] data) { return data == null ? "" : makesafe(string.Join(", ", data)); }
		static string makesafe(uint data) { return data.ToString(); }
		static string makesafe(int data) { return data.ToString(); }

		static void Main(string[] args) {
			FileInfo targetFile = new FileInfo(args[0]);
			FileInfo tmpFile = new FileInfo(args[0] + ".tmp");
			FileInfo errFile = new FileInfo(args[0] + ".err");


			StreamWriter outputlog = null;
			StreamWriter errorlog = null;
			try {
				DateTime start = DateTime.Now;
				DateTime prev = DateTime.Now;
				int filecount = 0;
				Console.WriteLine("Iterating over: " + string.Join(", ", args.Skip(1).ToArray()) + " into " + args[0]);
				if(tmpFile.Exists) tmpFile.Delete();
				if(errFile.Exists) errFile.Delete();
				outputlog = new StreamWriter(tmpFile.OpenWrite());

				var files = (from s in
									 (from arg in args.Skip(1)
									  from file in new DirectoryInfo(arg).DescendantFiles()
									  select FuncUtil.Swallow(() => SongDataFactory.LoadFromFile(file), () => null))
								 where s != null
								 select s);

				foreach(ISongData songdata in files) {
					try {
						XElement song = songdata.ConvertToXml();
						outputlog.WriteLine(song.ToString());
						filecount++;
						if((DateTime.Now - prev).TotalSeconds >= 1.0) {
							prev = DateTime.Now;
							double secs = (DateTime.Now - start).TotalSeconds;
							Console.WriteLine(filecount + " songs indexed in " + secs + (secs == 0 ? "" : ", average fps:" + filecount / secs));
						}
					} catch(Exception e) {
						if(errorlog == null) errorlog = new StreamWriter(errFile.OpenWrite());
						errorlog.WriteLine("ERROR in file: " + songdata.Uri);
						errorlog.WriteLine(e.ToString());
						errorlog.WriteLine(e.StackTrace);
					}
				}
				Console.WriteLine("COMPLETE!!!! --");
				double finaldur = (DateTime.Now - start).TotalSeconds;
				Console.WriteLine(filecount + " songs indexed in " + finaldur + (finaldur == 0 ? "" : ", average fps:" + filecount / finaldur));
				outputlog.Flush();
				outputlog.Close();
				outputlog = null;
				targetFile.Delete();
				tmpFile.MoveTo(targetFile.FullName);//probably not multi-threaded secure... but like... we're not going to be safely generating the same DB simultaneously anyhow.
			} catch(Exception e) {
				if(errorlog != null) {
					errorlog.WriteLine("FATAL ERROR!!!");
					errorlog.WriteLine(e.ToString());
					errorlog.WriteLine(e.StackTrace);
				}
				throw;
			} finally {
				if(outputlog != null) outputlog.Close();
				if(errorlog != null) errorlog.Close();
			}
		}
	}
}