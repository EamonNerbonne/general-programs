using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace HWRsplitter {
    public struct LengthEstimate {
        public double len, var;
        public static LengthEstimate operator +(LengthEstimate a, LengthEstimate b) {
            return new LengthEstimate {
                len = a.len + b.len,
                var = a.var + b.var
            };
        }
    }

    public class SymbolWidth {
        public char c;
        public LengthEstimate estimate;
    }
    public static class SymbolWidthParser {

        /// <summary>
        /// Special chars: 
        /// 0 - start of line
        /// 1 - unknown char
        /// 10 - end of line
        /// 32 - general word spacing (add this to EVERY word once).
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static Dictionary<char, SymbolWidth> Parse(FileInfo file) {
            Dictionary<char, SymbolWidth> retval;
            using (var reader = file.OpenText())
                retval = reader.ReadToEnd()
                        .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(line => line.Split(',').Select(part => part.Trim()).ToArray())
                        .Where(parts=>parts.Length==4)//no empty lines!
                        .Select(parts => new SymbolWidth {
                            c = (char)int.Parse(parts[0]),
                            estimate = new LengthEstimate {
                                len = double.Parse(parts[1]),
                                var = double.Parse(parts[2])
                            }
                        })
                        .ToDictionary(symbolWidth => symbolWidth.c);

            retval[(char)1] = new SymbolWidth {
                c = (char)1,
                estimate = new LengthEstimate {
                    len = retval.Values.Select(sw => sw.estimate.len).Average(),
                    var = retval.Values.Select(sw => sw.estimate.var).Average() * 9
                }
            };

            return retval;
        }
        public static Dictionary<char, SymbolWidth> Unknown {
            get {
                return (new[] { 
                    new SymbolWidth { 
                        c = (char)1, 
                        estimate= new LengthEstimate{
                            var = 1000, 
                            len = 50
                        }
                    }
                })
                    .ToDictionary(sw => sw.c);
            }
        }
    }
}
