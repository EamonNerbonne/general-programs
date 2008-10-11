using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using LMstats = WikiParser.LMFastOrderingImpl; //alternative implementations: LMOrderingImpl, LMReferenceImpl
namespace WikiParser
{
    /// <summary>
    /// By Eamon Nerbonne (http://eamon.nerbonne.org/) 
    /// 
    /// This program parses a wiki xml dump and finds sentences in it which text_cat (http://www.let.rug.nl/~vannoord/TextCat/Demo/)
    /// labels as non-english, but probably are english.
    /// 
    /// To do so it uses :
    ///  - a wiki "parser" which parses the xml file and strips markup using a regex, (See WikiMarkupStripper.cs)
    ///  - a custom white space normalizer (not crucial) (see WhitespaceNormalizer.cs)
    ///  - a regex to split english text into sentences (imperfect, of course, see EnglishSentenceFinder.cs)
    ///  - a few simple heuristics and a dictionary to grade the likelyhood that a string is actually an (interesting) english sentence
    ///    (and not just a title or another language), see EnglishSentenceGrader.cs
    ///  - and finally, three reimplementations of text_cat, which should behave identically, but be faster.  
    ///  - 1st: LMReferenceImpl.cs which uses the same algorithm as text_cat
    ///  - 2nd: LMOrderingImpl.cs which doesn't use a hashtable but a sorted list (6x speedup)
    ///  - 3rd: LMFastOrderingImpl.cs which uses 64-bit integer math (RankedBytegram.cs) instead of string ngrams (another 3x speedup)
    ///  
    /// This file contains a large PLINQ query concatenating the above functions.
    /// You will need the Parallel Extensions to .NET to run this code; the June 2008 CTP is at:
    /// http://www.microsoft.com/downloads/details.aspx?FamilyId=348F73FD-593D-4B3C-B055-694C50D2B0F3&displaylang=en
    /// if you don't want to use it, comment out the line marked "//PARALLELIZATION:", remove the reference to the System.Threading.dll,
    /// and recompile.
    /// </summary>
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
            //first we parse the command line and doing boring initialization stuff.
            CommandLineArgs parsedArgs = new CommandLineArgs(args);
            if (!parsedArgs.AreOK) {
                Console.Error.WriteLine("Cannot continue.\n\n");
                parsedArgs.PrintUsage();
                Environment.Exit(1);
                return;//shouldn't be necessary...
            }
            parsedArgs.PrintValues();

            //a dictionary is used (if present) to improve the sentence grading.
            Console.Write("Loading Dictionary...");
            EnglishSentenceGrader.LoadDictionary( parsedArgs.LMFileSearchPath.GetFiles("english.ngl").FirstOrDefault());
            Console.WriteLine("done.");

            //the text_cat *.lm files are read to build language models
            Console.Write("Loading Language Models...");
            var langs = (
                from lmFile in parsedArgs.LMFiles
                let language = new LMstats(lmFile)
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

            //Open the wiki dump file and initialize certain tracking variables used to print continual updates...
            Stream stream = parsedArgs.WikiFilePath.OpenRead();
            XmlReader reader = XmlReader.Create(stream);
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
                .AsParallel()//PARALLELIZATION: comment out this line to disable parallel processing.
                ;

            //The following query composes the various components into one big function.
            var parallelQuery = //This won't be executed until enumerated over.
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
                                let stats = new LMstats(sentence, "unknown")
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
            
            //The parallelQuery function is executed and all sentences it produces are logged.
            foreach (var entry in parallelQuery) {
                foreach (var hit in entry.Hits) {
                    if (hit.damage * 1.05 < entry.EnglishDamage && hit.damage < 1.05 * entry.BestDamage) {
                        //all language that match 5% better than english are considered,
                        //but only if they're no more than 5% worse than the best matching language
                        hit.language.HitRun++;
                        hit.language.HitCounter++;
                        hit.language.AppendToLog(entry.Sentence, entry.PageTitle);
                    } else {
                        hit.language.HitRun = 0;
                    }
                }
                sentencecount++;
               // if (sentencecount >= 30000) break;//for benchmarking.

                pageCount = Math.Max( entry.PageIndex,pageCount);
                if (DateTime.Now - lastTime > TimeSpan.FromSeconds(1.0)) {
                    //every second we print a status update.
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
