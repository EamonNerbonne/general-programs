using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace HWRsplitter {
    public class Program {

        public const string DataPath = @"C:\Users\Eamon\eamonhome\docs-trunk\uni\2008-HandWritingRecognition\data";
        public const string ImgPath = @"C:\Users\Eamon\HWR";
        public static void Main(string[] args) { prog = new Program(DataPath, ImgPath); }
        public static Program prog;
        public LinesAnnot linesAnnot;
        public Program(string dataPath, string imgPath) {
            FileInfo lineAnnotFile = new FileInfo(Path.Combine(DataPath, "line_annot.txt"));
            linesAnnot = new LinesAnnot(lineAnnotFile,x=>true);
        }
    }
}
