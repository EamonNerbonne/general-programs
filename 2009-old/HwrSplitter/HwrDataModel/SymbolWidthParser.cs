using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HwrDataModel
{
	public struct LengthEstimate
	{
		public readonly double len, var;
		public static LengthEstimate operator +(LengthEstimate a, LengthEstimate b) { return new LengthEstimate(a.len + b.len, a.var + b.var); }
		public LengthEstimate(double len, double var) { this.len = len; this.var = var; }
		public double stddev { get { return Math.Sqrt(var); } }
	}

	public class SymbolWidth
	{
		public readonly char c;//by agreement, char 0 is str-start, char 1 is unknown, char 10 is str-end, and char 32 is space
		public readonly uint code;
		public readonly LengthEstimate estimate;
		public SymbolWidth(char c, uint code, LengthEstimate estimate) { this.c = c; this.code = code; this.estimate = estimate; }
		public SymbolWidth(char c, LengthEstimate estimate) { this.c = c; this.code = uint.MaxValue; this.estimate = estimate; }
		public SymbolWidth WithCode(uint newcode) { if (code != uint.MaxValue) throw new ApplicationException("code already set"); else return new SymbolWidth(c, newcode, estimate); }
	}


	public static class SymbolWidthParser
	{

		/// <summary>
		/// Special chars: 
		/// 0 - start of line
		/// 1 - unknown char
		/// 10 - end of line
		/// 32 - general word spacing (add this to EVERY word once).
		/// </summary>
		/// <param name="file"></param>
		/// <returns></returns>
		public static SymbolWidth[] Parse(FileInfo file) {
			//TODO: known limitation: culture-sensitive parsing.
			Dictionary<char, SymbolWidth> retval;
			using (var reader = file.OpenText())
				retval = reader.ReadToEnd()
						.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
						.Select(line => line.Split(',').Select(part => part.Trim()).ToArray())
						.Where(parts => parts.Length == 4)//no empty lines!
						.Select(parts => new SymbolWidth(
							(char)int.Parse(parts[0]),
							new LengthEstimate( double.Parse(parts[1]), double.Parse(parts[2]))
						))
						.ToDictionary(symbolWidth => symbolWidth.c);
			
			retval[(char)1] = retval.Values.Any()
				? new SymbolWidth(
					(char)1,
					new LengthEstimate(retval.Values.Select(sw => sw.estimate.len).Average(), retval.Values.Select(sw => sw.estimate.var).Average() * 9 )
				    )
				: new SymbolWidth(
					(char)1,
					new LengthEstimate( 50, 1000)
					);

			return retval.Values.OrderBy(c => c.c).Select((symCode, i) => symCode.WithCode((uint)i)).ToArray();
		}
	}
}
