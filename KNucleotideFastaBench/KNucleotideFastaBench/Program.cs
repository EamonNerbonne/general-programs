using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

enum Base : byte { A, C, G, T }

static class Program {
	public static void Main() {
		//Start concurrent workers that will count dna fragments
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
		
		//Read lines into chunks.  The exact size isn't that important.
		//Smaller chunks are more concurrent but less CPU efficient.
		var batches = ReadLinesIntoChunks(64 * 1024);

		//Pass chunks into concurrent consumers
		foreach (var batch in batches)
			foreach (var worker in workers.Reverse())
				worker.queue.Add(batch);

		foreach (var w in workers)
			w.queue.CompleteAdding();


		var fragments = new[] {
			"GGT", "GGTA", "GGTATT", "GGTATTTTAATT",
			"GGTATTTTAATTTATAGT"
		};

		//Show output for each consumer
		for (int i = 0; i < workers.Length; i++) {
			var stats = workers[i].task.Result;
			if (i < 2)
				Console.WriteLine(stats.Summary());
			else {
				var dna = fragments[i - 2];
				Console.WriteLine(
					stats.CountOf(dna.Select(c => toBase[c].Value).ToArray())
						+ "\t" + dna
					);
			}
		}
	}


	static IEnumerable<Base[]> ReadLinesIntoChunks(int batchSize) {
		string line;
		do {
			line = Console.ReadLine();
		} while (line != null && !line.StartsWith(">THREE"));
		//we just skipped all lines upto section three

		int i = 0;
		var arr = new Base[batchSize];

		while (true) {
			line = Console.ReadLine();
			if (line == null || line.StartsWith(">"))
				break; //stop when end or new section is reached
			if (!line.StartsWith(";")) //ignore comments
				foreach (var c in line) {
					arr[i++] = toBase[c].Value;
					if (i == batchSize) {
						//ok, our batch is full, so yield it to consumers.
						yield return arr;
						i = 0;
						arr = new Base[batchSize];
					}
				}
		}

		if (i > 0) {
			//last batch isn't entirely full, but don't forget it.
			Array.Resize(ref arr, i);
			yield return arr;
		}
	}

	static readonly Base?[] toBase = Enumerable.Range(0, 't' + 1).Select(i => {
		Base b;
		return Enum.TryParse(((char)i).ToString(), true, out b) ? b : default(Base?);
	}).ToArray();
}

class DnaStats {
	public Func<Base[], int> CountOf;
	public Func<string> Summary;

	public DnaStats(int length, IEnumerable<Base[]> seq) {
		if (length > 8)  //use a sparse hash (dictionary) for longer fragments
			Init<NormalHash>(seq, length); 
		else //...and use a dense hash (aka array) for very short fragments.
			Init<ArrayHash>(seq, length);
	}

	void Init<T>(IEnumerable<Base[]> seq, int len)
		where T : struct, ICounter {
		T impl = new T();
		impl.Init(len);
		int total = 0;
		ulong current = 0; //represents bases as the rightmost packed bits
		foreach (var seg in seq)
			foreach (var b in seg) {
				//push new base into packed representation:
				current = current >> 2 | (ulong)b << len * 2 - 2;
				total++;
				
				if (total < len) continue;
				//only count this if we have filled our representation.
				impl.Add(current);
			}

		CountOf = dna =>
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