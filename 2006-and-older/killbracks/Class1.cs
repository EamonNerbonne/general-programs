using System;
using System.IO;
namespace killbracks {
    class Class1 {
        static void Main(string[] args) {
            if(args.Length!=1) {
                Console.WriteLine("Accepts only one param a dir to scan and kill brackets off mp3's");
                return;
            }
            DirectoryInfo di =new DirectoryInfo(args[0]);
            if(!di.Exists) {
                Console.WriteLine("Non existant dir");
                return;
            }
            foreach(FileInfo fi in di.GetFiles("MBZ* (?).mp3")) {
                try {
                    fi.MoveTo(fi.Name.Substring(0,fi.Name.Length-8)+".mp3");
                } catch {
                    Console.WriteLine(fi.Name + ": didn't rename");
                }
            }
        }
    }
}
