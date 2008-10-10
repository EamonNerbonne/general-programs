#define USEFAST
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using EamonExtensionsLinq.Filesystem;
using System.Text.RegularExpressions;
using System.Reflection;

namespace WikiParser
{
    struct RankedNgram 
    {
        public ulong ngramK;
        public int rank;
        const int BITOFFSET = 4 * 8;
        public static ulong CreateFromBytes(byte[] arr) {
            return CreateFromBytes(arr, 0, arr.Length);
        }
        public static ulong CreateFromBytes(byte[] arr, int start, int end) {
            ulong retval = 0;
            int offset = BITOFFSET;
            while (start != end) {
                retval |= (ulong)arr[start] << offset;
                start++; offset -= 8;
            }
            return retval;
        }
        public static ulong[] CreateFromByteString(byte[] arr) {
            return CreateFromByteString(arr, 0, arr.Length);
        }
        public static ulong[] CreateFromByteString(byte[] arr, int start, int end) {
            int len = end - start;
            //0,1,3,6..10..15..20.....
            int count =
                len == 0 ? 0 :
                len == 1 ? 1 :
                len == 2 ? 3 :
                len == 3 ? 6 :
                len == 4 ? 10 :
                len * 5 - 10;
            ulong[] retval = new ulong[count];
            int next = 0;
            ulong b;

            if (start == end) return retval;
            b = ((ulong)arr[start]);// <<BITOFFSET;
            retval[next] = b << BITOFFSET; next++;
            start++;

            if (start == end) return retval;
            b = ((ulong)arr[start]); //<< BITOFFSET;
            retval[next] = b << BITOFFSET; next++;
            retval[next] = retval[next - 2] | (b << (BITOFFSET-8)); next++;
            start++;

            if (start == end) return retval;
            b = ((ulong)arr[start]);// << BITOFFSET;
            retval[next] = b << BITOFFSET; next++;
            retval[next] = retval[next - 3] | (b << (BITOFFSET - 8)); next++;
            retval[next] = retval[next - 3] | (b << (BITOFFSET - 16)); next++;
            start++;

            if (start == end) return retval;
            b = ((ulong)arr[start]);// << BITOFFSET;
            retval[next] = b << BITOFFSET; next++;
            retval[next] = retval[next - 4] | (b << (BITOFFSET - 8)); next++;
            retval[next] = retval[next - 4] | (b << (BITOFFSET - 16)); next++;
            retval[next] = retval[next - 4] | (b << (BITOFFSET - 24)); next++;
            start++;

            if (start == end) return retval;
            b = ((ulong)arr[start]);// << BITOFFSET;
            retval[next] = b << BITOFFSET; next++;
            retval[next] = retval[next - 5] | (b << (BITOFFSET - 8)); next++;
            retval[next] = retval[next - 5] | (b << (BITOFFSET - 16)); next++;
            retval[next] = retval[next - 5] | (b << (BITOFFSET - 24)); next++;
            retval[next] = retval[next - 5] | b; next++;
            start++;
 
            while (start < end) {
                b = ((ulong)arr[start]);// << BITOFFSET;
                retval[next] = b << BITOFFSET; next++;
                retval[next] = retval[next - 6] | (b << (BITOFFSET - 8)); next++;
                retval[next] = retval[next - 6] | (b << (BITOFFSET - 16)); next++;
                retval[next] = retval[next - 6] | (b << (BITOFFSET - 24)); next++;
                retval[next] = retval[next - 6] | b ; next++;
                start++;
            }

            return retval;
        }
        public override string ToString() {
            return
                Encoding.Default.GetString(new[] {
                    (byte)(ngramK >> 32), 
                    (byte)(ngramK >> 24),
                    (byte)((ngramK >> 16) & 0xff), 
                    (byte)((ngramK >> 8) & 0xff), 
                    (byte)((ngramK) & 0xff),
                }.Where(b => b != 0).ToArray())
                  + ": " + rank;
        }
    }
    public class LMStats
    {
        const int NumberOfRanks = 400;
        static Regex lineparser;
        static Regex wordSplitter;
        static LMStats() {
            lineparser = new Regex(@"^(?<ngram>.*?)\t (?<count>\d+)$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);
            wordSplitter = new Regex(@"[^0-9\s]+", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);
        }
        Dictionary<string, int> ngramToRank;
        Dictionary<string, int> ngramToCount;
        KeyValuePair<string, int>[] orderedNgrams;
        RankedNgram[] orderedK;
        public string Name { get; private set; }
        public int HitCounter { get; set; }
        public int HitRun { get; set; }
        public LMStats(FileInfo fromfile, bool initCounts) {
            int rank = 0;
            if (initCounts)
                ngramToCount = new Dictionary<string, int>();
            ngramToRank = new Dictionary<string, int>();
            List<RankedNgram> tmp = new List<RankedNgram>();
            foreach (var line in fromfile.GetLines(Encoding.Default)) {
                var match = lineparser.Match(line);
                string ngram = match.Groups["ngram"].Value;
                int count = Int32.Parse(match.Groups["count"].Value);
                tmp.Add(new RankedNgram {
                    ngramK = RankedNgram.CreateFromBytes(Encoding.Default.GetBytes(ngram)),
                    rank = rank
                });
                if (initCounts)
                    ngramToCount[ngram] = count;
                ngramToRank[ngram] = rank;
                rank++;
            }
            orderedK = tmp.OrderBy(n => n.ngramK).ToArray();
            orderedNgrams = ngramToRank.OrderBy(kv => kv.Key, StringComparer.Ordinal).ToArray();
            Name = Path.GetFileNameWithoutExtension(fromfile.FullName);
        }

        public void AppendToLog(string sentence,string title) {
            string log=string.Format("============{0} {2}\n{1}\n",string.Join("",Enumerable.Repeat(".", HitRun).ToArray()),  sentence,title );
            File.AppendAllText(Name + ".log",log ,Encoding.UTF8);
        }

        public LMStats(string fromstring, bool initCounts, string name) {
            Name = name;
#if !USEFAST
            var sortedNgrams =
                MakeNgramsFromSentence(fromstring)
                .GroupBy(ngram => ngram)
                .Select(g => new { Ngram = g.Key, Count = g.Count() })
                .OrderByDescending(p => p.Count)
                .ThenBy(p=>p.Ngram,StringComparer.Ordinal)
                .Take(NumberOfRanks)
                .Select((p, i) => new { p.Ngram, p.Count, Rank = i })
                .ToArray();
                        ngramToRank = sortedNgrams.ToDictionary(p => p.Ngram, p => p.Rank);
            orderedNgrams = ngramToRank.OrderBy(kv => kv.Key, StringComparer.Ordinal).ToArray();
            if (initCounts)
                ngramToCount = sortedNgrams.ToDictionary(p => p.Ngram, p => p.Count);

#else
            var ngramsK = MakeNgramsKFromSentence(fromstring);
            Array.Sort(ngramsK);//direct sort seems faster.
            //ngramsK = ngramsK.OrderBy(n => n).ToArray();//TODO:sort?
            List<RankedNgram> ngramsG = new List<RankedNgram>();
            ulong lastkey = ngramsK[0];
            int lastkeycount=0;
            foreach (ulong ngramK in ngramsK) {
                if (ngramK == lastkey) {
                    lastkeycount++;
                } else {
                    ngramsG.Add(new RankedNgram {
                        ngramK = lastkey,
                        rank = lastkeycount
                    });
                    lastkey = ngramK;
                    lastkeycount = 1;
                }
            }
            ngramsG.Add(new RankedNgram {
                ngramK = lastkey,
                rank = lastkeycount
            });
            ngramsG.Sort((a, b) => b.rank - a.rank);
            orderedK = 
                ngramsG
//                .OrderByDescending(p=>p.rank)
                .Take(NumberOfRanks)//direct sort seems faster.
                .Select((p,i)=>new RankedNgram {ngramK= p.ngramK,rank=i})
                .OrderBy(p=>p.ngramK)
                . ToArray();
#endif           
        }
        static IEnumerable<string> MakeWords(string sentence) {
            return wordSplitter.Matches(sentence).Cast<Match>().Select(m => m.Value);
        }
        static IEnumerable<string> MakeNgramsFromSentence(string sentence) {
            return MakeWords(sentence).SelectMany<string, string>(MakeNgramsFromWord);
        }
        static ulong[] MakeNgramsKFromSentence(string sentence) {
            return MakeWords(sentence).SelectMany(w => MakeNgramsKFromWord(w)).ToArray();
        }
        static IEnumerable<string> MakeNgramsFromWord(string word) {
            word = Encoding.Default.GetString( Encoding.Default.GetBytes("_" + word + "_"));
            for (int i = 0; i < word.Length; i++) {
                int upto = Math.Min(word.Length - i, 5);
                for (int j = 1; j <= upto; j++) {
                    yield return word.Substring(i, j);
                }
            }
        }


        static ulong[] MakeNgramsKFromWord(string word) {
            word = "_" + word + "_";
            byte[] bytes = Encoding.Default.GetBytes(word);
            return RankedNgram.CreateFromByteString(bytes);
        }



        public int CompareTo(LMStats other) {
            int damage = 0;
            var orderedS = orderedNgrams;
            var orderedO = other.orderedNgrams;
            int si = 0, oi = 0;
            while (si < orderedS.Length) {
                int lastCompare = 0;
                string s = orderedS[si].Key;
                while (oi < orderedO.Length && (lastCompare = string.CompareOrdinal(s, orderedO[oi].Key)) > 0) oi++;
                if (oi == orderedO.Length) {
                    damage += (orderedS.Length - si) * NumberOfRanks;
                    break;
                }
                damage += (lastCompare == 0) ?
                    Math.Abs(orderedO[oi].Value - orderedS[si].Value) :
                    NumberOfRanks;
                si++;
            }
            return damage;
        }
#if false
        public int CompareSlow(LMStats other) {
            int damage = 0;
            //            int[] damArr = new int[ngramToRank.Count];
            foreach (var kv in ngramToRank.OrderBy(kv => kv.Key, StringComparer.Ordinal).ToArray()) {
                int otherRank;
                if (other.ngramToRank.TryGetValue(kv.Key, out otherRank))
                    damage += Math.Abs(kv.Value - otherRank);
                else
                    damage += NumberOfRanks;
            }
            return damage;
        }
#endif
        public int CompareFast(LMStats other) {
            int damage = 0;
            //var damageArr = new int[orderedS.Length];
            int si = 0, oi = 0;
            ulong key = orderedK[si].ngramK;
            ulong okey = other.orderedK[oi].ngramK;

            while (true) {
                while (key > okey) {
                    oi++;
                    if (oi == other.orderedK.Length) {
                        damage += (orderedK.Length - si) * NumberOfRanks;
                        return damage;
                    }
                    okey = other.orderedK[oi].ngramK;
                }

                damage += (key == okey) ?
                    Math.Abs(orderedK[si].rank - other.orderedK[oi].rank) :
                    NumberOfRanks;

                si++;
                if (si == orderedK.Length)
                    return damage;
                key = orderedK[si].ngramK;
            }
        }
        public int TryCompareFast(LMStats other) {
#if USEFAST
            return  CompareFast(other);
#else
            return CompareTo(other);
#endif
        }

    }
}
