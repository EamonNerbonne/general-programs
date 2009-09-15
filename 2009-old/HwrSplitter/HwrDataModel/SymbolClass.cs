using System;

namespace HwrDataModel
{
	public class SymbolClass
	{
		char? letter;//by agreement, char 0 is str-start, char 1 is unknown, char 10 is str-end, and char 32 is space
		uint? code;
		public char Letter { get { return letter.Value; } set { if (letter.HasValue && letter.Value != value) throw new ApplicationException("letter already set"); else letter = value; } }
		public string LetterReadable {
			get { return letter.HasValue ? (letter.Value <= ' ' ? ((int)letter.Value).ToString() : "'" + letter.Value.ToString() + "'") : "<null>"; }
			set {
				char newVal = value.StartsWith("'")
					? value[1]
					: (char)int.Parse(value);
				Letter = newVal;
			}
		}

		public uint Code { get { return code.Value; } set { if (code.HasValue) throw new ApplicationException("code already set"); code = value; } }
		public GaussianEstimate Length { get; set; }
		public FeatureDistributionEstimate[][] SubPhase { get; set; }
		public SymbolClass() { }
		public SymbolClass(char c, GaussianEstimate lengthEstimate) { this.Letter = c; this.Length = lengthEstimate; }
		public SymbolClass(char c, uint code, GaussianEstimate lengthEstimate) { this.Letter = c; this.Code = code; this.Length = lengthEstimate; }
	}
}
