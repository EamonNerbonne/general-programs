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

struct BaseSeq
{
	const uint BasesPerULong = 32;
	readonly ulong[] rawdata;
	readonly int length;
	public BaseSeq(IEnumerable<Base> bases)
	{
		rawdata = new ulong[16];
		length = 0;
		var buffer = new Base[32];
		var list = new List<ulong>(32);
		var bufI = 0;
		foreach (var b in bases)
		{
			if (bufI == 32)
			{
				ulong next=0;
				for (int i = 0; i < bufI; i++)
					next |= (ulong) buffer[i] << i*2;
				list.Add(next);
				rawdata[length/BasesPerULong] = next;
				length += bufI;
				bufI = 0;
			}
		}
		if (length/BasesPerULong >= rawdata.Length)
			Array.Resize(ref rawdata, rawdata.Length*2);
		ulong next1=0;
		for (int i1 = 0; i1 < bufI; i1++)
			next1 |= (ulong) buffer[i1] << i1*2;
		rawdata[length/BasesPerULong] = next1;
		length += bufI;
		Array.Resize(ref rawdata, length);
	}

	public Base this[int i]
	{
		get
		{
			var dataIndex = i/32;
			var innerOffset = (i%32)*2;
			return (Base) (rawdata[dataIndex] >> innerOffset & 3);
		}
	}

}


public struct DnaSeq : IEquatable<DnaSeq>
{
	public BitArray Array;
	public int Start;
	public int Length;
	public uint Hash;

	public DnaSeq(BitArray array, int start, int length)
	{

		Array = array; Start = start; Length = length;
		Hash = 0;
		int hashOff = 0;
		for (int i = 0; i < Length; i++)
		{
			Hash = (Array[(Start + i) * 2] ? 1u : 0) + (Array[(Start + i) * 2 + 1] ? 2u : 0) << hashOff;
			hashOff = hashOff + 2 % 32;
		}
	}

	public DnaSeq(BitArray array) : this(array, 0, array.Length / 2) { }

	public override int GetHashCode() { return (int)Hash; }

	public bool Equals(DnaSeq other)
	{
		if (Length != other.Length || Hash != other.Hash) return false;
		for (int i = 0; i < Length*2; i++)
			if (Array[Start*2 + i] != other.Array[other.Start*2 + i]) return false;
		return true;
	}

	public override string ToString()
	{
		var arr = Array;
		return string.Concat(Enumerable.Range(Start, Length).Select(i => ((Base)((arr[i * 2] ? 1u : 0) + (arr[i * 2 + 1] ? 2u : 0))).ToString()));
	}
}

enum Base : byte { A, C, G, T }

static class Extensions
{
	public static Base ToBase(this char c)
	{
		c = char.ToUpperInvariant(c);
		if (c == 'A') return Base.A;
		if (c == 'C') return Base.C;
		if (c == 'G') return Base.G;
		if (c == 'T') return Base.T;
		throw new ArgumentOutOfRangeException("c");
	}

	public static BitArray GetBits(this IEnumerable<char> bases)
	{
		var typedBases = bases.Select(ToBase).ToArray();
		var array = new BitArray(typedBases.Length * 2);
		for (var i = 0; i < typedBases.Length; i++)
		{
			var b = typedBases[i];
			array[2 * i] = ((byte)b & 1) == 1;
			array[2 * i + 1] = ((byte)b & 2) == 2;
		}
		return array;
	}

	public static IEnumerable<string> Lines(this TextReader reader)
	{
		for (var line = reader.ReadLine(); line != null; line = reader.ReadLine())
			yield return line;
	}
}

public static class Program
{
	public static int TaskCount;
	public static int Current = -1;
	public static KNucleotide[] kna;

	public static void Main(string[] args)
	{

		var sw = ThreadCpuTimer.StartNew();

		try
		{
#if true
			var console_In = args[0] != null ? File.OpenText(args[0]) : Console.In;
			var inputSequence = console_In.Lines().SkipWhile(s => !s.StartsWith(">THREE")).Skip(1)
				.TakeWhile(s => !s.StartsWith(">")).Where(s => !s.StartsWith(";")).
				SelectMany(l => l);
#else
			StreamReader source = new StreamReader(args[0] != null ? File.OpenRead(args[0]) : Console.OpenStandardInput());

			var input = new List<string>();

			while ((line = source.ReadLine()) != null)
				if (line.StartsWith( ">THREE"))
					break;

			while ((line = source.ReadLine()) != null)
			{
				char c = line[0];
				if (c == '>') break;
				if (c != ';') input.Add(line.ToUpper());
			}
#endif
			var lengths = new[] { 1, 2, 3, 4, 6, 12, 18 };

			TaskCount = lengths.Aggregate(0, (cnt, len) => cnt + len);
			kna = new KNucleotide[TaskCount];

			var bases = inputSequence.GetBits();
			lengths.Aggregate(0, (cnt, len) => {
				for (int i = 0; i < len; i++)
					kna[cnt + i] = new KNucleotide(bases, len, i);
				return cnt + len;
			});

			var threads = new Thread[Environment.ProcessorCount];
			for (int i = 0; i < threads.Length; i++)
				(threads[i] = new Thread(CountFrequencies)).Start();

			foreach (var t in threads)
				t.Join();

			var seqs = new[]
				{
					null, null,
					"GGT", "GGTA", "GGTATT", "GGTATTTTAATT",
					"GGTATTTTAATTTATAGT"
				};

			int index = 0;
			lengths.Aggregate(0, (cnt, len) => {
				if (len < 3)
				{
					for (int i = 1; i < len; i++)
						kna[cnt].AddFrequencies(kna[cnt + i]);
					kna[cnt].WriteFrequencies();
				}
				else
				{
					var fragment = new DnaSeq(seqs[index].GetBits());
					int freq = 0;
					for (int i = 0; i < len; i++)
						freq += kna[cnt + i].GetCount(fragment);
					Console.WriteLine("{0}\t{1}", freq, fragment);
				}
				index++;
				return cnt + len;
			});
		}
		finally
		{

			Console.Error.WriteLine("Took " + sw.WallClockMilliseconds() / 1000.0 + "s; t0: " + sw.CpuMilliseconds() / 1000.0 + "s; " + GC.GetTotalMemory(false) / 1024 / 1024 + "MB");
		}
	}

	static void CountFrequencies()
	{
		int index;
		while ((index = Interlocked.Increment(ref Current)) < TaskCount)
			kna[index].KFrequency();
	}

}

public class KNucleotide
{
	class Count { public int V;}

	readonly Dictionary<DnaSeq, Count> frequencies
		= new Dictionary<DnaSeq, Count>();

	readonly BitArray sequence;
	readonly int length;
	readonly int frame;

	public KNucleotide(BitArray s, int l, int f)
	{
		sequence = s; length = l; frame = f;
	}

	public void AddFrequencies(KNucleotide other)
	{
		foreach (var kvp in other.frequencies)
		{
			Count count;
			if (frequencies.TryGetValue(kvp.Key, out count))
				count.V += kvp.Value.V;
			else
				frequencies[kvp.Key] = kvp.Value;
		}
	}

	public void WriteFrequencies()
	{
		var items = new List<KeyValuePair<DnaSeq, Count>>(frequencies);
		items.Sort(SortByFrequencyAndCode);
		double percent = 100.0 / (sequence.Length / 2 - length + 1);
		foreach (var item in items)
			Console.WriteLine("{0} {1:f3}",
						item.Key.ToString(), item.Value.V * percent);
		Console.WriteLine();
	}

	public int GetCount(DnaSeq fragment)
	{
		Count count;
		if (!frequencies.TryGetValue(fragment, out count))
			count = new Count();
		return count.V;
	}

	public void KFrequency()
	{
		int n = sequence.Length / 2 - length + 1;
		for (int i = frame; i < n; i += length)
		{
			var key = new DnaSeq(sequence, i, length);
			Count count;
			if (frequencies.TryGetValue(key, out count))
				count.V++;
			else
				frequencies[key] = new Count { V = 1 };
		}
	}

	int SortByFrequencyAndCode(
			KeyValuePair<DnaSeq, Count> i0,
			KeyValuePair<DnaSeq, Count> i1)
	{
		int order = i1.Value.V.CompareTo(i0.Value.V);
		if (order != 0) return order;
		return i0.Key.ToString().CompareTo(i1.Key.ToString());
	}
}

