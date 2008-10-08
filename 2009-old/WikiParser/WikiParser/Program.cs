//#define DISABLETHREADING
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
            @"(?><)(!--([^-]|-[^-]|--[^>])*-->|([rR][eE][fF]|[sS][mM][aA][lL][lL]).*?(/>|</([rR][eE][fF]|[sS][mM][aA][lL][lL])>))",
            @"^((?>#)[rR][eE][dD][iI][rR][eE][cC][tT].*$|(?>\*)\**)",
            @"(?<=&)(?>[aA])[mM][pP];",
            @"&(?>[nN])[bB][sS][pP];",
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
            @"(?<=[\.\?!]\s+|^)(?>(?<sentence>(\(|" + "\"" + @")?[A-Z](dr\. |\(b\. c\.|e\.g\.|\(b\.|\(d\.|[Ss]t\.|et al\.| vs\.|Mrs\.|Mr\.|ed\.| c\.| v\.|[A-Z]\. |\.( *[a-z]|[\w\d])|[^\.\n\?!])+[\.\?!](\)|" + "\"" + @")?))(?=\s|$)";
         

        static string CombineRegex(IEnumerable<string> regexes) {
            return string.Join("|", regexes.Select(r => "(" + r + ")").ToArray());
        }

        static string FilterOutSpaces(string text) {

            StringBuilder s = new StringBuilder(text.Length);
            

            char c;
            for(int i=0;i<text.Length;i++) {
                c = text[i];
                
                if (c == ' ' || c == '\t') {
                    while (true) {
                        if (++i >= text.Length) {
                            break;
                        }
                        c = text[i];

                        
                        if (c == '\n') {
                            while(true)  {
                                if (++i >= text.Length) {
                                    s.Append('\n');
                                    break;
                                }
                                c=text[i];

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

        static Regex markupStripper,spaceStripper,markupReplace, sentenceFinder;
        static XNamespace ns = XNamespace.Get(wikinamespacestring);

        readonly static string[] ignore = new[] { "spanish", "swedish", "italian", "slovak-ascii", "estonian", "scots_gaelic", "portuguese", "tagalog", "czech-iso8859_2", "indonesian", "norwegian", "irish", "basque", "hungarian", "slovenian-ascii", "welsh", "polish", "swahili", "serbian-ascii", "finnish", "lithuanian", "slovenian-iso8859_2", "albanian", "croatian-ascii", "malay", "slovak-windows1250", "latvian", "quechua", "sanskrit", "amharic-utf", "arabic-iso8859_6", "arabic-windows1256", "armenian", "belarus-windows1251", "bosnian", "bulgarian-iso8859_5", "chinese-big5", "chinese-gb2312", "georgian", "greek-iso8859-7", "hebrew-iso8859_8", "hindi", "icelandic", "japanese-euc_jp", "japanese-shift_jis", "korean", "marathi", "mingo", "nepali", "persian", "russian-iso8859_5", "russian-koi8_r", "russian-windows1251", "tamil", "thai", "turkish", "ukrainian-koi8_u", "vietnamese", "yiddish-utf" };

        static void Main(string[] args) {

            var langs = LMStats.LoadLangs(false)
                .Where(l=>! ignore.Contains(l.Name))
                .ToArray();



            var options = RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Multiline | RegexOptions.CultureInvariant;
            markupStripper = new Regex(CombineRegex(regexes1),options);
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
            foreach (var sentence in reader
                .StreamElements("page")
//                .InAnotherThread(100)
                .Where(page => !(page.Element(ns + "title").Value.StartsWith("Talk:") || page.Element(ns + "title").Value.StartsWith("User:")) )
                .Select(page => {
                    pageCount++;
                    if (DateTime.Now - last > TimeSpan.FromSeconds(1.0)) {
                        DateTime cur = DateTime.Now;
                        long curPos = stream.Position;
                        Console.WriteLine("{5}: Read {0} pages,   {1:g5}/sec,   avg:{2:g5},   {3:g5} MB/s, avg: {4:g5}.", pageCount, (pageCount-lastPageCount) / (cur - last).TotalSeconds, pageCount / (cur - start).TotalSeconds,
                            (curPos - lastPos) / (cur - last).TotalSeconds / 1048576, curPos / (cur - start).TotalSeconds / 1048576, sentencecount);
                        Console.WriteLine(string.Join("  ", langs.OrderByDescending(l => l.HitCounter).Take(5).Select(l => l.Name + "=" + l.HitCounter).ToArray()));
                        last = cur;
                        lastPos = curPos;
                        lastPageCount = pageCount;
                    }
                    return Page2Text(page).Replace('\t',' ');
                })
                .Where(text => text != null)
//                .InAnotherThread(100)
                .Select(text => markupStripper.Replace(text, ""))
  //              .InAnotherThread(100)
                .Select(text => markupReplace.Replace(text, "${txt}"))
    //            .InAnotherThread(100)
                .Select(text => FilterOutSpaces(text))
                .Where(text => text.Length > 2)
                .InAnotherThread(100)
                .SelectMany(text => plainText2Sentences(text))
                              .Where(s=>gradeSentence(s)>1.2)/**/
                              .InAnotherThread(10000)
                ) {
                sentencecount++;
                LMStats stats = new LMStats(sentence, false, "unknown");
                var hits = (from language in langs
                            let damage = stats.TryCompareFast(language)
                            orderby damage
                            select new { damage, language }).ToArray();
                bool betterThanE = true;
                foreach(var hit in hits) {
                    if (hit.language.Name == "english") {
                        betterThanE = false;
                        continue;
                    }
                    if (betterThanE) {
                        hit.language.HitRun++;
                        hit.language.HitCounter++;
                        hit.language.AppendToLog(sentence);
                    } else {
                        hit.language.HitRun = 0;
                    }
                }

                /*
                                 int val = 0;
                 foreach (var hit in hits.Take(1))
                                    hit.language.HitCounter += (++val);

                
                                if (hits.First().language.Name != "english") {
                                    Console.WriteLine(sentence);
                                    Console.WriteLine(gradeSentence(sentence));
                                    foreach (var hit in hits.Take(3)) {
                                        Console.Write("{0}: {1}; ", hit.language.Name, hit.damage);
                                    }
                                    Console.WriteLine();
                                    Console.ReadKey();
                                }/**/

            //    if (sentencecount >= 20000) break;
            }
            Console.WriteLine("Took: {0} seconds.", (DateTime.Now - start).TotalSeconds);
            DateTime curE = DateTime.Now;
            long curPosE = stream.Position;
            Console.WriteLine("{5}: Read {0} pages,   {1:g5}/sec,   avg:{2:g5},   {3:g5} MB/s, avg: {4:g5}.", pageCount, 1000 / (curE - last).TotalSeconds, pageCount / (curE - start).TotalSeconds,
                (curPosE - lastPos) / (curE - last).TotalSeconds / 1048576, curPosE / (curE - start).TotalSeconds / 1048576, sentencecount);
            //            XmlReader reader = XmlReader.Create(wikiPath);
            //Console.ReadKey();
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
            return (wordCharCount - numCount  - capCount) / (double)charCount + (Math.Min(wordCount, 6) * 0.15) - 1.5*((capWordCount - 1) / (double)wordCount) ;
        }

        static IEnumerable<string> plainText2Sentences(string text) {
            return sentenceFinder.Matches(text)
                    .Cast<Match>()
                    .Where(m => m.Success)
                    .Select(m => m.Groups["sentence"].Value);
        }

    }
    static class BackgroundProcessor
    {
        class ProcHelp<T> : IDisposable
        {
            Semaphore readS;
            Semaphore writeS;
            bool cancelled = false;
            IEnumerable<T> orig;
            T[] buffer;
            int readPos = 0, writePos = 0, queueDepth;
            Thread bgThread;
            public ProcHelp(IEnumerable<T> orig, int queueDepth) {
                if (queueDepth < 1) throw new ArgumentException("You cannot make a background buffer of size 0;");
                readS = new Semaphore(0, queueDepth + 1);//+1 is for cancelling.
                writeS = new Semaphore(queueDepth, queueDepth + 1);
                this.orig = orig;
                this.queueDepth = queueDepth;
                buffer = new T[queueDepth];
            }
            public void BackgroundRun(object ignored) {
                BackgroundRun();
            }
            public void BackgroundRun() {
                bgThread = Thread.CurrentThread;
                var enumerator = orig.GetEnumerator();
                while (true) {
                    writeS.WaitOne();
                    if (cancelled) break;
                    try {
                        if (enumerator.MoveNext()) {
                            lock (buffer) buffer[writePos] = enumerator.Current;
                            writePos = (writePos + 1) % queueDepth; //writePos is bgThread local, no need to lock.
                        } else {
                            cancelled = true;
                            break;
                        }
                    } catch {
                        cancelled = true;
                        throw;
                    } finally {
                        readS.Release(1);// we don't want the main thread to block - on error, empty or continue.
                    }
                }
            }

            public bool Generate(out T item) {
                readS.WaitOne();
                if (cancelled) {
                    item = default(T);
                    return false; //no need to increment writeS since the writer has already exited anyhow...
                } else {
                    lock (buffer) item = buffer[readPos];
                    readPos = (readPos + 1) % queueDepth;//readPos is Generate-thread local, no need to lock.
                    writeS.Release(1);
                    return true;
                }
            }
            public void Cancel() {
                cancelled = true;
                //if(bgThread.IsAlive) bgThread.Abort(); //some people claim Thread.Abort is nasty.  Commenting this line out will potentially allow some unnecessary computation, but not break anything.
                writeS.Release(1);
            }
            public void Dispose() {
                Cancel();
            }
        }
        public static IEnumerable<T> InAnotherThread<T>(this IEnumerable<T> orig) {
            return InAnotherThread(orig, 10);
        }

        public static IEnumerable<T> InAnotherThread<T>(this IEnumerable<T> orig, int queueDepth) {
#if DISABLETHREADING
            return orig;
#else
            using (ProcHelp<T> proc = new ProcHelp<T>(orig, queueDepth)) {
                ThreadPool.QueueUserWorkItem(proc.BackgroundRun);
                while (true) {
                    int worker, cp, workerMax, cpMax;
                    ThreadPool.GetAvailableThreads(out worker, out cp);
                    if (worker > 0 && cp > 0) break;
                    ThreadPool.GetMaxThreads(out workerMax, out cpMax);
                    if (worker == 0) workerMax += 4;
                    if (cp == 0) cpMax += 4;
                    ThreadPool.SetMaxThreads(workerMax, cpMax);
                }
                //Thread t = new Thread(proc.BackgroundRun);
                //  t.IsBackground = true;
                // t.Start();
                while (true) {
                    T item;
                    bool hasNext = proc.Generate(out item);
                    if (hasNext)
                        yield return item;
                    else
                        yield break;
                }

            }
#endif
        }

    }
}
