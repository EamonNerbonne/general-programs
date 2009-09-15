using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HwrDataModel
{
	public class SymbolClasses
	{
		Dictionary<char, uint> lookupCode;
		uint unknownCode;
		SymbolClass[] symbols;
		int iteration = 0;
		int lastPage = -1;

		public uint LookupSymbolCode(char letter)
		{
			uint code;
			if (!lookupCode.TryGetValue(letter, out code))
				code = unknownCode;
			return code;
		}

		public int Count { get { return symbols.Length; } }
		public SymbolClass this[int code] { get { return symbols[code]; } }

		public int Iteration { get { return iteration; } set { iteration = value; } }
		public int LastPage { get { return lastPage; } set { lastPage = value; } }
		public SymbolClass[] Symbol
		{
			get { return symbols; }
			set
			{
				if (symbols != null)
					throw new ApplicationException("Symbol Classes already set");
				symbols = value;
				lookupCode = symbols.ToDictionary(sym => sym.Letter, sym => sym.Code);
				bool containsSpecialChars = lookupCode.ContainsKey((char)0) && lookupCode.ContainsKey((char)1) && lookupCode.ContainsKey((char)10) && lookupCode.ContainsKey((char)32);
				if (!containsSpecialChars)
					throw new ArgumentException("the loaded symbols are incomplete; they must at a minimum include symbols 0 (start), 1(unknown), 10(end) and 32(space).");
				unknownCode = lookupCode[(char)1];
			}
		}
	}
}
