using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using EamonExtensionsLinq.Filesystem;
using System.Text.RegularExpressions;

namespace WikiParser
{
    public class LMStats
    {
        const int NumberOfRanks=400;
        static Regex lineparser;
        static Regex wordSplitter;
        static LMStats() {
            lineparser = new Regex(@"^(?<ngram>.*?)\t (?<count>\d+)$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);
            wordSplitter = new Regex(@"[^0-9\s]+", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);
        }
        Dictionary<string, int> ngramToRank;
        Dictionary<string, int> ngramToCount;
        public string Name { get; private set; }
        public int HitCounter { get; set; }
        public LMStats(FileInfo fromfile, bool initCounts) {
            int rank = 0;
            if (initCounts)
                ngramToCount = new Dictionary<string, int>();
            ngramToRank = new Dictionary<string, int>();
            foreach (var line in fromfile.GetLines(Encoding.Default)) {
                var match = lineparser.Match(line);
                string ngram = match.Groups["ngram"].Value;
                int count = Int32.Parse(match.Groups["count"].Value);
                if (initCounts)
                    ngramToCount[ngram] = count;
                ngramToRank[ngram] = rank;
                rank++;
            }
            Name = Path.GetFileNameWithoutExtension( fromfile.FullName);
        }

        public LMStats(string fromstring, bool initCounts,string name) {
            Name = name;
            var sortedNgrams =
                MakeNgramsFromSentence(fromstring)
                .GroupBy(ngram => ngram)
                .Select(g => new { Ngram = g.Key, Count = g.Count() })
                .OrderByDescending(p => p.Count)
                .Take(NumberOfRanks)
                .Select((p, i) => new { p.Ngram, p.Count, Rank = i })
                .ToArray();

            ngramToRank = sortedNgrams.ToDictionary(p => p.Ngram, p => p.Rank);
            if (initCounts)
                ngramToCount = sortedNgrams.ToDictionary(p => p.Ngram, p => p.Count);
        }
        static IEnumerable<string> MakeNgramsFromSentence(string sentence) {
            return MakeWords(sentence).SelectMany<string, string>(MakeNgramsFromWord);
        }
        static IEnumerable<string> MakeNgramsFromWord(string word) {
            word = "_" + word + "_";
            for (int i = 0; i < word.Length; i++) {
                int upto = Math.Min(word.Length - i, 4);
                for (int j = 1; j <= upto; j++) {
                    yield return word.Substring(i, j);
                }
            }
        }
        static IEnumerable<string> MakeWords(string sentence) {
            return wordSplitter.Matches(sentence).Cast<Match>().Select(m => m.Value);
        }


        public int CompareTo(LMStats other) {
            int damage=0;
            foreach (var kv in ngramToRank) {
                int otherRank;
                if (other.ngramToRank.TryGetValue(kv.Key, out otherRank))
                    damage += Math.Abs(kv.Value - otherRank);
                else
                    damage += NumberOfRanks;
            }
            return damage;
        }

        public static LMStats[] LoadLangs(bool initCounts) {
            return 
                new DirectoryInfo("text_cat" + Path.DirectorySeparatorChar + "LM")
                .GetFiles("*.lm")
                .Select(fi=>new LMStats(fi,initCounts))
                .ToArray();
        }

    }
}
