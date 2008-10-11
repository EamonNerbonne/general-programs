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
    public class LMFastOrderingImpl : LMAbstract<LMFastOrderingImpl>
    {
        Ranked5gram[] orderedK;
        public LMFastOrderingImpl(FileInfo fromfile) {
            orderedK = 
                LoadFromFile(fromfile)
                .Select(rn=> new Ranked5gram(rn))
                .OrderBy(n => n.ngramK)
                .ToArray();
            //using the ordinal string comparer is both faster and more consistent with the other implementation.
            Name = Path.GetFileNameWithoutExtension(fromfile.FullName);
        }


        public LMFastOrderingImpl(string fromstring, string name) {
            Name = name;
            var ngramsK = MakeBytegramsFromSentence(fromstring).ToArray();
            Array.Sort(ngramsK);//direct sort is faster than: ngramsK = ngramsK.OrderBy(n => n).ToArray();
            List<Ranked5gram> ngramsG = new List<Ranked5gram>();
            ulong lastkey = ngramsK[0];
            int lastkeycount=0;
            foreach (ulong ngramK in ngramsK) {
                if (ngramK == lastkey) {
                    lastkeycount++;
                } else {
                    ngramsG.Add(new Ranked5gram {
                        ngramK = lastkey,
                        rank = lastkeycount
                    });
                    lastkey = ngramK;
                    lastkeycount = 1;
                }
            }
            ngramsG.Add(new Ranked5gram {
                ngramK = lastkey,
                rank = lastkeycount
            });
            ngramsG.Sort((a, b) => b.rank - a.rank);//direct sort seems faster.
            orderedK = 
                ngramsG
//                .OrderByDescending(p=>p.rank) //implied by previous sort statement.
                .Take(NumberOfRanks)
                .Select((p,i)=>new Ranked5gram {ngramK= p.ngramK,rank=i})
                .OrderBy(p=>p.ngramK)
                . ToArray();
        }
        static IEnumerable<ulong> MakeBytegramsFromSentence(string sentence) {
            //HACK: using the default ansi encoding ensures that behaviour is most similar to text_cat, which makes byte-based ngrams.
            //If the aim were actually high recognition, this wouldn't be a wise choice; you'd want a larger character set.
            return 
                from Match m in wordSplitter.Matches(sentence)
                let rawword = m.Value
                let word = "_" + rawword + "_"
                let bytes = Encoding.Default.GetBytes(word)
                from ngram in Ranked5gram.CreateFromByteString(bytes)
                select ngram;

            //TODO: explicitly use the right encoding rather than the default.
        }


        public int CompareFast(LMFastOrderingImpl other) {
            int damage = 0;
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
        public override int CompareTo(LMFastOrderingImpl other) {
            return CompareFast(other);
        }

    }
}
