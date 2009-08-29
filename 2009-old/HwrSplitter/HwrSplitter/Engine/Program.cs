using System.IO;
using System.Linq;

namespace HwrDataModel {
    public static class Program {
		static readonly DirectoryInfo HwrDir = new DirectoryInfo( new[] { @"D:\EamonLargeDocs\HWR", @"C:\Users\nerbonne\HWR" }.First(Directory.Exists));


		public static string DataPath { get { return  HwrDir.CreateSubdirectory("data").FullName; } }
		public static string ImgPath { get { return HwrDir.CreateSubdirectory("Original").FullName; } }
		public static DirectoryInfo SymbolPath { get { return HwrDir.CreateSubdirectory("Symbols"); } }
    }
}
