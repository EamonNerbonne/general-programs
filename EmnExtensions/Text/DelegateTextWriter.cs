using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmnExtensions.Text {
    public class DelegateTextWriter : AbstractTextWriter {
        public event Action<string> OnWrite;
        public event Action OnClose;

        public DelegateTextWriter(Action<string> onWrite,Action onClose){
            OnWrite = onWrite;
            OnClose = onClose ?? NullOp;
        }
        static void NullOp(){}
        public DelegateTextWriter(Action<string> onWrite) :this(onWrite,null) {}
        protected override void WriteString(string value) {
            OnWrite(value);
        }
        protected override void Dispose(bool disposing) {
			OnClose();
			base.Dispose(disposing);
        }
    }
}
