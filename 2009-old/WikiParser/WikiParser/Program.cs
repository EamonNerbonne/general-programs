using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml;
using System.IO;

using EamonExtensionsLinq.Filesystem;
using System.Text.RegularExpressions;
using System.Threading;
namespace WikiParser
{
    public static class XmlStreamRead
    {
        public static IEnumerable<XElement> StreamElements(this XmlReader reader, string matchName) {
            while (reader.ReadToFollowing(matchName))
                yield return (XElement)XElement.ReadFrom(reader);
        }
    }

    class Program
    {
        static readonly string[] regexes1 = {
            @"(?>'')'*",
            @"(?><)(!--([^-]|-[^-]|--[^>])*-->|([mM][aA][tT][hH]|[rR][eE][fF]|[sS][mM][aA][lL][lL]).*?(/>|</([mM][aA][tT][hH]|[rR][eE][fF]|[sS][mM][aA][lL][lL])>))",
            @"^((?>#)[rR][eE][dD][iI][rR][eE][cC][tT].*$|(?>\*)\**)",
            @"(?<=&)(?>[aA])[mM][pP];",
            @"&(?>[nN])([bB][sS][pP]|[dD][aA][sS][hH]);",
        };
        static readonly string[] regexesReplace = {
            @"\{(\|([^\|]|\|[^\}])*\||\{([^\}]|\}[^\}])*\})\}",
            @"(?>\[\[([^\[:\|\]]*):)([^\[\]]|\[\[[^\[\]]*\]\])*\]\]",
            @"\[([^ \[\]]+( (?<txt>[^\[\]]*))?|\[((?<txt>[^\[\]:\|]*)|[^\[\]:\|]*\|(?<txt>[^\[\]]*))\])\]",
            @"</?[a-zA-Z]+( [^>]*?)?/?>",
            @"^=+(?<txt>.*?)=+ *$",
        };
        static readonly string[] regexesSpaceTrim = {
            @"^ +",
            @" +$",
            @"(?<=\n\n)(\n *)+",
        };


        const string sentenceRegex =
            @"(?<=[\.\?!]\s+|^)((?<sentence>(\(|" + "\"" + @")?[A-Z]( ([Ss]t|Mrs?|dr|ed|c|v|vs|vol|et al)\.|\(\w\.|[A-Z]\. |\.([\w\d]| (\w\.( \w\.)*|[a-z]))|[^\.\n\?!])+[\.\?!](\)|" + "\"" + @")?))(?=\s|$)";
        //|\(b\. c\.|e\.g\.|i\.e\.

        static string CombineRegex(IEnumerable<string> regexes) {
            return string.Join("|", regexes.Select(r => "(" + r + ")").ToArray());
        }

        static string FilterOutSpaces(string text) {

            StringBuilder s = new StringBuilder(text.Length);


            char c;
            for (int i = 0; i < text.Length; i++) {
                c = text[i];

                if (c == ' ' || c == '\t') {
                    while (true) {
                        if (++i >= text.Length) {
                            break;
                        }
                        c = text[i];


                        if (c == '\n') {
                            while (true) {
                                if (++i >= text.Length) {
                                    s.Append('\n');
                                    break;
                                }
                                c = text[i];

                                if (c == '\n') {
                                    while (true) {
                                        if (++i >= text.Length) {
                                            s.Append('\n');
                                            break;
                                        }
                                        c = text[i];

                                        if (!(c == ' ' || c == '\t' || c == '\n')) {
                                            s.Append('\n');
                                            s.Append('\n');
                                            s.Append(c);
                                            break;
                                        }
                                    }
                                    break;
                                } else if (!(c == ' ' || c == '\t')) {
                                    s.Append('\n');
                                    s.Append(c);
                                    break;
                                }
                            }
                            break;
                        } else if (!(c == ' ' || c == '\t')) {
                            s.Append(' ');
                            s.Append(c);
                            break;
                        }
                    }
                } else if (c == '\n') {
                    while (true) {
                        if (++i >= text.Length) {
                            s.Append('\n');
                            break;
                        }
                        c = text[i];

                        if (c == '\n') {
                            while (true) {
                                if (++i >= text.Length) {
                                    s.Append('\n');
                                    break;
                                }
                                c = text[i];

                                if (!(c == ' ' || c == '\t' || c == '\n')) {
                                    s.Append('\n');
                                    s.Append('\n');
                                    s.Append(c);
                                    break;
                                }
                            }
                            break;
                        } else if (!(c == ' ' || c == '\t')) {
                            s.Append('\n');
                            s.Append(c);
                            break;
                        }
                    }
                } else {
                    s.Append(c);
                }
            }
            return s.ToString();
        }

        const string wikinamespacestring = "http://www.mediawiki.org/xml/export-0.3/";

        const string wikiPath = @"C:\wiki\enwiki-20080724-pages-meta-current.xml";
        static IEnumerable<int> testEnum() {
            yield return 0;
            yield return 1;
            yield return 0;
            yield return 1;
            //   throw new Exception("uh-oh");
        }

        static Regex markupStripper, spaceStripper, markupReplace, sentenceFinder;
        static XNamespace ns = XNamespace.Get(wikinamespacestring);

        readonly static string[] ignore = new[] { "" };
        //new[] { "spanish", "swedish", "italian", "slovak-ascii", "estonian", "scots_gaelic", "portuguese", "tagalog", "czech-iso8859_2", "indonesian", "norwegian", "irish", "basque", "hungarian", "slovenian-ascii", "welsh", "polish", "swahili", "serbian-ascii", "finnish", "lithuanian", "slovenian-iso8859_2", "albanian", "croatian-ascii", "malay", "slovak-windows1250", "latvian", "quechua", "sanskrit", "amharic-utf", "arabic-iso8859_6", "arabic-windows1256", "armenian", "belarus-windows1251", "bosnian", "bulgarian-iso8859_5", "chinese-big5", "chinese-gb2312", "georgian", "greek-iso8859-7", "hebrew-iso8859_8", "hindi", "icelandic", "japanese-euc_jp", "japanese-shift_jis", "korean", "marathi", "mingo", "nepali", "persian", "russian-iso8859_5", "russian-koi8_r", "russian-windows1251", "tamil", "thai", "turkish", "ukrainian-koi8_u", "vietnamese", "yiddish-utf" };

        static void Main(string[] args) {

            var langs = LMStats.LoadLangs(false)
                .Where(l => !ignore.Contains(l.Name))
                .ToArray();

            var englishlang = langs.Where(l => l.Name == "english").First();
            langs = langs.Where(l => l != englishlang).ToArray();


            var options = RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Multiline | RegexOptions.CultureInvariant;
            markupStripper = new Regex(CombineRegex(regexes1), options);
            markupReplace = new Regex(CombineRegex(regexesReplace), options);
            spaceStripper = new Regex(CombineRegex(regexesSpaceTrim), options);
            sentenceFinder = new Regex(sentenceRegex, options);

            Stream stream = File.OpenRead(wikiPath);
            XmlReader reader = XmlReader.Create(stream);
            int pageCount = 0;
            int lastPageCount = 0;
            DateTime start = DateTime.Now, last = DateTime.Now;
            long lastPos = stream.Position;
            int sentencecount = 0;
            foreach (var entry in reader
                .StreamElements("page")
                .AsParallel()
                .Where(page => !(page.Element(ns + "title").Value.StartsWith("Talk:") || page.Element(ns + "title").Value.StartsWith("User:")))
                .Select(page => {
                    pageCount++;
                    lock (langs) //not that a race condition in debug output is a huge problem, but it doesn't cost much perf anyhow.
                        if (DateTime.Now - last > TimeSpan.FromSeconds(1.0)) {
                            DateTime cur = DateTime.Now;
                            long curPos = stream.Position;
                            Console.WriteLine("{5}: Read {0} pages,   {1:g5}/sec,   avg:{2:g5},   {3:g5} MB/s, avg: {4:g5}.", pageCount, (pageCount - lastPageCount) / (cur - last).TotalSeconds, pageCount / (cur - start).TotalSeconds,
                                (curPos - lastPos) / (cur - last).TotalSeconds / 1048576, curPos / (cur - start).TotalSeconds / 1048576, sentencecount);
                            Console.WriteLine(string.Join("  ", langs.OrderByDescending(l => l.HitCounter).Take(5).Select(l => l.Name + "=" + l.HitCounter).ToArray()));
                            last = cur;
                            lastPos = curPos;
                            lastPageCount = pageCount;
                        }
                    return new { Text = Page2Text(page).Replace('\t', ' '), Title = page.Element(ns + "title").Value };
                })
                .Where(text => text.Text != null)
                .Select(text => new { Text = markupStripper.Replace(text.Text, ""), text.Title })
                .Select(text => new { Text = markupReplace.Replace(text.Text, "${txt}"), text.Title })
                .Select(text => new { Text = FilterOutSpaces(text.Text), text.Title })
                .Where(text => text.Text.Length > 2)
                .SelectMany(text => plainText2Sentences(text.Text).Select(s => new { PageTitle = text.Title, Sentence = s }))
                .Where(s => gradeSentence(s.Sentence) > 1.2)
                .Select(titledText => {
                    var stats = new LMStats(titledText.Sentence, false, "unknown");
                    return new {
                        titledText.Sentence,
                        titledText.PageTitle,
                        Stats = stats,
                        Hits = (from language in langs
                                let damage = stats.TryCompareFast(language)
                                orderby damage
                                select new { damage, language }).ToArray()
                    };
                })
                ) {
                var sentence = entry.Sentence;
                var title = entry.PageTitle;
                sentencecount++;
                var hits = entry.Hits;
                var stats = entry.Stats;
                var engDmg = stats.TryCompareFast(englishlang);
                foreach (var hit in hits) {
                    if (hit.damage * 1.05 < engDmg) {
                        hit.language.HitRun++;
                        hit.language.HitCounter++;
#if DEBUG
                        yes = true;
#else
                        hit.language.AppendToLog(sentence, title);
#endif
                    } else {
                        hit.language.HitRun = 0;
                    }
                }
#if DEBUG
                if (yes) {
                    Console.WriteLine("In page {0}",title);
                    Console.WriteLine(sentence);
                    Console.WriteLine(gradeSentence(sentence));
                    foreach (var hit in hits.Take(3)) {
                        Console.Write("{0}: {1}; ", hit.language.Name, hit.damage);
                    }
                    Console.WriteLine();
                    Console.ReadKey();
                }
#endif
//                if (sentencecount >= 50000) break;
            }
            Console.WriteLine("Took: {0} seconds.", (DateTime.Now - start).TotalSeconds);
            DateTime curE = DateTime.Now;
            long curPosE = stream.Position;
            Console.WriteLine("{5}: Read {0} pages,   {1:g5}/sec,   avg:{2:g5},   {3:g5} MB/s, avg: {4:g5}.", pageCount, 1000 / (curE - last).TotalSeconds, pageCount / (curE - start).TotalSeconds,
                (curPosE - lastPos) / (curE - last).TotalSeconds / 1048576, curPosE / (curE - start).TotalSeconds / 1048576, sentencecount);
        }


        static string Page2Text(XElement page) {
            var textEl = page.Element(ns + "revision").Element(ns + "text");
            return textEl == null ? null : textEl.Value;
        }


        static readonly Regex caps = new Regex("[A-Z]", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        static readonly Regex nums = new Regex("[0-9]", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        static readonly Regex wordChars = new Regex(@"\w", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        static readonly Regex words = new Regex(@"(^|\s)\S+", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        static readonly Regex capwords = new Regex(@"(^| )[^ a-zA-Z_0-9]*[A-Z]\S*", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        static double gradeSentence(string sentenceCandidate) {
            int capCount = caps.Matches(sentenceCandidate).Count;
            int numCount = nums.Matches(sentenceCandidate).Count;
            int wordCharCount = wordChars.Matches(sentenceCandidate).Count;
            int wordCount = words.Matches(sentenceCandidate).Count;
            int capWordCount = capwords.Matches(sentenceCandidate).Count;
            int charCount = sentenceCandidate.Length;
            return (wordCharCount - numCount - capCount) / (double)charCount + (Math.Min(wordCount, 6) * 0.15) - 1.5 * ((capWordCount - 1) / (double)wordCount);
        }

        static IEnumerable<string> plainText2Sentences(string text) {
            return sentenceFinder.Matches(text)
                    .Cast<Match>()
                    .Where(m => m.Success)
                    .Select(m => m.Groups["sentence"].Value);
        }
    }
}
