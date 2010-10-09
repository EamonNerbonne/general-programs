using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace WikiParser {
	public static class EnglishSentenceGrader {
		static readonly Regex caps = new Regex("[A-Z]", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		static readonly Regex nums = new Regex("[0-9]", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		static readonly Regex wordChars = new Regex(@"\w", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		static readonly Regex words = new Regex(@"(^|\s)\S+", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		static readonly Regex capwords = new Regex(@"(^| )[^ a-zA-Z_0-9]*[A-Z]\S*", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

		public static double GradeEnglishSentence(string sentenceCandidate) {
			int capCount = caps.Matches(sentenceCandidate).Count;
			int numCount = nums.Matches(sentenceCandidate).Count;
			int wordCharCount = wordChars.Matches(sentenceCandidate).Count;
			var wordMatches = words.Matches(sentenceCandidate);
			int wordCount = wordMatches.Count;
			double pref = 0.0;
			if (dictionary != null) {
				int inDictCount = (
					from Match m in wordMatches
					where m.Success
					let word = m.Value.Trim().ToLowerInvariant()
					where dictionary.Contains(word)
					select word
				 ).Count();
				pref = (2 * inDictCount - wordCount) / (double)wordCount;
			}
			int capWordCount = capwords.Matches(sentenceCandidate).Count;
			int charCount = sentenceCandidate.Length;
			double capRate = wordCount == 1 ? 0.5 :
				(capWordCount - 1) / (double)(wordCount - 1);
			return (wordCharCount - numCount - capCount) / (double)charCount
				+ 0.3 * Math.Min(wordCount - capWordCount, 6)
				- 1.0 * capRate
				+ pref;
		}

		static HashSet<string> dictionary;

		public static void LoadDictionary(FileInfo fileInfo) {
			if (fileInfo == null)
				dictionary = null;
			else {
				var dictElems =
					fileInfo.GetLines().Select(line => line.Trim().ToLowerInvariant());
				dictionary = new HashSet<string>(dictElems);
			}
		}
	}
}
