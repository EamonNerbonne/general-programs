using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LvqGui.CreatorGui {
	public abstract class CloneableAs<T> where T:CloneableAs<T> {
		public T Clone() { return (T)MemberwiseClone(); }
	}
}
