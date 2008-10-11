using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace WikiParser
{
    /// <summary>
    /// This implementation is the fastest.  It takes 8.07s (64-bit) or 9.93s (32-bit) 
    /// to process the first 30000 sentences on an intel q9300.
    /// </summary>
    public class LMFastOrderingImpl : LMAbstract<LMFastOrderingImpl>
    {
        RankedBytegram[] orderedK;
        public LMFastOrderingImpl(FileInfo fromfile) {
            orderedK = 
                LoadFromFile(fromfile)
                .Select(rn=> new RankedBytegram(rn))
                .OrderBy(n => n.ngramK)
                .ToArray();
            //using the ordinal string comparer is both faster and more consistent with the other implementation.
            Name = Path.GetFileNameWithoutExtension(fromfile.FullName);
        }


        public LMFastOrderingImpl(string fromstring, string name) {
            Name = name;
            var ngramsK = MakeBytegramsFromSentence(fromstring);
            Array.Sort(ngramsK);//direct sort is faster than: ngramsK = ngramsK.OrderBy(n => n).ToArray();
            List<RankedBytegram> ngramsG = new List<RankedBytegram>();
            ulong lastkey = ngramsK[0];
            int lastkeycount=0;
            foreach (ulong ngramK in ngramsK) {
                if (ngramK == lastkey) {
                    lastkeycount++;
                } else {
                    ngramsG.Add(new RankedBytegram {
                        ngramK = lastkey,
                        rank = lastkeycount
                    });
                    lastkey = ngramK;
                    lastkeycount = 1;
                }
            }
            ngramsG.Add(new RankedBytegram {
                ngramK = lastkey,
                rank = lastkeycount
            });
            ngramsG.Sort((a, b) => b.rank - a.rank);//direct sort seems faster.
            orderedK = new RankedBytegram[Math.Min(400, ngramsG.Count)];
            for (int i = 0; i < orderedK.Length; i++) {
                orderedK[i].rank = i;
                orderedK[i].ngramK = ngramsG[i].ngramK;
            }
            Array.Sort(orderedK,(a, b) => a.ngramK.CompareTo(b.ngramK));//direct sort seems faster.
        }
        static ulong[] MakeBytegramsFromSentence(string sentence) {
            //HACK: using the default ansi encoding ensures that behaviour is most similar to text_cat, which makes byte-based ngrams.
            //If the aim were actually high recognition, this wouldn't be a wise choice; you'd want a larger character set.

            var matches = wordSplitter.Matches(sentence).Cast<Match>().ToArray();
            int spaceNeeded = matches.Select(m => RankedBytegram.CalcNgramCountFromLength(m.Length + 2)).Sum();
            ulong[] retval = new ulong[spaceNeeded];
            int writePtr=0;
            foreach(Match m in matches) {
                byte[] bytes = Encoding.Default.GetBytes("_"+m.Value+"_");
                RankedBytegram.CreateFromByteString(bytes,retval,ref writePtr);
            }
            return retval;
            //TODO: explicitly use the right encoding rather than the default.
        }


        public int CompareFast(LMFastOrderingImpl other) {
            int damage = 0;
            int si = 0, oi = 0;
            ulong key = orderedK[si].ngramK;
            var otherOrderedK = other.orderedK;
            ulong okey = otherOrderedK[oi].ngramK;
            while (true) {
                while (key > okey) {
                    oi++;
                    if (oi == otherOrderedK.Length) {
                        damage += (orderedK.Length - si) * NumberOfRanks;
                        return damage;
                    }
                    okey = otherOrderedK[oi].ngramK;
                }

                damage += (key != okey) ?
                     NumberOfRanks :
                    Math.Abs(orderedK[si].rank - otherOrderedK[oi].rank);

                si++;
                if (si == orderedK.Length)
                    return damage;
                key = orderedK[si].ngramK;
            }
        }
        public override int CompareTo(LMFastOrderingImpl other) {
            return CompareFast(other);
        }

    }
}
