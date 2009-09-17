using System;
using System.Collections.Generic;
using System.Linq;
using MoreLinq;
using EmnExtensions.Filesystem;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Xml;
using EmnExtensions;
using System.Globalization;

namespace HwrDataModel
{
	public class SymbolClasses : XmlSerializableBase<SymbolClasses>
	{
		Dictionary<char, uint> lookupCode;
		uint unknownCode;
		SymbolClass[] symbols;
		int iteration = 0;
		int nextPage = 0;


		//XmlSerialized properties BEGIN ==============================================
		public int Iteration { get { return iteration; } set { iteration = value; } }
		public int NextPage { get { return nextPage; } set { nextPage = value; } }
		public SymbolClass[] Symbol {
			get { return symbols; }
			set {
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
		//XmlSerialized properties END ==============================================

		public uint LookupSymbolCode(char letter) {
			uint code;
			if (!lookupCode.TryGetValue(letter, out code))
				code = unknownCode;
			return code;
		}

		public int Count { get { return symbols.Length; } }
		public SymbolClass this[int code] { get { return symbols[code]; } }

		public static SymbolClasses Load(FileInfo symbolsFile) {
			bool isZip = symbolsFile.Name.ToLowerInvariant().EndsWith(".xml.gz");

			using (Stream fileStream = symbolsFile.OpenRead())
			using (Stream zipStream = isZip ? new GZipStream(fileStream, CompressionMode.Decompress) : fileStream)
			using (XmlReader xmlreader = XmlReader.Create(zipStream))
				return SymbolClasses.Deserialize(xmlreader);
		}

		public static SymbolClasses TryLoad(DirectoryInfo dir) {
			var symbolFiles = dir.GetFiles("symbols*.xml").Concat(dir.GetFiles("symbols*.xml.gz"));
			FileInfo newestFile = symbolFiles.Aggregate((FileInfo)null, (a, b) => a == null || b == null ? a ?? b : a.LastWriteTimeUtc < b.LastWriteTimeUtc ? b : a);
			return
				newestFile == null
				? null
				: Load(newestFile);
		}

		public static SymbolClasses LoadWithFallback(DirectoryInfo dataDir, FileInfo charWidthFile) { 
			return TryLoad(dataDir) ?? SymbolClassParser.Parse(charWidthFile); 
		}
		public void Save(DirectoryInfo saveDir) {
			using (var stream = saveDir.GetRelativeFile("symbols-" + DateTime.Now.ToString("u", CultureInfo.InvariantCulture).Replace(' ', '_').Replace(':', '.') + "-p" + nextPage + ".xml.gz").Open(FileMode.Create))
			using (var zipStream = new GZipStream(stream, CompressionMode.Compress))
				SerializeTo(zipStream);
		}
	}
}
