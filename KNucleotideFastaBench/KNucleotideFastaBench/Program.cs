using System;
using System.Collections.Concurrent;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

		var workers = new[] { 1, 2, 3, 4, 6, 12, 18 }.Select(l => {
			var queue = new BlockingCollection<Base[]>(8);
			/*
			return new {
				queue,
				task = Task.Factory.StartNew(() => DnaStats.Create(l, queue.GetConsumingEnumerable()), TaskCreationOptions.LongRunning)
			};
			 /*/
			var tcs = new TaskCompletionSource<DnaStats>();
			new Thread(() => tcs.SetResult(DnaStats.Create(l, queue.GetConsumingEnumerable()))) {
				Priority = ThreadPriority.BelowNormal
			}.Start();
			return new { queue, task = tcs.Task };
			/**/
		}).ToArray();

		var console_In = args.Length > 0 ? File.OpenText(args[0]) : Console.In;
		var batches = console_In.Lines()
			.SkipWhile(s => !s.StartsWith(">THREE")).Skip(1)
			.TakeWhile(s => !s.StartsWith(">")).Where(s => !s.StartsWith(";"))
			.SelectMany(s => s).Select(ToBase).Batch(1024 * 64);

		foreach (var batch in batches)
			foreach (var worker in workers.Reverse())
				worker.queue.Add(batch);

		foreach (var worker in workers.Reverse())
			worker.queue.CompleteAdding();

		var fragments = new[] { "GGT", "GGTA", "GGTATT", "GGTATTTTAATT", 
				"GGTATTTTAATTTATAGT" };

		Console.WriteLine(workers[0].task.Result.Summary());
		Console.WriteLine(workers[1].task.Result.Summary());

		foreach (var result in fragments.Zip(workers.Skip(2), (frag, h) => h.task.Result.GetCount(frag.Select(ToBase).ToArray()) + "\t" + frag))
			Console.WriteLine(result);
		Console.Error.WriteLine("Took " + sw.WallClockMilliseconds() / 1000.0 + "s; t0: " + sw.CpuMilliseconds() / 1000.0 + "s; " + GC.GetTotalMemory(false) / 1024 / 1024 + "MB");
		//Console.ReadLine();
	}
}

struct DnaStats
{
	public Func<Base[], int> GetCount;
	public Func<string> Summary;

	public static DnaStats Create(int length, IEnumerable<Base[]> seq)
	{
		return length > 8 ? Impl<SparseCounter>(length, seq)
			: Impl<DenseCounter>(length, seq);
	}

	static DnaStats Impl<T>(int length, IEnumerable<Base[]> seq)
	where T : struct, ICounter
	{
		T impl = new T();
		impl.Init(length);
		int total = 0;
		ulong dna = 0;
		foreach (var seg in seq)
			foreach (var b in seg)
			{
				dna = Push(dna, b, length);
				total++;
				if (total < length) continue;
				impl.Add(dna);
			}
		return new DnaStats {
			GetCount = frag => impl.GetCount(DnaFragment(frag, 0, frag.Length)),
			Summary = () => impl.Summary(length, total),
		};
	}

	interface ICounter
	{
		void Init(int len);
		void Add(ulong dna);
		int GetCount(ulong dna);
		string Summary(int len, int total);
	}

	public struct DenseCounter : ICounter
	{
		int[] counts;
		public void Init(int len) { counts = new int[1 << len * 2]; }
		public void Add(ulong dna) { counts[dna]++; }
		public int GetCount(ulong dna) { return counts[dna]; }

		public string Summary(int len, int total)
		{
			return string.Concat(
				counts.Select((c, dna) => new {
					p = c * 100.0 / (total - len + 1),
					dna = Stringify((ulong)dna, len)
				})
				.OrderByDescending(x => x.p).ThenBy(x => x.dna)
				.Select(x => x.dna + " " + x.p.ToString("f3") + "\n")
				);
		}
	}

	struct SparseCounter : ICounter
	{
		class Count { public int V;}
		Dictionary<ulong, Count> counts;
		public void Init(int len) { counts = new Dictionary<ulong, Count>(1<<16); }
		public string Summary(int len, int total) { return null; }

		public void Add(ulong dna)
		{
			Count count;
			if (counts.TryGetValue(dna, out count))
				count.V++;
			else
				counts[dna] = new Count { V = 1 };
		}
		public int GetCount(ulong dna)
		{
			Count count;
			return counts.TryGetValue(dna, out count) ? count.V : 0;
		}
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
}
