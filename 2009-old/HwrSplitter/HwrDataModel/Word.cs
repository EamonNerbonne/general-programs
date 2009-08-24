using System.Xml.Linq;
using System.Collections.Generic;

namespace DataIO
{
    public class Word : ShearedBox, IAsXml
    {
        public string text;
        public int no;
        public TrackStatus leftStat, rightStat, topStat, botStat;
        public LengthEstimate symbolBasedLength;
        public object guiTag;
        // public double cost = double.NaN;
        public Word() { 
            leftStat= rightStat= topStat= botStat=TrackStatus.Uninitialized;
        }
        public Word(string text, int no, double top, double bottom, double left, double right, double shear)
            : base(top, bottom, left, right, shear) {
            this.text = text;
            this.no = no;
            leftStat = rightStat = topStat = botStat = TrackStatus.Initialized;
        }
		public void EstimateLength(Dictionary<char, SymbolWidth> symbolWidths) {
			symbolBasedLength = EstimateWordLength(text, symbolWidths);
		}

        public Word(Word toCopy)
            : this(toCopy.text, toCopy.no, toCopy.top, toCopy.bottom, toCopy.left, toCopy.right, toCopy.shear) {
            leftStat= toCopy.leftStat;
            rightStat=toCopy.rightStat;
            topStat=toCopy.topStat;
            botStat=toCopy.botStat;

            symbolBasedLength = toCopy.symbolBasedLength;
        }
        public Word(XElement fromXml)
            : base(fromXml) {
            text = (string)fromXml.Attribute("text");
            no = (int)fromXml.Attribute("no");
            leftStat = rightStat = topStat = botStat = TrackStatus.Calculated;//TODO, these should be saved in the XML

        }


        public XNode AsXml() {
            return new XElement("Word",
                new XAttribute("no", no),
                base.MakeXAttrs(),
                new XAttribute("text", text)
                );
        }

		static LengthEstimate EstimateWordLength(string word, Dictionary<char, SymbolWidth> symbolWidths) {
			LengthEstimate estimate = EstimateCharLength(' ', symbolWidths);
			foreach (char c in word) 
				estimate += EstimateCharLength(c, symbolWidths);
			
			return estimate;
		}

		private static LengthEstimate EstimateCharLength(char c, Dictionary<char, SymbolWidth> symbolWidths) {
			SymbolWidth sym;
			if (symbolWidths.TryGetValue(c, out sym))
				return sym.estimate;
			sym = symbolWidths[(char)1];
			return sym.estimate;
		}

    }
}
