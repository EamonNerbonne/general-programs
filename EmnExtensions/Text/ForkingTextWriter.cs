using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EmnExtensions.Text {
	public class ForkingTextWriter : AbstractTextWriter {
		readonly bool closeUnderlying;
		readonly TextWriter[] writers;
		public ForkingTextWriter(IEnumerable<TextWriter> targetWriters, bool? closeUnderlyingWriters = null) {
			closeUnderlying = closeUnderlyingWriters ?? true;
			writers = targetWriters.ToArray();
		}

		protected override void WriteString(string value) { foreach (var writer in writers) writer.Write(value); }

		public override void Flush() { base.Flush(); foreach (var writer in writers) writer.Flush(); }

		protected override void Dispose(bool disposing) {
			if (closeUnderlying && disposing)
				foreach (var writer in writers)
					writer.Dispose();
		}
	}
}
