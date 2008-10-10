using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WikiParser
{
    public static class RegexHelper
    {
        public static string Combine(IEnumerable<string> regexes) {
            return string.Join("|", regexes.Select(r => "(" + r + ")").ToArray());
        }
    }
}
