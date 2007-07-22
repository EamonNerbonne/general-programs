using System;
using System.Collections.Generic;
using System.Text;
using System.Query;
using System.Xml.XLinq;
using System.Data.DLinq;
using EamonExtensions.Filesystem;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
namespace LINQConsoleApplication2 {
    class Program {
        static void Main(string[] args) {
            string tmpPhotoPath = args[0];
            Regex number = new Regex(@"\d\d\d\d",RegexOptions.Compiled);
            Dictionary<string, string> dict = new Dictionary<string, string>();
            
            foreach(string name in (from s in (from f in new DirectoryInfo(tmpPhotoPath).TryGetFiles() where f.Extension.ToLower()==".jpg" select f.Name) select number.Match(s).Value))
                dict[name]=name;
            foreach (FileInfo file in new DirectoryInfo(args[0]).TryGetFiles()) {
                string newname=null;
                if(dict.ContainsKey(number.Match(file.Name).Value) && file.Extension.ToLower() == ".delete")
                    newname = file.Name.Substring(0,file.Name.Length - ".delete".Length);
                else if (!dict.ContainsKey(number.Match(file.Name).Value) && file.Extension.ToLower() == ".cr2")
                    newname = file.Name + ".delete";

               if (newname!= null)
                   file.MoveTo(Path.Combine(file.Directory.FullName, newname));
            }
        }
    }
}
