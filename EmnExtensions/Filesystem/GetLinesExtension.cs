using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace EamonExtensions.Filesystem {
    public static class GetLinesExtension {
        public static IEnumerable<string> GetLines(this FileInfo fi) {
            var stream = fi.OpenText();
            while (!stream.EndOfStream)
                yield return stream.ReadLine();
        }
    }
}