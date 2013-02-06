using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

enum Base : byte { A, C, G, T }

static class Program {
	public static Base ToBase(this char c) {
		if (c == 'A' || c == 'a') return Base.A;
		if (c == 'C' || c == 'c') return Base.C;
		if (c == 'G' || c == 'g') return Base.G;
		if (c == 'T' || c == 't') return Base.T;
		throw new ArgumentOutOfRangeException("c");
	}

	public static IEnumerable<string> Lines(this TextReader inp) {
		for (var line = inp.ReadLine(); line != null; line = inp.ReadLine())
			yield return line;
	}

	static IEnumerable<T[]> Batch<T>(this IEnumerable<T> list, int batchSize) {
		int i = 0;
		T[] arr = new T[batchSize];
		foreach (var t in list) {
			arr[i++] = t;
			if (i == batchSize) {
				yield return arr;
				i = 0;
				arr = new T[batchSize];
			}
		}
		if (i > 0) {
			Array.Resize(ref arr, i);
			yield return arr;
		}
	}

	public static void Main(string[] args) {
		var sw = Stopwatch.StartNew();

		var workers =
			new[] { 1, 2, 3, 4, 6, 12, 18 }.Select(length => {
				var queue = new BlockingCollection<Base[]>(4);
				var list = queue.GetConsumingEnumerable();
				return new {
					queue,
					task = Task.Factory.StartNew(
						() => new DnaStats(length, list),
						TaskCreationOptions.LongRunning)
				};
			}).ToArray();

		var console_In = args.Length > 0 ? File.OpenText(args[0]) : Console.In; //!

		var batches = console_In.Lines()
			.SkipWhile(s => !s.StartsWith(">THREE")).Skip(1)
			.TakeWhile(s => !s.StartsWith(">")).Where(s => !s.StartsWith(";"))
			.SelectMany(s => s).Select(ToBase).Batch(1024 * 64);

		foreach (var batch in batches)
			foreach (var worker in workers.Reverse())
				worker.queue.Add(batch);

		foreach (var worker in workers.Reverse())
			worker.queue.CompleteAdding();

		var fragments = new[] {
			"GGT", "GGTA", "GGTATT", "GGTATTTTAATT",
			"GGTATTTTAATTTATAGT"
		};

		Console.WriteLine(workers[0].task.Result.Summary());
		Console.WriteLine(workers[1].task.Result.Summary());

		foreach (var result in fragments.Zip(workers.Skip(2), (dna, stats) =>
			stats.task.Result.GetCount(dna.Select(ToBase).ToArray()) + "\t" + dna))
			Console.WriteLine(result);
		Console.Error.WriteLine("Took " + sw.Elapsed.TotalSeconds + "s; " + GC.GetTotalMemory(false) / 1024 / 1024 + "MB");
	}
}

class DnaStats {
	public Func<Base[], int> GetCount;
	public Func<string> Summary;

	public DnaStats(int length, IEnumerable<Base[]> seq) {
		if (length > 8)
			Init<NormalHash>(seq, length);
		else
			Init<ArrayHash>(seq, length);
	}

	void Init<T>(IEnumerable<Base[]> seq, int len)
		where T : struct, ICounter {
		T impl = new T();
		impl.Init(len);
		int total = 0;
		ulong current = 0;
		foreach (var seg in seq)
			foreach (var b in seg) {
				current = current >> 2 | (ulong)b << len * 2 - 2;
				total++;
				if (total < len) continue;
				impl.Add(current);
			}

		GetCount = dna =>
			impl.GetCount(
				dna.Select((b, i) => (ulong)b << 2 * i)
					.Aggregate((x, y) => x | y));
		Summary = () => impl.Summary(len, total);
	}

	interface ICounter {
		void Init(int len);
		void Add(ulong dna);
		int GetCount(ulong dna);
		string Summary(int len, int total);
	}

	struct ArrayHash : ICounter {
		int[] counts;
		public void Init(int len) { counts = new int[1 << len * 2]; }
		public void Add(ulong dna) { counts[dna]++; }
		public int GetCount(ulong dna) { return counts[dna]; }

		public string Summary(int len, int total) {
			return string.Concat(
				counts.Select((c, dna) => new {
					p = c * 100.0 / (total - len + 1),
					dna = string.Concat(All((ulong)dna, len))
				})
					.OrderByDescending(x => x.p).ThenBy(x => x.dna)
					.Select(x => x.dna + " " + x.p.ToString("f3") + "\n")
				);
		}
	}

	struct NormalHash : ICounter {
		class IntRef { public int val; }
		Dictionary<ulong, IntRef> counts;
		public void Init(int len) {
			counts = new Dictionary<ulong, IntRef>(1 << 16);
		}
		public void Add(ulong dna) {
			IntRef count;
			if (!counts.TryGetValue(dna, out count))
				counts[dna] = count = new IntRef();
			count.val++;
		}
		public int GetCount(ulong dna) {
			IntRef count;
			return counts.TryGetValue(dna, out count) ? count.val : 0;
		}
		public string Summary(int len, int total) { return null; }
	}

	static IEnumerable<Base> All(ulong dna, int l) {
		return Enumerable.Range(0, l).Select(i => (Base)(dna >> i * 2 & 3));
	}
}