using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmnExtensions.Text {
    /// <summary>
    /// This class implements a minimalist text writer.  Only one method is necessary to implement, namely 
    /// <code>WriteString</code>, unlike the normal <code>TextWriter</code>, which requires you implement all of them.
    /// This class uses the <code>FormatProvider</code> property to format everything, and is probably a little slower
    /// than an optimal writer, but extremely simple.
    /// 
    /// Beyond the required implementation of <code>WriteString</code>, inhereting classes might wish to override 
    /// <code>Encoding</code>, which currently (meaninglessly) returns <code>Encoding.Unicode</code> if a meaningful
    /// alternative exists, and might wish to overrise <code>Close</code>, <code>Flush</code> &amp; <code>Dispose</code>
    /// as necessary.
    /// </summary>
    public abstract class AbstractTextWriter :TextWriter{
        //no idea, of course, but could be, certainly!

        public override void Write(bool value) {
                        WriteString(value.ToString(FormatProvider));
        }

        public override void Write(char value) {
                        WriteString(value.ToString(FormatProvider));
        }

        public override void Write(char[] buffer) {
                        WriteString(new string(buffer));
        }

        public override void Write(char[] buffer, int index, int count) {
                        WriteString(new string(buffer,index,count));
        }

        public override void Write(decimal value) {
                        WriteString(value.ToString(FormatProvider));
        }

        public override void Write(double value) {
                        WriteString(value.ToString(FormatProvider));
        }

        public override void Write(float value) {
                        WriteString(value.ToString(FormatProvider));
        }

        public override void Write(int value) {
                        WriteString(value.ToString(FormatProvider));
        }

        public override void Write(long value) {
                        WriteString(value.ToString(FormatProvider));
        }

        public override void Write(object value) {
            WriteString(value.ToString());
        }

        public override void Write(string format, object arg0) {
            WriteString(string.Format(FormatProvider, format, arg0));
        }

        public override void Write(string format, object arg0, object arg1) {
            WriteString(string.Format(FormatProvider, format, arg0,arg1));
        }

        public override void Write(string format, object arg0, object arg1, object arg2) {
            WriteString(string.Format(FormatProvider, format, arg0,arg1,arg2));
        }

        public override void Write(string format, params object[] arg) {
            WriteString(string.Format(FormatProvider,format,arg));
        }

        public override void Write(string value) {
            WriteString(value.ToString(FormatProvider));
        }

        public override void Write(uint value) {
            WriteString(value.ToString(FormatProvider));
        }

        public override void Write(ulong value) {
            WriteString(value.ToString(FormatProvider));
        }

        protected abstract void WriteString(string value);

        public override Encoding Encoding {
            get { return Encoding.Unicode; }
        }
    }
}
