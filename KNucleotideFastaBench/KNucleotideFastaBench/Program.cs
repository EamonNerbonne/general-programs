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
using System.Collections.Concurrent;

enum Base : byte { A, C, G, T }
struct DnaFragment {
	public class ComparerImpl : IEqualityComparer<DnaFragment> {
		public bool Equals(DnaFragment x, DnaFragment y) { return x.data == y.data; }
		public int GetHashCode(DnaFragment obj) { return (int)(obj.data >> 32 ^ obj.data); }
	}
	public static readonly ComparerImpl Comparer = new ComparerImpl();

	public readonly ulong data;
	public DnaFragment(Base[] seq, int offset, int length) {
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
	public override string ToString() {
		var tmp = this;
		return string.Concat(Enumerable.Range(0, Length).Select(tmp.Get));
	}
}
static class Program {
	public static Base ToBase(this char c) {
		if (c == 'A' || c == 'a') return Base.A;
		if (c == 'C' || c == 'c') return Base.C;
		if (c == 'G' || c == 'g') return Base.G;
		if (c == 'T' || c == 't') return Base.T;
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

	public static void Main(string[] args) {

		var sw = ThreadCpuTimer.StartNew();

		try {
			var console_In = args.Length > 0 ? File.OpenText(args[0]) : Console.In;
			var bases = console_In.Lines()
				.SkipWhile(s => !s.StartsWith(">THREE")).Skip(1)
				.TakeWhile(s => !s.StartsWith(">")).Where(s => !s.StartsWith(";"))
				.SelectMany(s => s).Select(c => c.ToBase()).ToArray();

			GC.Collect(2, GCCollectionMode.Optimized, false);

			var lengths = new[] { 1, 2, 3, 4, 6, 12, 18 };

			var kna = lengths.Select(len => Enumerable.Range(0, len).Select(i => new KNucleotide()).ToArray()).ToArray();

			foreach (var set in kna) {
				var lenT = Stopwatch.StartNew();
				Console.Error.Write("len: " + set.Length);
				int i = 0;
				foreach (var knF in set) {
					var tim = Stopwatch.StartNew();
					set[0].KFrequency(bases, set.Length, i++);
					Console.Error.Write("; " + tim.ElapsedMilliseconds);
				}
				Console.Error.WriteLine(": " + lenT.ElapsedMilliseconds);

			}

			var fragments = new[] { "GGT", "GGTA", "GGTATT", "GGTATTTTAATT", 
				"GGTATTTTAATTTATAGT" }.Select(s => new DnaFragment(s));
			kna[0][0].WriteFrequencies(bases, 1);
			//kna[1][0].AddFrequencies(kna[1][1]);
			kna[1][0].WriteFrequencies(bases, 2);


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

	Dictionary<DnaFragment, Count> frequencies = new Dictionary<DnaFragment, Count>(DnaFragment.Comparer);

	public void AddFrequencies(KNucleotide other) {
		foreach (var kvp in other.frequencies)
			frequencies[kvp.Key] = new Count { V = GetCount(kvp.Key) + kvp.Value.V };
	}

	public void WriteFrequencies(Base[] seq, int length) {
		double percent = 100.0 / (seq.Length - length + 1);
		foreach (var item in frequencies
			.OrderByDescending(kv => kv.Value.V).ThenBy(kv => kv.Key.ToString()))
			Console.WriteLine(item.Key + " " + (item.Value.V * percent).ToString("f3"));
		Console.WriteLine();
	}

	public int GetCount(DnaFragment fragment) {
		Count count;
		return frequencies.TryGetValue(fragment, out count) ? count.V : 0;
	}

	public void KFrequency(Base[] seq, int length, int frame) {
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

