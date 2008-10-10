using System.Text.RegularExpressions;
using System;

namespace WikiParser
{
    public static class EnglishSentenceGrader
    {
        static readonly Regex caps = new Regex("[A-Z]", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        static readonly Regex nums = new Regex("[0-9]", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        static readonly Regex wordChars = new Regex(@"\w", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        static readonly Regex words = new Regex(@"(^|\s)\S+", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        static readonly Regex capwords = new Regex(@"(^| )[^ a-zA-Z_0-9]*[A-Z]\S*", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        public static double GradeEnglishSentence(string sentenceCandidate) {
            int capCount = caps.Matches(sentenceCandidate).Count;
            int numCount = nums.Matches(sentenceCandidate).Count;
            int wordCharCount = wordChars.Matches(sentenceCandidate).Count;
            int wordCount = words.Matches(sentenceCandidate).Count;
            int capWordCount = capwords.Matches(sentenceCandidate).Count;
            int charCount = sentenceCandidate.Length;
            return (wordCharCount - numCount - capCount) / (double)charCount + (Math.Min(wordCount, 6) * 0.15) - 1.5 * ((capWordCount - 1) / (double)wordCount);
        }


    }
}
