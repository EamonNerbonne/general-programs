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

        static void Main(string[] args) {
            CommandLineArgs parsedArgs = new CommandLineArgs(args);
            if (!parsedArgs.AreOK) {
                Console.Error.WriteLine("Cannot continue.\n\n");
                parsedArgs.PrintUsage();
                Environment.Exit(1);
                return;//shouldn't be necessary...
            }
            parsedArgs.PrintValues();

            Stream stream = parsedArgs.WikiFilePath.OpenRead();
            XmlReader reader = XmlReader.Create(stream);

            var langs = (
                from lmFile in parsedArgs.LMFiles
                let language = new LMStats(lmFile, false)
                where !parsedArgs.IgnoreLangs.Contains (language.Name )
                select language
                ).ToArray();

            
            var englishlang = langs.Where(l => l.Name == "english").FirstOrDefault();
            if (englishlang == null) {
                Console.Error.WriteLine("Could not find english.lm.  Did you ignore english?");
                Environment.Exit(1);
                return;
            }

            langs = langs.Where(l => l != englishlang).ToArray();

            /*          
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
          */
            int pageCount = 0;
            int lastPageCount = 0;
            int sentencecount = 0;
            DateTime start = DateTime.Now, last = DateTime.Now;
            long lastPos = stream.Position;

            var parallelQuery = //won't be executed until enumerated over.
                                from pageXml in reader.StreamElements(el => el.LocalName == "page").AsParallel()
                                where
                                    !(pageXml.Element(ns + "title").Value.StartsWith("Talk:")
                                    || pageXml.Element(ns + "title").Value.StartsWith("User:")
                                    || pageXml.Element(ns + "title").Value.StartsWith("User talk:"))
                                let rawtext = Page2Text(pageXml)
                                let title = pageXml.Element(ns + "title").Value
                                where rawtext != null && rawtext.Length > 2
                                let strippedtext = WikiMarkupStripper.StripMarkup(rawtext)
                                let text = WhitespaceNormalizer.Normalize(strippedtext)
                                where text.Length > 2
                                from sentence in EnglishSentenceFinder.FindEnglishSentences(text)
                                let grade = EnglishSentenceGrader.GradeEnglishSentence(sentence)
                                where grade > 1.2
                                let stats = new LMStats(sentence, false, "unknown")
                                let hits = (from language in langs
                                            let damage = stats.TryCompareFast(language)
                                            orderby damage
                                            select new { damage, language }
                                            ).ToArray()
                                where hits[0].language!=englishlang
                                select new {
                                    Sentence = sentence,
                                    PageTitle = title,
                                    Stats = stats,
                                    Grade = grade,
                                    EnglishDamage = stats.TryCompareFast(englishlang),
                                    BestDamage = hits[0].damage,
                                    Hits = hits
                                };
            
            foreach (var entry in parallelQuery) {
                sentencecount++;
                foreach (var hit in entry.Hits) {
                    if (hit.damage * 1.05 < entry.EnglishDamage && hit.damage < 1.05*entry.BestDamage) {
                        hit.language.HitRun++;
                        hit.language.HitCounter++;
                        hit.language.AppendToLog(entry.Sentence, entry.PageTitle);
                    } else {
                        hit.language.HitRun = 0;
                    }
                }
            }
            Console.WriteLine("Took: {0} seconds.", (DateTime.Now - start).TotalSeconds);
            DateTime curE = DateTime.Now;
            long curPosE = stream.Position;
            Console.WriteLine("{5}: Read {0} pages,   {1:g5}/sec,   avg:{2:g5},   {3:g5} MB/s, avg: {4:g5}.", pageCount, 1000 / (curE - last).TotalSeconds, pageCount / (curE - start).TotalSeconds,
                (curPosE - lastPos) / (curE - last).TotalSeconds / 1048576, curPosE / (curE - start).TotalSeconds / 1048576, sentencecount);
        }




    }
}
