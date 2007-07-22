using System;
using System.Collections.Generic;
using System.Text;
using System.Query;
using System.Xml.XLinq;
using System.Data.DLinq;
using System.IO;
using EamonExtensions.Filesystem;

namespace M3UCollector {
    class M3UCollector {
        static void Main(string[] args) {
            foreach (FileInfo m3ufile in from filename in args select new FileInfo(Path.Combine(System.Environment.CurrentDirectory,filename))) {
                
                var targetDir = m3ufile.Directory.CreateSubdirectory(m3ufile.Name + ".files");
                foreach (FileInfo file in from line in m3ufile.GetLines() where !line.StartsWith("#") select new FileInfo(line)) {
                    var newfile = new FileInfo(Path.Combine(targetDir.FullName,file.Name));
                    Console.Write(file.FullName + ": ");
                    if (file.Exists && !newfile.Exists) {
                        file.CopyTo(newfile.FullName);
                        Console.WriteLine("stored.");
                    } else {
                        Console.WriteLine(newfile.Exists ? "already stored." : "not found.");
                    }
                }
            }
        }
    }
}
