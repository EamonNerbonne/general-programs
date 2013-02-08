/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/

   contributed by Eamon Nerbonne
*/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

static class Program {
	const string bases = "ACGT";
	static byte?[] toBase = new byte?['t' + 1];

	public static void Main() {
		for (var i = 0; i < 4; i++)
			toBase["acgt"[i]] = (byte)i;

		//Start concurrent workers that will count dna fragments
		var workers = new[] { 1, 2, 3, 4, 6, 12, 18 }.Select(len => {
			var queue = new BlockingCollection<byte[]>(4);
			return new {
				len,
				queue,
				task = Task.Factory.StartNew(
					() =>
						//use a sparse hash (dictionary) for longer fragments
						len > 8 ? new Sparse(queue.GetConsumingEnumerable(), len)
						//...and use a dense hash (aka array) for very short fragments.
						: (ICounter)new Dense(queue.GetConsumingEnumerable(), len),
					TaskCreationOptions.LongRunning)
			};
		}).ToArray();

		//Read lines into chunks.  The exact size isn't that important.
		//Smaller chunks are more concurrent but less CPU efficient.
		foreach (var chunk in LinesToChunks(1 << 16))
			//Pass chunks into concurrent consumers; add to last workers first
			//as a minor threading optimization.
			foreach (var w in workers.Reverse())
				w.queue.Add(chunk);

		foreach (var w in workers.Reverse())
			w.queue.CompleteAdding();

		//Show output for each consumer
		foreach (var w in workers) {
			if (w.len < 3)
				Console.WriteLine(((Dense)w.task.Result).Summary(w.len));
			else {
				var dna = "ggtattttaatttatagt".Substring(0, w.len);
				Console.WriteLine(
					w.task.Result.Count(dna.Reverse().Aggregate(0ul,
							(v, c) => v << 2 | toBase[c].Value))
					+ "\t" + dna.ToUpper()
				);
			}
		}
	}

	static IEnumerable<byte[]> LinesToChunks(int size) {
		string line;
		while ((line = Console.ReadLine()) != null)
			if (line.StartsWith(">THREE"))
				break;

		//we just skipped all lines upto section three

		int i = 0;
		var arr = new byte[size];

		while ((line = Console.ReadLine()) != null)
			foreach (var c in line) {
				arr[i++] = toBase[c].Value;
				if (i == size) {
					//ok, our batch is full, so yield it to consumers.
					yield return arr;
					i = 0;
					arr = new byte[size];
				}
			}

		if (i > 0) {
			//last batch isn't entirely full, but don't forget it.
			Array.Resize(ref arr, i);
			yield return arr;
		}
	}

	static void Init<T>(T impl, IEnumerable<byte[]> seq, int len)
		where T : ICounter {
		int i = 0;
		ulong dna = 0; //represent dna bases as the rightmost packed bits
		foreach (var arr in seq)
			foreach (ulong b in arr) {
				dna = dna >> 2 | b << len * 2 - 2;//push into packed bits
				i++;
				if (i >= len) //only count dna if its already long enough
					impl.Add(dna);
			}
	}

	struct Dense : ICounter {
		public Dense(IEnumerable<byte[]> seq, int len) {
			counts = new int[1 << len * 2];
			Init(this, seq, len);
		}
		int[] counts;
		public void Add(ulong dna) { counts[dna]++; }
		public int Count(ulong dna) { return counts[dna]; }
		public string Summary(int len) {
			var scale = 100.0 / counts.Sum();
			return string.Concat(
				counts.Select((c, dna) => new {
					p = c * scale,
					dna = string.Concat(Enumerable.Range(0, len)
									.Select(i => bases[dna >> i * 2 & 3]))
				})
					.OrderByDescending(x => x.p).ThenBy(x => x.dna)
					.Select(x => x.dna + " " + x.p.ToString("f3") + "\n")
				);
		}
	}

	struct Sparse : ICounter {
		public Sparse(IEnumerable<byte[]> seq, int len) {
			counts = new Dictionary<ulong, IntRef>(1 << 16);
			Init(this, seq, len);
		}
		Dictionary<ulong, IntRef> counts;
		public void Add(ulong dna) {
			IntRef count;
			if (!counts.TryGetValue(dna, out count))
				counts[dna] = new IntRef { val = 1 };
			else
				count.val++;
		}
		public int Count(ulong dna) {
			IntRef count;
			return counts.TryGetValue(dna, out count) ? count.val : 0;
		}
	}

	class IntRef { public int val; }
	interface ICounter {
		void Add(ulong dna);
		int Count(ulong dna);
	}
}