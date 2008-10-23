using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EmnExtensions.Filesystem
{
	public static class GetLinesExtension
	{
		public static IEnumerable<string> GetLines(this FileInfo fi) {
			var stream = fi.OpenText();
			while(!stream.EndOfStream)
				yield return stream.ReadLine();
		}
		public static IEnumerable<string> GetLines(this FileInfo fi, Encoding encoding) {
			using(Stream stream = fi.OpenRead()) {
				var reader = new StreamReader(stream, encoding);
				while(!reader.EndOfStream)
					yield return reader.ReadLine();
			}
		}
	}
}