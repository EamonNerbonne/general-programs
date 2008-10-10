using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;

namespace WikiParser
{
    class CommandLineArgs
    {
        public FileInfo WikiFilePath { get; private set; }
        public DirectoryInfo LMFileSearchPath { get; private set; }
        public IEnumerable<FileInfo> LMFiles { get { return LMFileSearchPath.GetFiles("*.lm"); } }
        public IEnumerable<string> IgnoreLangs { get; private set; }
        public bool IncludeNonArticles { get; private set; }

        
        public bool AreOK { get { return WikiFilePath != null && LMFileSearchPath != null; } }


        public CommandLineArgs(string[] args) {
            IgnoreLangs = Enumerable.Empty<string>();

            var exeDir = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory.FullName;
            var sep = Path.DirectorySeparatorChar;
            var LMsearchPath = new[]{
                    "text_cat" +sep + "LM",
                    "LM"
                };
            LMsearchPath = LMsearchPath.Concat(LMsearchPath.Select(path => Path.Combine(exeDir, path))).ToArray();
            string wikiPath = null;

            var argsQ = new Queue<string>(args);
            while (argsQ.Count != 0) {
                string nextOpt = argsQ.Dequeue();
                if (nextOpt.ToUpperInvariant().StartsWith("-LM=")) {
                    LMsearchPath = new[] { nextOpt.Substring(4) };
                }else if(nextOpt.ToUpperInvariant().StartsWith("-I=")) {
                    IgnoreLangs = 
                        IgnoreLangs.Concat(
                        nextOpt.Substring(3).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        );
                } else if(nextOpt.ToLowerInvariant() =="-includenonarticles"){
                    IncludeNonArticles = true;
                } else {
                    wikiPath = nextOpt;
                }

            }

            if (wikiPath == null) {
                Console.Error.WriteLine("Required parameter missing: the location of the wikipedia data file.");
                return;
            }

            foreach (var path in LMsearchPath.Concat(new[] { wikiPath })) {
                if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0) {
                    Console.Error.WriteLine("Invalid path '{0}'", path);
                    return;
                }
            }


            if (!File.Exists(wikiPath)) {
                Console.Error.WriteLine("Could not find wiki dump at '{0}'", wikiPath);
                return;
            }

            WikiFilePath = new FileInfo(wikiPath);

            var LMcandidateDirs =
                from path in LMsearchPath
                let dir = new DirectoryInfo(path)
                where dir.Exists
                where dir.GetFiles("*.lm").Length > 1
                select dir;

            LMFileSearchPath = LMcandidateDirs.FirstOrDefault();
            if (LMFileSearchPath == null) {
                Console.Error.WriteLine("Could not find the LM dir!  Searched in: {0}",
                    string.Join(", ", LMsearchPath)
                    );
                return;
            }

            IgnoreLangs = IgnoreLangs.ToArray().AsEnumerable();
        }

        public void PrintUsage() {
            var exeName = Path.GetFileName(Assembly.GetExecutingAssembly().Location);
            Console.WriteLine("Usage: {0} <wikiPath> [-lm=<directory with .lm statistic files>] [-i=<comma separated languages to ignore>]",exeName);
            Console.WriteLine("By default, language files are searched for in the 'text_cat/LM' and 'LM' subdirectories");
            Console.WriteLine("of the current path and of the executable's path");
            Console.WriteLine("For each hit found, a line will be appended to a log file of the language's name in the current directory");
        }

        public void PrintValues() {
            Console.WriteLine("Wiki xml dump: {0}", WikiFilePath.FullName);
            Console.WriteLine("lm statistic file directory: {0}", LMFileSearchPath.FullName);
            Console.WriteLine("Languages being ignored: {0}", string.Join(",",IgnoreLangs.ToArray()));
            Console.WriteLine("IncludeNonArticles: {0}", IncludeNonArticles);
        }
    }

}
