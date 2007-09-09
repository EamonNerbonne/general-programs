using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.AccessControl;
using System.Reflection;
using EamonExtensionsLinq.DebugTools;

namespace FileSystemInfoDisplayer
{
	class Program
	{
		static void PrintGenericProperties(FileSystemInfo fs) {
			Console.WriteLine("ToString():    " + fs.ToString());
			Console.WriteLine("Name:          " + fs.Name);
			Console.WriteLine("FullName:      " + fs.FullName);
			Console.WriteLine("Extension:     " + fs.Extension);
			Console.WriteLine("CreationTime:  " + fs.CreationTime);
			Console.WriteLine("LastAccessTime:" + fs.LastAccessTime);
			Console.WriteLine("LastWriteTime: " + fs.LastWriteTime);
			Console.WriteLine("Attributes:    ");

			FileAttributes fsattr = fs.Attributes;
			foreach(FileAttributes attr in Enum.GetValues(typeof(FileAttributes)))
				Console.WriteLine("     *         " + attr.ToString() + ": " + ((attr & fsattr) == attr));

			/*if(fs is FileInfo) {
				 FileSecurity sec =((FileInfo)fs).GetAccessControl(AccessControlSections.All);
				 sec.GetAccessRules(true,true,
			}*/
		}

		static void Main(string[] args) {
			string[] queries = null;
			if(args.Length == 0) {
				Console.WriteLine("Assembly+Environment info:");
				FileInfo assFile = new FileInfo(Assembly.GetExecutingAssembly().PrintProperties("Assembly").Location);
				queries = new string[]{assFile.FullName};
				foreach(Environment.SpecialFolder specFolder in Enum.GetValues(typeof(Environment.SpecialFolder))) {
					Console.WriteLine(specFolder.ToString() + " = " + Environment.GetFolderPath(specFolder));
				}

			} else { queries = args; }
			foreach(var query in queries) {
				Console.WriteLine("Query: " + query);
				bool fileE = File.Exists(query), dirE = Directory.Exists(query);
				Console.WriteLine("File.Exists: " + fileE);
				Console.WriteLine("Directory.Exists: " + dirE);
				if(!fileE && !dirE) return;
				FileInfo fi = fileE ? new FileInfo(query) : null;
				DirectoryInfo di = dirE ? new DirectoryInfo(query) : null;
				PrintGenericProperties(fileE ? (FileSystemInfo)fi : di);
			}
		}
	}
}
