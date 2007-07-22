using System;
using System.Collections.Generic;
using System.Text;
using System.Query;
using System.Xml.XLinq;
using System.Data.DLinq;
using EamonExtensions.Filesystem;
using EamonExtensions.DebugTools;
using System.IO;


namespace HideFromPicasa {
    class Program {
        static void Main(string[] args) {
            DirectoryInfo dir = new DirectoryInfo(args[0]);
            var files2make = from d in dir.DescendantDirs() 
                              where d.Name == "slides" || d.Name == "thumbs" 
                              select d.GetRelativeFile("picasa.ini");
            files2make = files2make.ToArray();
            foreach (FileInfo picasaini in files2make) {
                    StreamWriter writer =  picasaini.Exists ? new StreamWriter(picasaini.OpenWrite()) : picasaini.CreateText();
                writer.WriteLine(
@"[Picasa]
P2category=Hidden Folders
[encoding]
utf8=1"
                );
                writer.Flush();
                writer.Close();
                picasaini.Attributes = FileAttributes.Hidden;

            }
        }
    }
}
