using System;
using System.Collections.Generic;
using System.Text;
using System.Query;
using System.Xml.XLinq;
using TagLib;
using System.IO;
using SongDataLib;
using EamonExtensions;
using EamonExtensions.Filesystem;

namespace TagLibSharp_LINQ {
    class Program {
        static string makesafe(string data) { return new string((data ?? "").Replace('\t', ' ').Replace('\n', ' ').Where(c=>c >= ' ').ToArray()); }
        static string makesafe(string[] data) { return data == null ? "" : makesafe(string.Join(", ", data)); }
        static string makesafe(uint data) { return data.ToString(); }
        static string makesafe(int data) { return data.ToString(); }

        static void Main(string[] args) {
            StreamWriter outputlog = null;
            StreamWriter errorlog = null;
            try {
                DateTime start = DateTime.Now;
                DateTime prev = DateTime.Now;
                int filecount = 0;
                Console.WriteLine("Iterating over: " + string.Join(", ", args.Skip(1).ToArray()) + " into " + args[0]);
                if (System.IO.File.Exists(args[0]))
                    System.IO.File.Delete(args[0]);
                if (System.IO.File.Exists(args[0] + ".err"))
                    System.IO.File.Delete(args[0] + ".err");
                outputlog = new StreamWriter(new FileInfo(args[0]).OpenWrite());
                errorlog = new StreamWriter(new FileInfo(args[0] + ".err").OpenWrite());

                var files = (from s in 
                            (from arg in args.Skip(1).ToArray() 
                             from file in new DirectoryInfo(arg).DescendantFiles() 
                             select FuncUtil.Swallow(()=>new SongData(file),()=>null))
                         where s != null
                         select s);

                foreach (SongData file in files) {
                    try {
                        XElement song = file.ConvertToXml();
                        outputlog.WriteLine(song.Xml);
                        filecount++;
                        if ((DateTime.Now - prev).TotalSeconds >= 1.0) {
                            prev = DateTime.Now;
                            double secs = (DateTime.Now - start).TotalSeconds;
                            Console.WriteLine(filecount + " songs indexed in " + secs + (secs == 0 ? "" : ", average fps:" + filecount / secs));
                        }
                    } catch (Exception e) {
                        errorlog.WriteLine("ERROR in file: " + file.filepath);
                        errorlog.WriteLine(e.ToString());
                        errorlog.WriteLine(e.StackTrace);
                    }
                }
                Console.WriteLine("COMPLETE!!!! --");
                double finaldur = (DateTime.Now - start).TotalSeconds;
                Console.WriteLine(filecount + " songs indexed in " + finaldur + (finaldur == 0 ? "" : ", average fps:" + filecount / finaldur));
            } catch (Exception e) {
                if (errorlog != null)
                    errorlog.WriteLine("FATAL ERROR!!!");
                errorlog.WriteLine(e.ToString());
                errorlog.WriteLine(e.StackTrace);
                throw;
            } finally {
                if (outputlog != null)
                    outputlog.Close();
                if (errorlog != null)
                    errorlog.Close();
            }
        }
    }
}
