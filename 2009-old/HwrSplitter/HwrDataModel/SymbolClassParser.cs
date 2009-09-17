using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HwrDataModel
{
	public static class SymbolClassParser
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
		public static SymbolClasses Parse(FileInfo file)
		{
			//TODO: known limitation: culture-sensitive parsing.
			Dictionary<char, SymbolClass> symbolsByChar;
			using (var reader = file.OpenText())
				symbolsByChar = reader.ReadToEnd()
						.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
						.Select(line => line.Split(',').Select(part => part.Trim()).ToArray())
						.Where(parts => parts.Length == 4)//no empty lines!
						.Select(parts => new SymbolClass(
							(char)int.Parse(parts[0]),
							GaussianEstimate.CreateWithVariance(double.Parse(parts[1]), double.Parse(parts[2]))
						))
						.ToDictionary(symbolWidth => symbolWidth.Letter);

			symbolsByChar[(char)1] = symbolsByChar.Values.Any()
				? new SymbolClass(
					(char)1,
					GaussianEstimate.CreateWithVariance(symbolsByChar.Values.Select(sw => sw.Length.Mean).Average(), symbolsByChar.Values.Select(sw => sw.Length.Variance).Average() * 9)
					)
				: new SymbolClass(
					(char)1,
					GaussianEstimate.CreateWithVariance(50, 1000)
					);

			var symbolClasses = symbolsByChar.Values.OrderBy(c => c.Letter).ToArray();
			for (int i = 0; i < symbolClasses.Length; i++)
				symbolClasses[i].Code = (uint)i;
			return new SymbolClasses { Symbol = symbolClasses };
		}
	}
}
