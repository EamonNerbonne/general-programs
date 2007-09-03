using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.AccessControl;

namespace FileSystemInfoDisplayer
{
    class Program
    {
        static void PrintGenericProperties(FileSystemInfo fs)
        {
            Console.WriteLine("ToString():    " + fs.ToString());
            Console.WriteLine("Name:          " + fs.Name);
            Console.WriteLine("FullName:      " + fs.FullName);
            Console.WriteLine("Extension:     " + fs.Extension);
            Console.WriteLine("CreationTime:  " + fs.CreationTime);
            Console.WriteLine("LastAccessTime:" + fs.LastAccessTime);
            Console.WriteLine("LastWriteTime: " + fs.LastWriteTime);
            Console.WriteLine("Attributes:    ");
            
            FileAttributes fsattr = fs.Attributes;
            foreach (FileAttributes attr in Enum.GetValues(typeof(FileAttributes)))
                Console.WriteLine("     *         " + attr.ToString() + ": " + ((attr & fsattr) == attr));
            
            /*if(fs is FileInfo) {
                FileSecurity sec =((FileInfo)fs).GetAccessControl(AccessControlSections.All);
                sec.GetAccessRules(true,true,
            }*/
        }

        static void Main(string[] args)
        {
            if (args.Length != 1) {
                Console.WriteLine("Usage: FileSystemInfoDisplayer [path]");
                return;
            }
            Console.WriteLine("Query: "+args[0]);
            bool fileE=File.Exists(args[0]),dirE=Directory.Exists(args[0]);
            Console.WriteLine("File.Exists: "+fileE);
            Console.WriteLine("Directory.Exists: "+dirE);
            if (!fileE && !dirE) return;
            FileInfo fi = fileE?new FileInfo(args[0]):null;
            DirectoryInfo di = dirE?new DirectoryInfo(args[0]):null;
            PrintGenericProperties(fileE?(FileSystemInfo)fi: di);
        }
    }
}
