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
    public struct RankedNgram{
        public string ngram;
        public int rank;
    }

    public abstract class LMAbstract<T> where T:LMAbstract<T>
    {
        public const int NumberOfRanks = 400;

        public string Name { get; protected set; }
           //Name = Path.GetFileNameWithoutExtension(fromfile.FullName);
        public int HitCounter { get; set; }
        public int HitRun { get; set; }
        protected static RankedNgram[] LoadFromFile(FileInfo fromfile) {
            return fromfile
                .GetLines(Encoding.Default)
                .Select( (line,rank)=>
                    new RankedNgram {
                        rank = rank,
                        ngram = lineparser.Match(line).Groups["ngram"].Value
                    })
                    .ToArray();
        }

        public void AppendToLog(string sentence, string title) {
            string log = string.Format("============{0} {1}\n{2}\n",
                string.Join("", Enumerable.Repeat(".", HitRun).ToArray()),
                title, 
                sentence);

            File.AppendAllText(Name + ".log", log, Encoding.UTF8);
        }

        public abstract int CompareTo(T other);

        const RegexOptions opts = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture;
        protected static readonly Regex
            lineparser = new Regex(@"^(?<ngram>.*?)\t (?<count>\d+)$", opts),
            wordSplitter = new Regex(@"[^0-9\s]+", opts);
        protected static IEnumerable<string> MakeNgramsFromSentence(string sentence) {
            return
                from Match m in wordSplitter.Matches(sentence)
                let rawword = m.Value
                from ngram in MakeNgramsFromWord(rawword)
                select ngram;
        }
        protected static IEnumerable<string> MakeNgramsFromWord(string word) {
            word = Encoding.Default.GetString(Encoding.Default.GetBytes("_" + word + "_"));
            //HACK: this string garbling ensures that behaviour is most similar to text_cat, which makes byte-based ngrams.
            //If the aim were actually high recognition, this wouldn't be a wise choice.
            for (int i = 0; i < word.Length; i++) {
                int upto = Math.Min(word.Length - i, 5);
                for (int j = 1; j <= upto; j++) {
                    yield return word.Substring(i, j);
                }
            }
        }

    }
}
