﻿using System.Text.RegularExpressions;
using System.Linq;
using System;
using System.IO;
using System.Collections.Generic;

namespace AvsAnTrie {
    public partial class RegexTextUtils {
        //Note: regexes are NOT static and shared between threads because of... http://stackoverflow.com/questions/7585087/multithreaded-use-of-regex
        //This code is bottlenecked by regexes, so this really matters, here.
        const RegexOptions options = RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.CultureInvariant;

        static readonly Regex firstLetter = new Regex(@"(?<=^[^\s\w]*)\w", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant);
        static string Capitalize(string word) {
            return firstLetter.Replace(word, m => m.Value.ToUpperInvariant());
        }

        static HashSet<string> dictionary;
        public static void LoadDictionary(FileInfo fileInfo) {
            var dictElems =
                File.ReadAllLines(fileInfo.FullName)
                    .Select(line => line.Trim())
                    .SelectMany(word => new[] { word, Capitalize(word) });
            dictionary = new HashSet<string>(dictElems);
        }
    }
}
