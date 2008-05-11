using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace HWRsplitter {
    public class Program {

        public const string DataPath = @"C:\Users\Eamon\eamonhome\docs-trunk\uni\2008-HandWritingRecognition\data";
        public const string ImgPath = @"C:\Users\Eamon\HWR\Original";
        public static void Main(string[] args) { prog = new Program(DataPath, ImgPath); }
        public static Program prog;
        public AnnotLinesParser linesAnnot;
        public Program(string dataPath, string imgPath) {
            FileInfo lineAnnotFile = new FileInfo(Path.Combine(DataPath, "line_annot.txt"));
            linesAnnot = new AnnotLinesParser(lineAnnotFile,x=>true);
        }
    }
}
