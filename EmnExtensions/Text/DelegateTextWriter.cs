using System;

namespace EmnExtensions.Text
{
    public sealed class DelegateTextWriter : AbstractTextWriter
    {
        readonly Action<string> OnWrite;
        readonly Action OnClose;

        static void NullOp() { }

        public DelegateTextWriter(Action<string> onWrite, Action onClose = null)
        {
            OnWrite = onWrite;
            OnClose = onClose ?? NullOp;
        }

        protected override void WriteString(string value) => OnWrite(value);

        protected override void Dispose(bool disposing)
        {
            OnClose();
            base.Dispose(disposing);
        }
    }
}
