using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using KNucleotideFastaBench;

enum Base : byte { A, C, G, T }
static class Program
{
	public static Base ToBase(this char c)
	{
		if (c == 'A' || c == 'a') return Base.A;
		if (c == 'C' || c == 'c') return Base.C;
		if (c == 'G' || c == 'g') return Base.G;
		if (c == 'T' || c == 't') return Base.T;
		throw new ArgumentOutOfRangeException("c");
	}

	public static IEnumerable<string> Lines(this TextReader reader)
	{
		for (var line = reader.ReadLine(); line != null; line = reader.ReadLine())
			yield return line;
	}

	public static IEnumerable<T[]> Batch<T>(this IEnumerable<T> list, int batchSize)
	{
		int i = 0;
		T[] arr = new T[batchSize];
		foreach (var t in list)
		{
			arr[i++] = t;
			if (i == batchSize)
			{
				yield return arr;
				i = 0;
				arr = new T[batchSize];
			}
		}
		if (i > 0)
		{
			Array.Resize(ref arr, i);
			yield return arr;
		}
	}

	public static void Main(string[] args)
	{

		var sw = ThreadCpuTimer.StartNew();

		try
		{
			var console_In = args.Length > 0 ? File.OpenText(args[0]) : Console.In;
			var bases = console_In.Lines()
				.SkipWhile(s => !s.StartsWith(">THREE")).Skip(1)
				.TakeWhile(s => !s.StartsWith(">")).Where(s => !s.StartsWith(";"))
				.SelectMany(s => s).Select(ToBase).Batch(1024 * 1024).ToArray();

			GC.Collect(2, GCCollectionMode.Optimized, false);

			var kna = new[] { 1, 2, 3, 4, 6, 12, 18 }.Select(DnaCounter.Create).ToArray();

			foreach (var h in kna)
			{
				var lenT = Stopwatch.StartNew();
				h.Process(bases);
				Console.Error.WriteLine("len: " + h.len + ": " + lenT.ElapsedMilliseconds);
			}

			var fragments = new[] { "GGT", "GGTA", "GGTATT", "GGTATTTTAATT", 
				"GGTATTTTAATTTATAGT" };

			((DnaCounter.DenseCounter)kna[0]).WriteFrequencies();
			((DnaCounter.DenseCounter)kna[1]).WriteFrequencies();

			foreach (var result in fragments.Zip(kna.Skip(2), (frag, h) => h.GetCount(frag.Select(ToBase).ToArray()) + "\t" + frag))
				Console.WriteLine(result);
		}
		finally
		{
			Console.Error.WriteLine("Took " + sw.WallClockMilliseconds() / 1000.0 + "s; t0: " + sw.CpuMilliseconds() / 1000.0 + "s; " + GC.GetTotalMemory(false) / 1024 / 1024 + "MB");
		}
	}
}

abstract class DnaCounter
{
	public abstract int GetCount(Base[] fragment);
	public abstract void Process(IEnumerable<Base[]> seq);
	public readonly int len;
	protected DnaCounter(int l) { len = l; }
	public static DnaCounter Create(int length)
	{
		return length > 8 ? new SparseCounter(length)
			: (DnaCounter)new DenseCounter(length);
	}


	static ulong DnaFragment(Base[] seq, int offset, int length)
	{
		var dna = 0ul;
		for (int i = 0; i < length; i++)
			dna |= (ulong)seq[offset + i] << i * 2;
		return dna;
	}
	static Base Get(ulong dna, int i) { return (Base)(dna >> i * 2 & 3); }
	static ulong Push(ulong dna, Base b, int l) { return dna >> 2 | (ulong)b << l * 2 - 2; }
	static IEnumerable<Base> All(ulong dna, int l) { return Enumerable.Repeat(dna, l).Select(Get); }
	static string Stringify(ulong dna, int l) { return string.Concat(All(dna, l)); }

	public sealed class DenseCounter : DnaCounter
	{
		int[] counts;
		int total;
		public DenseCounter(int l) : base(l) { counts = new int[1 << l * 2]; }

		public override int GetCount(Base[] fragment)
		{
			return counts[DnaFragment(fragment, 0, len)];
		}

		public override void Process(IEnumerable<Base[]> seq)
		{
			ulong dna = 0;
			foreach (var seg in seq)
				foreach (var b in seg)
				{
					dna = Push(dna, b, len);
					total++;
					if (total < len) continue;
					counts[dna]++;
				}
		}

		public void WriteFrequencies()
		{
			Console.WriteLine(
				string.Concat(counts.Select((c, dna) => new {
					p = c * 100.0 / (total - len + 1),
					dna = Stringify((ulong)dna, len)
				})
				.OrderByDescending(x => x.p).ThenBy(x => x.dna)
				.Select(x => x.dna + " " + x.p.ToString("f3") + "\n")
				));
		}
	}

	sealed class SparseCounter : DnaCounter
	{
		public SparseCounter(int l) : base(l) { }
		class Count { public int V;}

		Dictionary<ulong, Count> counts = new Dictionary<ulong, Count>();
		int total;

		public override int GetCount(Base[] fragment)
		{
			Count count;
			return counts.TryGetValue(DnaFragment(fragment, 0, len), out count) ? count.V : 0;
		}

		public override void Process(IEnumerable<Base[]> seq)
		{
			ulong dna = 0;
			foreach (var seg in seq)
				foreach (var b in seg)
				{
					dna = Push(dna, b, len);
					total++;
					if (total < len) continue;
					Count count;
					if (counts.TryGetValue(dna, out count))
						count.V++;
					else
						counts[dna] = new Count { V = 1 };
				}
		}
	}
}
