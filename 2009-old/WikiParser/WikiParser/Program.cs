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
                yield return  (XElement)XElement.ReadFrom(reader);
        }
    }

    class Program
    {
        static readonly string[] regexes = {
            @"''+",
            @"&[nN][bB][sS][pP];",
            @"(?<txt>&)amp;",
            @"\[([^ \[\]]+( (?<txt>[^\[\]]*))?|\[((?<txt>[^\[:\|\]]*)|[^\[:\|\]]*(\|(?<txt>[^\[\]]*)|:([^\[\]]|\[\[([^\[\]]*)\]\])*))\])\]",
            @"\{(\|([^\|]|\|[^\}])*\||\{([^\}]|\}[^\}])*\})\}",
            @"<!--([^-]|-[^-]|--[^>])*-->"            ,
            @"<([rR][eE][fF]|[sS][mM][aA][lL][lL]).*?(/>|</([rR][eE][fF]|[sS][mM][aA][lL][lL])>)",
            @"<[a-zA-Z]+( [^>]*?)?(/>|>(?<txt>[^<]*)</[a-zA-Z]+>)",
            @"^[\* \t]+",
            @"^#[rR][eE][dD][iI][rR][eE][cC][tT].*$",
            @"^=+(?<txt>.*?)=+ *$",
            @"[ \t]+$",
            @"(?<txt>\n\n)\n+",

            
                                 };
        //static readonly string nestableRegex = @"\[\[[^\[:\|\]]*:(XXX)*\]\]";

        const string sentenceRegex = @"(?<=([\.\?!]\s+)|^)(\(|" + "\"" + @")?[A-Z](e\.g\.|dr. |[A-Z]\. [A-Z]|\d\.\d|\. *[a-z]|\.\w|[^\.\n\?!])+[\.\?!](\)|" + "\"" + @")?((?=\s)|$)";


        static string CombineRegex(IEnumerable<string> regexes) {
            return string.Join("|", regexes.Select(r => "(" + r + ")").ToArray());
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

        static Regex markupStripper, sentenceFinder;
        static XNamespace ns = XNamespace.Get(wikinamespacestring);

        static void Main(string[] args) {
            string fullRegex = CombineRegex(regexes);//.Concat(new[] { compRegex }));
            markupStripper = new Regex(fullRegex, RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Multiline | RegexOptions.CultureInvariant);
            sentenceFinder = new Regex(sentenceRegex, RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Multiline | RegexOptions.CultureInvariant);
            
            Stream stream = File.OpenRead(wikiPath);
            XmlReader reader = XmlReader.Create(stream);
            int pageCount = 0;
            DateTime start = DateTime.Now,last=DateTime.Now;
            long lastPos = stream.Position;
            int sentencecount = 0;
            foreach (var sentence in reader
                .StreamElements("page")
                .InAnotherThread(100)
                .Select(page=> {
                    pageCount++;
                    if (DateTime.Now - last > TimeSpan.FromSeconds(1.0)) {
                        DateTime cur = DateTime.Now;
                        long curPos = stream.Position;
                        Console.WriteLine("{5}: Read {0} pages,   {1:g5}/sec,   avg:{2:g5},   {3:g5} MB/s, avg: {4:g5}.", pageCount, 1000 / (cur - last).TotalSeconds, pageCount / (cur - start).TotalSeconds,
                            (curPos - lastPos) / (cur - last).TotalSeconds / 1048576, curPos / (cur - start).TotalSeconds / 1048576, sentencecount);
                        last = cur;
                        lastPos = curPos;
                    }
                    return Page2Text(page);
                })
                .Where(text=>text!=null)
                .InAnotherThread(100)
                .Select(text => StripWikiMarkup(text))
                .InAnotherThread(100)
                .Select(text => StripWikiMarkup(text))
                .InAnotherThread(100)
                .Where(text=>text.Length!=0)
                .SelectMany(text=>plainText2Sentences(text))
  //              .InAnotherThread(10000) /**/
                ) {
                    sentencecount++;
                if (sentencecount > 100000) break;
//                Console.WriteLine(sentence);
  //              Console.ReadKey();
            }
            Console.WriteLine("Took: {0} seconds.", (DateTime.Now - start).TotalSeconds);
//            XmlReader reader = XmlReader.Create(wikiPath);
            Console.ReadKey();
        }


        static string Page2Text(XElement page) {
            var textEl = page.Element(ns + "revision").Element(ns + "text");
            return textEl == null ? null : textEl.Value;
        }

        static string StripWikiMarkup(string markedUpText) {
            return markupStripper.Replace(markedUpText, "${txt}");
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
            return (wordCharCount- numCount*0.75-capCount)/(double)charCount +(Math.Min(wordCount,6)*0.1)-((capWordCount-1) / (double)wordCount);
        }

        static IEnumerable<string> plainText2Sentences(string text) {
            return sentenceFinder.Matches(text)
                    .Cast<Match>()
                    .Where(m => m.Success)
                    .Select(m => m.Value);
        }

        static IEnumerable<string> page2Sentences(XElement page) {
            string text = Page2Text(page);
            if (text == null) return Enumerable.Empty<string>();
            string plainText = StripWikiMarkup(text);
            if (plainText.Length == 0) return Enumerable.Empty<string>();
            return plainText2Sentences(plainText);
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
