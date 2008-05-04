using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace HWRsplitter {
    public class SymbolWidth {
        public char c;
        public double width;
        public double variance;
    }
    public static class SymbolWidthParser {
        public static Dictionary<char, SymbolWidth> Parse(FileInfo file) {
            using (var reader = file.OpenText())
                return
                    reader.ReadToEnd()
                        .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(line => line.Split(',').Select(part => part.Trim()).ToArray())
                        .Select(parts => new SymbolWidth {
                            c = (char)int.Parse(parts[0]),
                            width = double.Parse(parts[1]),
                            variance = double.Parse(parts[2])
                        })
                        .ToDictionary(symbolWidth => symbolWidth.c);



        }
    }
}
