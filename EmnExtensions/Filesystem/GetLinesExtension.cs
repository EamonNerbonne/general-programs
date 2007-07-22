using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace EamonExtensionsLinq.Filesystem {
    public static class GetLinesExtension {
        public static IEnumerable<string> GetLines(this FileInfo fi) {
            var stream = fi.OpenText();
            while (!stream.EndOfStream)
                yield return stream.ReadLine();
        }
        public static IEnumerable<string> GetLines(this FileInfo fi,Encoding encoding)
        {
            var stream = new StreamReader(fi.OpenRead(), encoding);
            while (!stream.EndOfStream)
                yield return stream.ReadLine();
        }
    }
}