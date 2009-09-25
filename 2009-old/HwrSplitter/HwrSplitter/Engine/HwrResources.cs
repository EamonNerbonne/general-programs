using System.IO;
using System.Linq;
using EmnExtensions.Filesystem;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using HwrDataModel;

namespace HwrSplitter.Engine
{
	public static class HwrResources
	{
		static readonly DirectoryInfo HwrDir = new DirectoryInfo(new[] { @"D:\EamonLargeDocs\HWR", @"C:\Users\nerbonne\HWR" }.First(Directory.Exists));
		static Regex imageFilenamePattern = new Regex(@"^NL_HaNa_H2_7823_(?<num>\d+)\.tif$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);


		public static DirectoryInfo DataDir { get { return HwrDir.CreateSubdirectory("data"); } }
		public static FileInfo CharWidthFile { get { return DataDir.GetRelativeFile("char-width.txt"); } }
		public static FileInfo LineAnnotFile { get { return DataDir.GetRelativeFile("line_annot.txt"); } }

		public static DirectoryInfo WordsGuessDir { get { return DataDir.CreateSubdirectory("words-guess"); } }
		public static FileInfo WordsGuessFile(int pageNum) { return WordsGuessDir.GetRelativeFile("NL_HaNa_H2_7823_" + pageNum.ToString("0000") + ".wordsguess"); }
		public static DirectoryInfo WordsTrainDir { get { return DataDir.CreateSubdirectory("words-train"); } }
		public static HwrTextPage WordsTrainingExample(int pageNum) {
			FileInfo wordFile = WordsTrainDir.GetRelativeFile("NL_HaNa_H2_7823_" + pageNum.ToString("0000") + ".words");
			return wordFile.Exists ? new HwrTextPage(wordFile, HwrEndpointStatus.Manual) : null;
		}
		public static IEnumerable<HwrTextPage> WordsTrainingExamples { get { return WordsTrainDir.GetFiles("NL_HaNa_H2_7823_*.words").Select(file => new HwrTextPage(file, HwrEndpointStatus.Manual)); } }

		public static DirectoryInfo ImageDir { get { return HwrDir.CreateSubdirectory("Original"); } }
		public static FileInfo[] ImageFiles { get { return ImageDir.GetFiles("NL_HaNa_H2_7823_*.tif"); } }
		public static HwrPageImage ImageFile(int pageNum) { return  new HwrPageImage(ImageDir.GetRelativeFile("NL_HaNa_H2_7823_" + pageNum.ToString("0000") + ".tif")); }
		public static IEnumerable<int> ImagePages { get { return ImageFiles.Select(fi => imageFilenamePattern.Match(fi.Name)).Where(m => m.Success).Select(m => int.Parse(m.Groups["num"].Value)); } }
		public static DirectoryInfo SymbolOutputDir { get { return HwrDir.CreateSubdirectory("Symbols"); } }


	}
}
