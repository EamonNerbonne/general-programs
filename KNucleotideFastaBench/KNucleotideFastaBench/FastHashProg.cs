#define FASTHASH
/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/

   contributed by Eamon Nerbonne
*/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
      var queue = new BlockingCollection<byte[]>(1);
      return new {
        len,
        queue,
        task = Task.Factory.StartNew(
          () =>
            //use a sparse hash (dictionary) for longer fragments
            len > 8 ? new Sparse(queue, len)
            //...and use a dense hash (aka array) for very short fragments.
            : (ICounter)new Dense(queue, len),
          TaskCreationOptions.LongRunning)
      };
    }).ToArray();

    //Read lines into chunks.  The exact size isn't that important.
    //Smaller chunks are more concurrent but less CPU efficient.
    foreach (var chunk in LinesToChunks(1 << 16))
      foreach (var w in workers)
        w.queue.Add(chunk);

    //Show output for each consumer
    foreach (var w in workers) {
      w.queue.CompleteAdding();
      if (w.len < 3)
        Console.WriteLine(((Dense)w.task.Result).Summary(w.len));
      else {
        var dna = "ggtattttaatttatagt".Substring(0, w.len);
        Console.WriteLine(
          w.task.Result.Count(dna.Aggregate(0ul,
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
    //ulong dna = 0;

    while ((line = Console.ReadLine()) != null)
      foreach (var c in line) {
        //dna = dna << 2 | toBase[c].Value;
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

  struct Dense : ICounter {
    public Dense(BlockingCollection<byte[]> seq, int len) {
      counts = new int[1 << len * 2];
      int mask = (1 << len * 2) - 1;
      int i = 0;
      int dna = 0;
      foreach (var arr in seq.GetConsumingEnumerable()) {
        int j = 0;
        while (i < len - 1 && j < arr.Length) {
          dna = dna << 2 & mask | arr[j++];
          i++;
        }
        while (j < arr.Length)
          counts[dna = dna << 2 & mask | arr[j++]]++;
      }
    }
    int[] counts;
    public int Count(ulong dna) { return counts[(int)dna]; }
    public string Summary(int len) {
      var scale = 100.0 / counts.Sum();
      return string.Concat(
        counts.Select((c, dna) => new {
          p = c * scale,
          dna = string.Concat(Enumerable.Range(0, len).Reverse()
                  .Select(i => bases[dna >> i * 2 & 3]))
        })
          .OrderByDescending(x => x.p).ThenBy(x => x.dna)
          .Select(x => x.dna + " " + x.p.ToString("f3") + "\n")
        );
    }
  }

  struct FastHash {
    struct Entry {
      public ulong key;
      public int value;
      public int next;
    }

    public FastHash(int bits) {
      table = new Entry[1 << bits];
      overflow = new Entry[1 << bits - 1];
      mask = table.Length - 1;
      nextOverflow = 0;
    }
    Entry[] table, overflow;
    int mask, nextOverflow;

    // borrowed from JDK
    static int hash(ulong h) {
      h ^= (h >> 20) ^ (h >> 12);
      return (int)(h ^ (h >> 7) ^ (h >> 4));
    }

    public int get(ulong key) {
      int idx = hash(key) & mask;
      if (table[idx].value == 0) return 0;
      else if (table[idx].key == key)
        return table[idx].value;
      else {
        for (idx = table[idx].next; idx != -1; idx = overflow[idx].next)
          if (overflow[idx].key == key)
            return overflow[idx].value;
        return 0;
      }
    }

    public void inc(ulong key, int by) {
      int idx = hash(key) & mask;
      if (table[idx].value == 0)
        table[idx] = new Entry { key = key, value = by, next = -1 };
      else if (table[idx].key == key)
        table[idx].value += by;
      else if (table[idx].next == -1) {
        table[idx].next = nextOverflow;
        overflow[nextOverflow++] = new Entry { key = key, value = by, next = -1 };
        EnsureOverflowOk();
      }
      else { //there already is an overflow!
        idx = table[idx].next;
        while (true) {
          if (overflow[idx].key == key) {
            overflow[idx].value += by;
            break;
          }
          else if (overflow[idx].next == -1) {
            overflow[idx].next = nextOverflow;
            overflow[nextOverflow++] = new Entry { key = key, value = by, next = -1 };
            EnsureOverflowOk();
            break;
          }
          else {
            idx = overflow[idx].next;
          }
        }
      }
    }

    private void EnsureOverflowOk() {
      if (nextOverflow < overflow.Length) return;
      var oldTable = table;
      var oldOverflow = overflow;
      //since idx is a bitmasked hash, a larger table means a larger bitmask
      //this means the new idxs cannot collide except if old indexes did.
      //and that means we can resize the core table without using overflow.
      table = new Entry[table.Length * 2];
      mask = table.Length - 1;
      for (int i = 0; i < oldTable.Length; i++) {
        var entry = oldTable[i];
        if (entry.value > 0) {
          entry.next = -1;
          table[hash(entry.key) & mask] = entry;
        }
      }
      //now we re-add all the overflows...
      //...how fortunate, the old table is already the right size.
      overflow = oldTable;
      nextOverflow = 0;
      for (int i = 0; i < oldOverflow.Length; i++) {
        var entry = oldOverflow[i];
        entry.next = -1;
        var idx = hash(entry.key) & mask;
        if (table[idx].value == 0) {
          table[idx] = entry;
        }
        else if (table[idx].next == -1) {
          table[idx].next = nextOverflow;
          overflow[nextOverflow++] = entry;
        }
        else { //there already is an overflow!
          idx = table[idx].next;
          while (overflow[idx].next != -1)
            idx = overflow[idx].next;

          overflow[idx].next = nextOverflow;
          overflow[nextOverflow++] = entry;
        }
      }
    }
  }


  //struct Buffered<T> {
  //  public T[] arr;
  //  public int length;
  //  public Task<Buffered<T>> next;

  //  public struct Builder {
  //    Buffered<T> buffer;
  //    readonly TaskCompletionSource<Buffered<T>> builder;
  //    public Task<Buffered<T>> Initial() { return builder.Task; }

  //    public Builder(int size) {
  //      builder = new TaskCompletionSource<Buffered<T>>();
  //      buffer = new Buffered<T> { length = 0, arr = new T[size] };
  //    }

  //    public void Add(T item) {
  //      buffer.arr[buffer.length++] = item;
  //      if (buffer.length == buffer.arr.Length) {
  //        var nextStage = new Builder(buffer.arr.Length);
  //        buffer.next = nextStage.Initial();
  //        builder.SetResult(buffer);
  //        this = nextStage;
  //      }
  //    }

  //    public void Finish() { builder.SetResult(buffer); }
  //  }
  //}

  struct Sparse : ICounter {
    struct Buffer {
      public class Chunk { public ulong[] Arr; public int Len;}
      public BlockingCollection<Chunk> chunks;
      ulong[] Arr;
      int Len;

      public Buffer(int size) { Len = 0; Arr = new ulong[size]; chunks = new BlockingCollection<Chunk>(1); }
      public void Add(ulong data) {
        if (Len == Arr.Length) {
          chunks.Add(new Chunk { Arr = Arr, Len = Len });
          Arr = new ulong[Len];
          Len = 0;
        }
        Arr[Len++] = data;
      }

      public void Done() {
        if (Len > 0)
          chunks.Add(new Chunk { Arr = Arr, Len = Len });
        chunks.CompleteAdding();
      }
    }

    public Sparse(BlockingCollection<byte[]> seq, int len) {
      var mask = (1ul << len * 2) - 1;
#if FASTHASH
      counts = new FastHash(5);
      int i = 0;
      ulong dna = 0;
      foreach (var arr in seq.GetConsumingEnumerable())
        foreach (var b in arr) {
          dna = dna << 2 & mask | b;
          i++;

          if (i >= len) //only count dna if its already long enough
            counts.inc(dna, 1);
        }

#elif false
      counts = new Dictionary<ulong, IntRef>();
      int i = 0;
      ulong dna = 0;
      foreach (var arr in seq.GetConsumingEnumerable())
        foreach (var b in arr) {
          dna = dna << 2 & mask | b;
          i++;

          if (i >= len) //only count dna if its already long enough
            Add(counts, dna, 1);
        }
#else
      var buffers = new[] { new Buffer(1 << 16), new Buffer(1 << 16), new Buffer(1 << 16), new Buffer(1 << 16) };
      var dicts = buffers.Select(buf => buf.chunks).Select(chunks =>
        Task.Factory.StartNew(() => {
          var d = new Dictionary<ulong, IntRef>();
          foreach (var chunk in chunks.GetConsumingEnumerable())
            if (chunk.Len == chunk.Arr.Length)
              foreach (var data in chunk.Arr)
                Add(d, data, 1);
            else
              foreach (var data in chunk.Arr.Take(chunk.Len))
                Add(d, data, 1);
          return d;
        }, TaskCreationOptions.LongRunning)).ToArray();

      int i = 0;
      ulong dna = 0;
      foreach (var arr in seq.GetConsumingEnumerable())
        foreach (var b in arr) {
          dna = dna << 2 & mask | b;
          i++;
          if (i >= len)
            buffers[dna & 3].Add(dna);
        }

      foreach (var buf in buffers) buf.Done();

      counts = dicts.Select(t => t.Result).Aggregate((a, b) => {
        foreach (var kv in b)
          a.Add(kv.Key, kv.Value);
        return a;
      });

#endif
      //var first = seq.GetConsumingEnumerable().First();

      //counts = Enumerable.Range(0, 2).Select(p =>
      //  Task.Factory.StartNew(() => {
      //    var d = new Dictionary<ulong, IntRef>();
      //    if (p == 0)
      //      foreach (var dna in first.Skip(len - 1))
      //        //only count dna if its already long enough
      //        Add(d, dna & mask, 1);

      //    foreach (var arr in seq.GetConsumingEnumerable())
      //      foreach (var dna in arr)
      //        //only count dna if its already long enough
      //        Add(d, dna & mask, 1);
      //    return d;
      //  })
      //).ToArray().Select(t => t.Result).Aggregate((a, b) => {
      //  foreach (var kv in b)
      //    Add(a, kv.Key, kv.Value.val);
      //  return a;
      //});
    }
#if FASTHASH
    FastHash counts;
#else
    Dictionary<ulong, IntRef> counts;
#endif
    static void Add(Dictionary<ulong, IntRef> dict, ulong dna, int x) {
      IntRef count;
      if (!dict.TryGetValue(dna, out count))
        dict[dna] = new IntRef { val = x };
      else
        count.val += x;
    }

    public int Count(ulong dna) {
#if FASTHASH
      return counts.get(dna);
#else
      IntRef count;
      return counts.TryGetValue(dna, out count) ? count.val : 0;
#endif
    }
  }
  class IntRef { public int val; }
  interface ICounter {
    //void Add(ulong dna);
    int Count(ulong dna);
  }
}