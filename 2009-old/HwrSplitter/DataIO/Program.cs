using System.IO;
using System.Linq;

namespace DataIO {
    public class Program {

		public readonly static string[] DataPaths = new[] { @"D:\EamonLargeDocs\HWR\data", @"C:\Users\nerbonne\HWR\data" };
		public static string DataPath { get { return DataPaths.First(path => Directory.Exists(path)); } }
		public readonly static string[] ImgPaths = new[] { @"D:\EamonLargeDocs\HWR\Original", @"C:\Users\nerbonne\HWR\Original" };
		public static string ImgPath { get { return ImgPaths.First(path => Directory.Exists(path)); } }
        public static void Main(string[] args) { prog = new Program(DataPath, ImgPath); }
        public static Program prog;
        public AnnotLinesParser linesAnnot;
        public Program(string dataPath, string imgPath) {
            FileInfo lineAnnotFile = new FileInfo(Path.Combine(DataPath, "line_annot.txt"));
            linesAnnot = new AnnotLinesParser(lineAnnotFile,x=>true);
        }
    }
}
