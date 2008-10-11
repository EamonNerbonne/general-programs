using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WikiParser
{
    /// <summary>
    /// This implementation is for reference only.  It takes 161s (64-bit) or 135s (32-bit) 
    /// to process the first 30000 sentences on an intel q9300.
    /// </summary>
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
