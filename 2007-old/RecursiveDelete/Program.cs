using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using EamonExtensionsLinq.Filesystem;
namespace ReqDelete
{
	class Program
	{
		static void Main(string[] args) {
			if(args.Length != 1) Console.WriteLine("Needs one parameter: the dir to delete");
			else {
				DirectoryInfo toDelete = new DirectoryInfo(args[0]);
				if(!toDelete.Exists) {
					Console.WriteLine("The specified directory doesn't exist: " + toDelete.FullName);
				} else {
					Console.WriteLine("Are you sure you want to recursively delete " + toDelete.FullName + "? (type 'yes' and press enter to confirm).");
					string confirm = Console.ReadLine().ToLowerInvariant();
					if(confirm != "yes") {
						Console.WriteLine("NOT confirmed, NOT deleting!");
					} else {
						RecursiveDelete(toDelete);
					}
				}
			}
		}

		static void RecursiveDelete(DirectoryInfo dir) {
			foreach(DirectoryInfo kid in dir.TryGetDirectories()) {
				if((kid.Attributes & FileAttributes.ReparsePoint) == 0) {
					RecursiveDelete(kid);
				} else {
					try { kid.Delete(false); } catch { Console.WriteLine("Couldn't delete " + kid.FullName); }
				}
			}
			foreach(FileInfo file in dir.TryGetFiles()) {
				try { file.Delete(); } catch { Console.WriteLine("Couldn't delete " + file.FullName); }
			}
			try { dir.Delete(false); } catch { Console.WriteLine("Couldn't delete " + dir.FullName); }
		}
	}
}
