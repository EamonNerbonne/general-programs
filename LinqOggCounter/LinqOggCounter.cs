using System;
using System.Collections.Generic;
using System.Text;
using System.Query;
using System.Xml.XLinq;
using System.Data.DLinq;
using System.IO;

namespace LinqOggCounter {
    class LinqOggCounter {
        static IEnumerable<DirectoryInfo> GetDirs(DirectoryInfo dir) {
            //both expresseions are equivalent
            return Sequence.Repeat(dir, 1).Concat( from d in dir.GetDirectories() from s in GetDirs(d) select s);
            //return Sequence.Repeat(dir, 1).Concat(dir.GetDirectories().SelectMany<DirectoryInfo,DirectoryInfo>(GetDirs));
        }
        
        static void Main(string[] args) {
            var di = new DirectoryInfo(args[0]);
            if (!di.Exists) throw new FileNotFoundException("Argument passed not found.", args[0]);
            var list = (
                from d in GetDirs(di) from f in d.GetFiles()
                where f.Extension.ToLower() == ".ogg" 
                select  new {Name = f.Name, Size = f.Length}
                ).ToArray();
            foreach (var item in list) Console.WriteLine("Name = " + item.Name + ", KB = " + item.Size / 1024);
            Console.WriteLine(list.Sum(entry => 1) + " Files.");
            Console.WriteLine(list.Sum(entry => entry.Size)/1024/1024 + " MB.");
            Console.ReadLine();
        }
    }
}
