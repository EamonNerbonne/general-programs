using System;
using System.IO;

namespace EmnExtensions.Text
{
    /// <summary>
    /// A wrapper to a Stream which does all its Read() and Peek() calls using ReadBlock() to handle slow underlying streams (eg Network Streams)
    /// </summary>
    public sealed class BlockingReader : TextReader
    {
        bool hasPeeked;
        int peekChar;
        readonly TextReader reader;

        public BlockingReader(TextReader reader) => this.reader = reader;

        public override int Read()
        {
            if (!hasPeeked) {
                return reader.Read();
            }

            hasPeeked = false;
            return peekChar;
        }

        public override int Peek()
        {
            if (!hasPeeked) {
                peekChar = reader.Read();
                hasPeeked = true;
            }

            return peekChar;
        }

        public override int Read(char[] buffer, int index, int count)
        {
            if (buffer == null) {
                throw new ArgumentNullException("buffer");
            }

            if (index < 0) {
                throw new ArgumentOutOfRangeException("index");
            }

            if (count < 0) {
                throw new ArgumentOutOfRangeException("count");
            }

            if (buffer.Length - index < count) {
                throw new ArgumentException("Buffer too small");
            }

            var peekCharsRead = 0;
            if (hasPeeked) {
                buffer[index] = (char)peekChar;
                hasPeeked = false;
                index++;
                count--;
                peekCharsRead++;
            }

            return peekCharsRead + reader.ReadBlock(buffer, index, count);
        }

        protected override void Dispose(bool disposing)
        {
            try {
                if (disposing) {
                    reader.Dispose();
                }
            } finally {
                base.Dispose(disposing);
            }
        }
    }
}
