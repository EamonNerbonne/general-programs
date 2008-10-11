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
using System.Reflection;
namespace WikiParser
{

    class Program
    {
        static XNamespace ns = XNamespace.Get("http://www.mediawiki.org/xml/export-0.3/");
        static string Page2Text(XElement page) {
            var textEl = page.Element(ns + "revision").Element(ns + "text");
            return textEl == null ? null : textEl.Value;
        }

        const RegexOptions options = RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant;
        static Regex nonArticlesTitle = new Regex(@"^(Talk|User|User talk|Wikipedia|Wikipedia talk):", options);


        static void Main(string[] args) {
            CommandLineArgs parsedArgs = new CommandLineArgs(args);
            if (!parsedArgs.AreOK) {
                Console.Error.WriteLine("Cannot continue.\n\n");
                parsedArgs.PrintUsage();
                Environment.Exit(1);
                return;//shouldn't be necessary...
            }
            parsedArgs.PrintValues();


            Console.Write("Loading Dictionary...");
            EnglishSentenceGrader.LoadDictionary( parsedArgs.LMFileSearchPath.GetFiles("english.ngl").FirstOrDefault());
            Console.WriteLine("done.");


            DoWikiFiltering<LMFastOrderingImpl>( parsedArgs);

        }

        static void DoWikiFiltering<LMStats>( CommandLineArgs parsedArgs) {
            Stream stream = parsedArgs.WikiFilePath.OpenRead();
            XmlReader reader = XmlReader.Create(stream);

            Console.Write("Loading Languages...");
            var langs = (
                from lmFile in parsedArgs.LMFiles
                let language = new LMReferenceImpl(lmFile)
                where !parsedArgs.IgnoreLangs.Contains(language.Name)
                select language
                ).ToArray();


            var englishlang = langs.Where(l => l.Name == "english").FirstOrDefault();
            if (englishlang == null) {
                Console.Error.WriteLine("Could not find english.lm.  Did you ignore english?");
                Environment.Exit(1);
                return;
            }
            langs = langs.Where(l => l != englishlang).ToArray();
            Console.WriteLine("done.");

            int lastPageCount = 0;
            int sentencecount = 0;
            int pageCount = 0;
            DateTime startTime = DateTime.Now, lastTime = DateTime.Now;
            long lastPos = stream.Position;

            //we'll query lazily over the xml stream.  We annotate the page with it's page # using functional syntax
            //since this isn't exposed in the query synax used next.
            //we use the .AsParallel method to be able to execute the rest of the query in parallel in a manycore machine.
            //of course, the streaming itself can't execute in parallel...
            var xmlPagesInParallel = reader
                .StreamElements(el => el.LocalName == "page")
                .Select((pageXml, i) => new { Page = pageXml, Index = i })
                .AsParallel();

            var parallelQuery = //won't be executed until enumerated over.
                                from page in xmlPagesInParallel
                                let pageXml = page.Page
                                let i = page.Index
                                let title = pageXml.Element(ns + "title").Value
                                where parsedArgs.IncludeNonArticles || !nonArticlesTitle.Match(title).Success
                                let rawtext = Page2Text(pageXml)
                                where rawtext != null && rawtext.Length > 2
                                let strippedtext = WikiMarkupStripper.StripMarkup(rawtext)
                                let text = WhitespaceNormalizer.Normalize(strippedtext)
                                where text.Length > 2
                                from sentence in EnglishSentenceFinder.FindEnglishSentences(text)
                                let grade = EnglishSentenceGrader.GradeEnglishSentence(sentence)
                                where grade > 2.5  //this is just a simple filter to prefer longer, more reasonable sentences.
                                let stats = new LMReferenceImpl(sentence, "unknown")
                                let hits = (from language in langs
                                            let damage = stats.CompareTo(language)
                                            orderby damage
                                            select new { damage, language }
                                            ).ToArray()
                                where hits[0].language != englishlang
                                select new {
                                    Sentence = sentence,
                                    PageTitle = title,
                                    PageIndex = i,
                                    Stats = stats,
                                    Grade = grade,
                                    EnglishDamage = stats.CompareTo(englishlang),
                                    BestDamage = hits[0].damage,
                                    Hits = hits
                                };

            foreach (var entry in parallelQuery) {
                foreach (var hit in entry.Hits) {
                    if (hit.damage * 1.05 < entry.EnglishDamage && hit.damage < 1.05 * entry.BestDamage) {
                        hit.language.HitRun++;
                        hit.language.HitCounter++;
                        hit.language.AppendToLog(entry.Sentence, entry.PageTitle);
                    } else {
                        hit.language.HitRun = 0;
                    }
                }
                sentencecount++;
                pageCount = entry.PageIndex;
                if (DateTime.Now - lastTime > TimeSpan.FromSeconds(1.0)) {
                    DateTime cur = DateTime.Now;
                    long curPos = stream.Position;//stream may be reading ahead due to parallelization, but we can't do much about that...
                    var lastElapsed = (cur - lastTime).TotalSeconds;
                    var totalElapsed = (cur - startTime).TotalSeconds;
                    Console.WriteLine("{0}: Read {1} pages at on avg {2:g5}/sec with {3:g5} MB/s (avg: {4:g5} MB/s)",
                        sentencecount,
                        pageCount, //out of order, so not strictly true...
                        pageCount / totalElapsed,
                        (curPos - lastPos) / lastElapsed / 1048576,
                        curPos / totalElapsed / 1048576);
                    lastTime = cur;
                    lastPos = curPos;
                    lastPageCount = pageCount;
                }
            }
            pageCount++;
            Console.WriteLine("Took: {0} seconds.", (DateTime.Now - startTime).TotalSeconds);
            DateTime curE = DateTime.Now;
            long curPosE = stream.Position;

            Console.WriteLine("{5}: Read {0} pages,   {1:g5}/sec,   avg:{2:g5},   {3:g5} MB/s, avg: {4:g5}.", pageCount, 1000 / (curE - lastTime).TotalSeconds, pageCount / (curE - startTime).TotalSeconds,
                (curPosE - lastPos) / (curE - lastTime).TotalSeconds / 1048576, curPosE / (curE - startTime).TotalSeconds / 1048576, sentencecount);

        }
    }
}
