﻿using System;
using System.IO;
using System.Linq;

namespace WikiParser
{
    /// <summary>
    /// This implementation is for reference only.  It takes 26.8s (64-bit) or 21.9s (32-bit) 
    /// to process the first 30000 sentences on an intel q9300.
    /// </summary>
    public class LMOrderingImpl:LMAbstract<LMOrderingImpl>
    {
        RankedNgram[] orderedNgrams;
        public LMOrderingImpl(FileInfo fromfile) {
            orderedNgrams = LoadFromFile(fromfile).OrderBy(rn => rn.ngram, StringComparer.Ordinal).ToArray();
            //using the ordinal string comparer is both faster and more consistent with the other implementation.
            Name = Path.GetFileNameWithoutExtension(fromfile.FullName);
        }


        public LMOrderingImpl(string fromstring, string name) {
            Name = name;
            orderedNgrams =
                MakeNgramsFromSentence(fromstring)
                .GroupBy(ngram => ngram)
                .Select(g => new { Ngram = g.Key, Count = g.Count() })
                .OrderByDescending(p => p.Count)
                .ThenBy(p => p.Ngram, StringComparer.Ordinal)
                .Take(NumberOfRanks)
                .Select((p, i) => new RankedNgram { ngram = p.Ngram, rank = i })
                .OrderBy(rn => rn.ngram, StringComparer.Ordinal)
                .ToArray();
        }

        public int CompareToOrdered(LMOrderingImpl other) {
            int damage = 0;
            var orderedS = orderedNgrams;
            var orderedO = other.orderedNgrams;
            int si = 0, oi = 0;
            while (si < orderedS.Length) {
                int lastCompare = 0;
                string s = orderedS[si].ngram;
                while (oi < orderedO.Length && (lastCompare = string.CompareOrdinal(s, orderedO[oi].ngram)) > 0) oi++;
                if (oi == orderedO.Length) {
                    damage += (orderedS.Length - si) * NumberOfRanks;
                    break;
                }
                damage += (lastCompare == 0) ?
                    Math.Abs(orderedO[oi].rank - orderedS[si].rank) :
                    NumberOfRanks;
                si++;
            }
            return damage;
        }

        public override int CompareTo(LMOrderingImpl other) {
            return CompareToOrdered(other);
        }
    }
}
