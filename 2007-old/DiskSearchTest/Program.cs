using System;
using System.Collections.Generic;
using System.Text;
using System.Query;
using System.Xml.XLinq;
using System.Data.DLinq;
using System.IO;
using EamonExtensions.Filesystem;
using EamonExtensions.DebugTools;
using EamonExtensions;

namespace DiskSearchTest {
    class Program {
        static void Main(string[] args) {
            Console.Write("Building Index of");
            var argdirs = from arg in args select new DirectoryInfo(arg);
            foreach (var d in argdirs)
                Console.Write(" " + d.FullName);
            Console.WriteLine();

            DateTime dtA = DateTime.Now;
            FileInfo[] files = (from aDir in argdirs from file in aDir.DescendantFiles() select file).ToArray();
            string[] filenames = (from file in files select FuncUtil.Swallow(()=>file.FullName,()=>"####FILE-ERROR")).ToArray();
            DateTime dtB = DateTime.Now;
            Console.WriteLine("Index of "+files.Length+" files built in " + (dtB - dtA).TotalSeconds + " secs.");

            string input = "";
            while (input != "EXIT") {
                dtA= DateTime.Now;
                Console.WriteLine("==================RESULTS=================");
                string[] query = input.Split(' ');
                var matches = (from file in filenames where query.All(q => file.Contains(q)) select file);
                matches.Take(5).PrintAllDebug();
                dtB = DateTime.Now;
                Console.WriteLine("in " + (dtB - dtA).TotalSeconds + " secs.");
                Console.WriteLine();
                Console.Write("New Query or EXIT:");
                input = Console.ReadLine();

            }
        }
    }
}
