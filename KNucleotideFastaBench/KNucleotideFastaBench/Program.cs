/* The Computer Benchmarks Game
 * http://shootout.alioth.debian.org/
 *
 * byte processing, C# 3.0 idioms, frame level paralellism by Robert F. Tobler
 */

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using KNucleotideFastaBench;

#if true
struct DnaSequence {
	const int BasesPerRaw = 32;
	readonly ulong[] rawdata;
	public readonly int Length;
	public DnaSequence(IEnumerable<Base> bases) {
		var list = new List<ulong>(32);
		int i = 0;
		ulong next = 0;
		foreach (var b in bases) {
			next |= (ulong)b << i * 2;
			i++;
			if (i == BasesPerRaw) {
				i = 0;
				list.Add(next);
				next = 0;
			}
		}
		Length = list.Count * BasesPerRaw + i;
		if (i != 0) list.Add(next);
		rawdata = list.ToArray();
	}

	public Base this[int i] { get { return (Base)(rawdata[i >> 5] >> (i & 31) * 2 & 3); } }
}
#elif true
struct DnaSequence {
	const int BasesPerRawLog = 4;
	readonly uint[] rawdata;
	public readonly int Length;
	public DnaSequence(IEnumerable<Base> bases) {
		var list = new List<uint>(64);
		int i = 0;
		uint next = 0;
		foreach (var b in bases) {
			next |= (uint)b << i * 2;
			i++;
			if (i == 16) {
				i = 0;
				list.Add(next);
				next = 0;
			}
		}
		Length = list.Count * 16 + i;
		if (i != 0) list.Add(next);
		rawdata = list.ToArray();
	}

	public Base this[int i] { get { return (Base)(rawdata[i >> 4] >> (i & 15) * 2 & 3); } }
}
#else 
struct DnaSequence {
	readonly Base[] data;
	public DnaSequence(IEnumerable<Base> bases) { data = bases.ToArray(); }
	public int Length { get { return data.Length; } }
	public Base this[int i] { get { return data[i]; } }
}
#endif
struct DnaFragment : IEquatable<DnaFragment> {
	public readonly ulong data;
	public DnaFragment(DnaSequence seq, int offset, int length) {
		data = (ulong)length & 31;
		for (int i = 0; i < length; i++)
			data |= (ulong)seq[offset + i] << 62 - i * 2;
	}
	public DnaFragment(string seq) {
		data = (ulong)seq.Length & 31;
		for (int i = 0; i < seq.Length; i++)
			data |= (ulong)seq[i].ToBase() << 62 - i * 2;
	}
	public int Length { get { return (int)data & 31; } }
	public Base Get(int i) { return (Base)(data >> 62 - i * 2 & 3); }
	public IEnumerable<Base> All() { var x = this; return Enumerable.Range(0, Length).Select(x.Get); }
	public override string ToString() { return string.Concat(All()); }
	public override int GetHashCode() { return (int)(data ^ (data >> 32)); }
	public bool Equals(DnaFragment other) { return data == other.data; }
}

enum Base : byte { A, C, G, T }

static class Extensions {
	public static Base ToBase(this char c) {
		c = char.ToUpperInvariant(c);
		if (c == 'A') return Base.A;
		if (c == 'C') return Base.C;
		if (c == 'G') return Base.G;
		if (c == 'T') return Base.T;
		throw new ArgumentOutOfRangeException("c");
	}

	public static BitArray GetBits(this IEnumerable<char> bases) {
		var typedBases = bases.Select(ToBase).ToArray();
		var array = new BitArray(typedBases.Length * 2);
		for (var i = 0; i < typedBases.Length; i++) {
			var b = typedBases[i];
			array[2 * i] = ((byte)b & 1) == 1;
			array[2 * i + 1] = ((byte)b & 2) == 2;
		}
		return array;
	}

	public static IEnumerable<string> Lines(this TextReader reader) {
		for (var line = reader.ReadLine(); line != null; line = reader.ReadLine())
			yield return line;
	}
}

static class Program {
	public static int TaskCount;
	public static int Current = -1;
	public static KNucleotide[] kna;

	public static void Main(string[] args) {

		var sw = ThreadCpuTimer.StartNew();

		try {
			var console_In = args.Length > 0 ? File.OpenText(args[0]) : Console.In;
			var bases = console_In.Lines().SkipWhile(s => !s.StartsWith(">THREE")).Skip(1)
				.TakeWhile(s => !s.StartsWith(">")).Where(s => !s.StartsWith(";"))
				.SelectMany(s => s).Select(c => c.ToBase()).ToArray();

			var seq = new DnaSequence(bases);

			var lengths = new[] { 1, 2, 3, 4, 6, 12, 18 };

			var kna = lengths.Select(len => Enumerable.Range(0, len).Select(i => new KNucleotide(seq, len, i)).ToArray()).ToArray();

			foreach (var set in kna)
				foreach (var knF in set) {
					var tim = Stopwatch.StartNew();
					knF.KFrequency();
					Console.Error.WriteLine("len: " + knF.length + "; frame: " + knF.frame + "; " + tim.Elapsed.TotalMilliseconds);
				}

			var fragments = new[] { "GGT", "GGTA", "GGTATT", "GGTATTTTAATT", 
				"GGTATTTTAATTTATAGT" }.Select(s => new DnaFragment(s));
			kna[0][0].WriteFrequencies();
			kna[1][0].AddFrequencies(kna[1][1]);
			kna[1][0].WriteFrequencies();


			foreach (var result in fragments.Zip(kna.Skip(2), (frag, set) =>
				set.Sum(knF => knF.GetCount(frag)) + "\t" + frag))
				Console.WriteLine(result);

		} finally {

			Console.Error.WriteLine("Took " + sw.WallClockMilliseconds() / 1000.0 + "s; t0: " + sw.CpuMilliseconds() / 1000.0 + "s; " + GC.GetTotalMemory(false) / 1024 / 1024 + "MB");
		}
	}

}

class KNucleotide {
	class Count { public int V;}

	readonly Dictionary<DnaFragment, Count> frequencies
		= new Dictionary<DnaFragment, Count>();

	readonly DnaSequence seq;
	public readonly int length;
	public readonly int frame;

	public KNucleotide(DnaSequence s, int l, int f) {
		seq = s; length = l; frame = f;
	}

	public void AddFrequencies(KNucleotide other) {
		foreach (var kvp in other.frequencies) {
			Count count;
			if (frequencies.TryGetValue(kvp.Key, out count))
				count.V += kvp.Value.V;
			else
				frequencies[kvp.Key] = kvp.Value;
		}
	}

	public void WriteFrequencies() {
		double percent = 100.0 / (seq.Length - length + 1);
		foreach (var item in frequencies
			.OrderByDescending(kv => kv.Value.V).ThenBy(kv => kv.Key.data))
			Console.WriteLine(item.Key + " " + (item.Value.V * percent).ToString("f3"));
		Console.WriteLine();
	}

	public int GetCount(DnaFragment fragment) {
		Count count;
		return frequencies.TryGetValue(fragment, out count) ? count.V : 0;
	}

	public void KFrequency() {
		int n = seq.Length - length + 1;
		for (int i = frame; i < n; i += length) {
			var key = new DnaFragment(seq, i, length);
			Count count;
			if (frequencies.TryGetValue(key, out count))
				count.V++;
			else
				frequencies[key] = new Count { V = 1 };
		}
	}
}

