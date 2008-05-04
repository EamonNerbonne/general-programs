using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.IO;
using System.Xml;
using System.Diagnostics;

namespace HWRsplitter {
    public interface IAsXml {
        XNode AsXml();
    }

    public abstract class ShearedBox {
        public double top;
        public double bottom;
        public double left;
        public double right;
        public double shear;
        public ShearedBox(){}
        public ShearedBox(double top, double bottom, double left, double right,double shear) {this.top=top;this.bottom=bottom;this.left=left;this.right=right;this.shear=shear;}
        public ShearedBox(XElement fromXml) {
            top=(double)fromXml.Attribute("top");
            bottom = (double)fromXml.Attribute("bottom");
            left = (double)fromXml.Attribute("left");
            right = (double)fromXml.Attribute("right");
            shear = (double)fromXml.Attribute("shear");
        }


        protected IEnumerable<XAttribute> MakeXAttrs() {
            yield return new XAttribute("top", top);
            yield return new XAttribute("bottom", bottom);
            yield return new XAttribute("left", left);
            yield return new XAttribute("right", right);
            yield return new XAttribute("shear", shear);
        }

    }
    public class Word:ShearedBox,IAsXml {
        public string text;
        public int no;
        public LengthEstimate? lengthEstimate;
        
        public Word(){}
        public Word(string text, int no, double top, double bottom, double left, double right, double shear)
            : base(top, bottom, left, right, shear) {
            this.text = text;
            this.no = no;
        }
        public Word(XElement fromXml):base(fromXml) {
            text = (string)fromXml.Attribute("text");
            no = (int)fromXml.Attribute("no");
        }


        public XNode AsXml() {
            return new XElement("Word",
                new XAttribute("no", no),
                base.MakeXAttrs(),
                new XAttribute("text", text)
                );
        }

    }

    public class TextLine : ShearedBox,IAsXml {
        public Word[] words;
        public int no;

        public TextLine(){}
        public TextLine(string text, int no, double top, double bottom, double left, double right, double shear, Dictionary<char, SymbolWidth> symbolWidths)
         :base(top,bottom,left,right,shear) {
            this.no=no;
            this.words = GuessWordsInString(text, symbolWidths).ToArray();
        }

        private LengthEstimate EstimateCharLength(char c, Dictionary<char, SymbolWidth> symbolWidths) {
            SymbolWidth sym;
            if (symbolWidths.TryGetValue(c, out sym))
                return sym.estimate;
            sym = symbolWidths[(char)1];
            return sym.estimate;
        }

        private LengthEstimate EstimateWordLength(string word, Dictionary<char, SymbolWidth> symbolWidths,  bool isFirst, bool isLast) {
            LengthEstimate estimate = EstimateCharLength(' ', symbolWidths);
            if (isFirst) estimate += EstimateCharLength((char)0, symbolWidths);
            if (isLast) estimate += EstimateCharLength((char)10, symbolWidths);
            foreach (char c in word) {
                estimate += EstimateCharLength(c, symbolWidths);
            }
            return estimate;
        }
        private IEnumerable<Word> GuessWordsInString(string text, Dictionary<char, SymbolWidth> symbolWidths) {
            int no=1;//"number" starts with 1!
            double width = right-left;
            var wordStrs = text.Split(new[]{' '}, StringSplitOptions.RemoveEmptyEntries);
            LengthEstimate[] lengthEstimates = wordStrs.Select((w, i) =>
                EstimateWordLength(w, symbolWidths, i == 0, i == wordStrs.Length - 1)).ToArray();
            var totalEstimate = lengthEstimates.Aggregate((a, b) => a + b);
            //ok, so we have a total line length and a per word estimate
            double estErr = totalEstimate.len - width;
            double correctionPerVar = -estErr / totalEstimate.var;
            double lengthToLeft = 0;
            for (int i = 0; i < wordStrs.Length;i++ ) {
                string wordStr = wordStrs[i];
                
                double lengthToRight = lengthToLeft+ lengthEstimates[i].len+lengthEstimates[i].var*correctionPerVar;
                yield return new Word(wordStr, no, top, bottom,
                    left + lengthToLeft,
                    left + lengthToRight,
                    shear) {
                     lengthEstimate=lengthEstimates[i]
                    };
                no++;
                lengthToLeft = lengthToRight;
            }
            Debug.Assert(Math.Abs(lengthToLeft - width) < 1, "math error");
        }
        public TextLine(XElement fromXml):base(fromXml) {
            no = (int)fromXml.Attribute("no");
            words = fromXml.Elements("Word").Select(xmlWord => new Word(xmlWord)).ToArray();
        }



        public XNode AsXml() {
            return new XElement("TextLine",
                new XAttribute("no", no),
                base.MakeXAttrs(),
                words.Select(word=>word.AsXml())
                    );
        }

    }

    public class WordsImage:IAsXml {
        public string name;
        public int pageNum;
        public TextLine[] textlines;
        public WordsImage() { }
        
        public WordsImage(XElement fromXml) {
            Init(fromXml);
        }
        public WordsImage(FileInfo file) {
            using (Stream stream = file.OpenRead())
            using (XmlReader xmlreader = XmlReader.Create(stream))
                Init(XDocument.Load(xmlreader).Root);

        }

        private void Init(XElement fromXml) {
            name = (string)fromXml.Attribute("name");
            pageNum = int.Parse(name.Substring(name.Length - 4, 4));
            textlines = fromXml.Elements("TextLine").Select(xmlTextLine => new TextLine(xmlTextLine)).ToArray();

        }
        public XNode AsXml() {
            return new XElement("Image",
                new XAttribute("name", name),
                textlines.Select(textline => textline.AsXml())
                );
        }

    }
}
