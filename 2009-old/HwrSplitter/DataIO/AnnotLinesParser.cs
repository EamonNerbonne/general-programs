using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace DataIO {
    public class AnnotLineSingle {
        public static Regex lineMatch = new Regex(@"^(?<Source>\w+)-(?<pageNum>\d+)-par-(?<par>\d+)-line-(?<line>\d+)\s+(?<top>\d+)\s+(?<bottom>\d+)\s+(?<left>\d+)\s+(?<right>\d+)\s+(?<text>.*)$", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Multiline);
        static readonly XmlReaderSettings settings = new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment };
        static string DeXmlize(string xmlString) {
            //this is probably not fast, but it's pretty safe.
            using (var reader = XmlReader.Create(new StringReader(xmlString), settings)) {
                if (reader.Read() && reader.NodeType == XmlNodeType.Text) {
                    return reader.Value;
                } else throw new InvalidDataException(string.Format("Expected a single valid xml-encoded string - not {0}", xmlString));
            }
        }
        public static IEnumerable<AnnotLineSingle> FromString(string str, Func<int, bool> pageFilter) {
            foreach (Match m in lineMatch.Matches(str)) {
                int pageNum = int.Parse(m.Groups["pageNum"].Value);
                if(pageFilter(pageNum))
                yield return new AnnotLineSingle() {
                    Source = m.Groups["Source"].Value,
                    pageNum = pageNum,
                    par = int.Parse(m.Groups["par"].Value),
                    line = int.Parse(m.Groups["line"].Value),
                    top = int.Parse(m.Groups["top"].Value),
                    bottom = int.Parse(m.Groups["bottom"].Value),
                    left = int.Parse(m.Groups["left"].Value),
                    right = int.Parse(m.Groups["right"].Value),
                    text = DeXmlize(m.Groups["text"].Value)
                };
            }
        }
        public string Source;
        public int pageNum;
        public int par;
        public int line;
        public int top;
        public int bottom;
        public int left;
        public int right;
        public string text;
    }

    public class AnnotLinesParser {//TODO: this should be 
        public AnnotLineSingle[] annotLines;
        public AnnotLinesParser(FileInfo file, Func<int, bool> pageFilter) {
            annotLines = AnnotLineSingle.FromString(file.OpenText().ReadToEnd(), pageFilter).ToArray();
        }
        public Dictionary<int, WordsImage> GuessWords(Dictionary<char, SymbolWidth> symbolWidths) {
            var wordsImages = //lazily evaluated query.
                from annotline in annotLines
                group annotline by annotline.pageNum into pagegroup
                select new WordsImage {
                    pageNum = pagegroup.Key,
                    name = string.Format("NL_HaNa_H2_7823_{0:D4}", pagegroup.Key),
                    textlines = (
                        from indexedLine in
                            (from annotline in pagegroup
                             orderby annotline.par, annotline.line
                             select annotline).Select((line, index) => new { Index = index + 1, Line = line })
                        where !indexedLine.Line.text.Contains("\\n") //filter out potential nonsense lines AFTER indexing
                        let line = indexedLine.Line
                        let height = line.bottom - line.top
                        select new TextLine(line.text, indexedLine.Index, line.top, line.bottom, line.left , line.right , 45,symbolWidths)
                    ).ToArray()//textlines in WordsImage
                };
            return wordsImages.ToDictionary(wordsImage => wordsImage.pageNum);

        }
        public static Dictionary<int, WordsImage> GetGuessWords(FileInfo file, Func<int, bool> pageFilter, Dictionary<char, SymbolWidth> symbolWidths) {
            return new AnnotLinesParser(file, pageFilter).GuessWords(symbolWidths);
        }
        public static WordsImage GetGuessWord(FileInfo file, int pageNum , Dictionary<char, SymbolWidth> symbolWidths) {
            return new AnnotLinesParser(file, x=>x==pageNum).GuessWords(symbolWidths)[pageNum];

        }

        public static AnnotLineSingle[] GetAnnotLines(FileInfo file) {
            return new AnnotLinesParser(file, x => true).annotLines;
        }
        public static AnnotLineSingle[] GetAnnotLines(FileInfo file, Func<int, bool> pageFilter) {
            return new AnnotLinesParser(file, pageFilter).annotLines;
        }

    }
}
