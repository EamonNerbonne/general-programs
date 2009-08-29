using System.IO;
using System.Linq;
using EmnExtensions.Filesystem;

namespace HwrDataModel {
    public static class HwrResources {
		static readonly DirectoryInfo HwrDir = new DirectoryInfo( new[] { @"D:\EamonLargeDocs\HWR", @"C:\Users\nerbonne\HWR" }.First(Directory.Exists));


		public static DirectoryInfo DataDir { get { return HwrDir.CreateSubdirectory("data"); } }
		public static FileInfo Symbols { get { return DataDir.GetRelativeFile("symbols.xml"); } }
		public static FileInfo CharWidthFile { get { return DataDir.GetRelativeFile("char-width.txt"); } }
		public static FileInfo LineAnnotFile { get { return DataDir.GetRelativeFile("line_annot.txt"); } }

		public static DirectoryInfo WordsGuessDir { get { return DataDir.CreateSubdirectory("words-guess"); } }
		public static DirectoryInfo WordsTrainDir { get { return DataDir.CreateSubdirectory("words-train"); } }
		public static DirectoryInfo ImageDir { get { return HwrDir.CreateSubdirectory("Original"); } }
		public static DirectoryInfo SymbolDir { get { return HwrDir.CreateSubdirectory("Symbols"); } }
    }
}
