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
				.SelectMany(s => s).Select(ToBase).ToArray();

			GC.Collect(2, GCCollectionMode.Optimized, false);

			var kna = new[] { 1, 2, 3, 4, 6, 12, 18 }.Select(len => new KNucleotide(len)).ToArray();

			foreach (var h in kna)
			{
				var lenT = Stopwatch.StartNew();
				h.KFrequency(bases);
				Console.Error.WriteLine("len: " + h.FrameLength + ": " + lenT.ElapsedMilliseconds);
			}

			var fragments = new[] { "GGT", "GGTA", "GGTATT", "GGTATTTTAATT", 
				"GGTATTTTAATTTATAGT" };

			kna[0].WriteFrequencies(bases);
			kna[1].WriteFrequencies(bases);

			foreach (var result in fragments.Zip(kna.Skip(2), (frag, h) => h.GetCount(frag.Select(ToBase).ToArray()) + "\t" + frag))
				Console.WriteLine(result);
		}
		finally
		{
			Console.Error.WriteLine("Took " + sw.WallClockMilliseconds() / 1000.0 + "s; t0: " + sw.CpuMilliseconds() / 1000.0 + "s; " + GC.GetTotalMemory(false) / 1024 / 1024 + "MB");
		}
	}
}

class KNucleotide
{
	class Count { public int V;}
	public readonly int FrameLength;

	static ulong DnaFragment(Base[] seq, int offset, int length)
	{
		var data = 0ul;
		for (int i = 0; i < length; i++)
			data |= (ulong)seq[offset + i] << i * 2;
		return data;
	}
	static Base Get(ulong fragment, int i) { return (Base)(fragment >> i * 2 & 3); }
	IEnumerable<Base> All(ulong code) { return Enumerable.Repeat(code, FrameLength).Select(Get); }
	string Stringify(ulong code) { return string.Concat(All(code)); }

	Dictionary<ulong, Count> counts = new Dictionary<ulong, Count>();
	int total;

	public KNucleotide(int l) { FrameLength = l; }

	public void WriteFrequencies(Base[] seq)
	{
		double percent = 100.0 / (seq.Length - FrameLength + 1);
		foreach (var item in counts
			.OrderByDescending(kv => kv.Value.V).ThenBy(kv => kv.Key.ToString()))
			Console.WriteLine(Stringify(item.Key) + " " + (item.Value.V * percent).ToString("f3"));
		Console.WriteLine();
	}

	public int GetCount(Base[] fragment)
	{

		Count count;
		return counts.TryGetValue(DnaFragment(fragment, 0, FrameLength), out count) ? count.V : 0;
	}

	public void KFrequency(Base[] seq)
	{
		int n = seq.Length - FrameLength + 1;
		for (int i = 0; i < n; i++)
		{
			var key = DnaFragment(seq, i, FrameLength);
			Count count;
			if (counts.TryGetValue(key, out count))
				count.V++;
			else
				counts[key] = new Count { V = 1 };
		}
	}
}

