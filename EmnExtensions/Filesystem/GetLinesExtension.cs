using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EmnExtensions.Filesystem
{
    public static class GetLinesExtension
    {
        public static IEnumerable<string> Lines(this TextReader inp)
        {
            using (inp) {
                for (var line = inp.ReadLine(); line != null; line = inp.ReadLine()) {
                    yield return line;
                }
            }
        }

        public static IEnumerable<string> GetLines(this FileInfo fi)
            => fi.OpenText().Lines();

        public static IEnumerable<string> GetLines(this FileInfo fi, Encoding encoding)
            => new StreamReader(fi.FullName, encoding).Lines();
    }
}
