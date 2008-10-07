
namespace EamonExtensionsLinq.Algorithms
{
	public struct Suffix
	{
		int AbsStartPos;
		public Suffix(int startPos) { this.AbsStartPos = startPos; }
		public Suffix Next { get { return new Suffix { AbsStartPos = this.AbsStartPos + 1 }; } }
		//public static Suffix operator ++(Su
		public override bool Equals(object obj) {
			if(!(obj is Suffix)) return false;
			return AbsStartPos == ((Suffix)obj).AbsStartPos;
		}
		public override int GetHashCode() { return AbsStartPos.GetHashCode(); }

		public static explicit operator int(Suffix suf) { return suf.AbsStartPos; }
		public static explicit operator Suffix(int startPos) { return new Suffix(startPos); }

		public static bool operator <(Suffix a, Suffix b) { return a.AbsStartPos < b.AbsStartPos; }
		public static bool operator >(Suffix a, Suffix b) { return a.AbsStartPos > b.AbsStartPos; }
		public static bool operator <=(Suffix a, Suffix b) { return a.AbsStartPos <= b.AbsStartPos; }
		public static bool operator >=(Suffix a, Suffix b) { return a.AbsStartPos >= b.AbsStartPos; }
		public static bool operator ==(Suffix a, Suffix b) { return a.AbsStartPos == b.AbsStartPos; }
		public static bool operator !=(Suffix a, Suffix b) { return a.AbsStartPos != b.AbsStartPos; }

		public static Suffix operator +(Suffix a, int offset) { return new Suffix(a.AbsStartPos + offset); }
		public static Suffix operator -(Suffix a, int offset) { return new Suffix(a.AbsStartPos - offset); }
		public static int operator -(Suffix a, Suffix b) { return a.AbsStartPos - b.AbsStartPos; }
	}
}
