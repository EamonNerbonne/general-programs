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
    public class LMReferenceImpl : LMAbstract<LMReferenceImpl>
    {
        Dictionary<string, int> ngramToRank;
        public LMReferenceImpl(FileInfo fromfile) {
            ngramToRank= LoadFromFile(fromfile).ToDictionary(rn => rn.ngram, rn => rn.rank);
            Name = Path.GetFileNameWithoutExtension(fromfile.FullName);
        }

        public LMReferenceImpl(string fromstring, string name) {
            Name = name;
            ngramToRank =
                MakeNgramsFromSentence(fromstring)
                .GroupBy(ngram => ngram)
                .Select(g => new { Ngram = g.Key, Count = g.Count() })
                .OrderByDescending(p => p.Count)
                .ThenBy(p=>p.Ngram,StringComparer.Ordinal)
                .Take(NumberOfRanks)
                .Select((p, i) => new { p.Ngram, p.Count, Rank = i })
                .ToDictionary(p => p.Ngram, p => p.Rank);
        }

        public override int CompareTo(LMReferenceImpl other) {
            return CompareToNaive(other);
        }
        
        public int CompareToNaive(LMReferenceImpl other) {
            int damage = 0;
            foreach (var kv in ngramToRank.OrderBy(kv => kv.Key, StringComparer.Ordinal).ToArray()) {
                int otherRank;
                if (other.ngramToRank.TryGetValue(kv.Key, out otherRank))
                    damage += Math.Abs(kv.Value - otherRank);
                else
                    damage += NumberOfRanks;
            }
            return damage;
        }
    }
}
