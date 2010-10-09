using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
namespace WikiParser
{
    public static class EnglishSentenceFinder
    {
        const string sentenceRegex =
    @"(?<=[\.\?!]\s+|^)((?<sentence>(\(|" + "\"" + @")?[A-Z]( ([Ss]t|Mrs?|dr|ed|c|v(s|ol)?|[nN]o(?=\s+[0-9])|et al)\.|\(\w+\.|[A-Z]\. |\.([\w\d]| (\w\.( \w\.)*|[a-z]))|[^\.\n\?!])+[\.\?!\n](\)|" + "\"" + @")?))(?=\s|$)";

        public static IEnumerable<string> FindEnglishSentences(string text) {
            return 
                from Match m in sentenceFinder.Matches(text)
                where m.Success
                select m.Groups["sentence"].Value;
        }

        const RegexOptions options = RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.CultureInvariant;
        static Regex sentenceFinder = new Regex(sentenceRegex, options);
    }
}
