using System.Text.RegularExpressions;

namespace WikiParser
{
    public static class WikiMarkupStripper
    {
        public static string StripMarkup(string wikiMarkedUpText) {
            string text = wikiMarkedUpText;
            text= markupStripper.Replace(text, "");
            text = markupReplace.Replace(text, "${txt}");
            return text;
        }

        static readonly string[] regexes1 = {
            @"(?>'')'*",
            @"(?><)(!--([^-]|-[^-]|--[^>])*-->|([mM][aA][tT][hH]|[rR][eE][fF]|[sS][mM][aA][lL][lL]).*?(/>|</([mM][aA][tT][hH]|[rR][eE][fF]|[sS][mM][aA][lL][lL])>))",
            @"^((?>#)[rR][eE][dD][iI][rR][eE][cC][tT].*$|(?>\*)\**|(?>=)=*)",
            @"(?<=&)(?>[aA])[mM][pP];",
            @"&(?>[nN])([bB][sS][pP]|[dD][aA][sS][hH]);",
            @"=+ *$",
        };
        static readonly string[] regexesReplace = {
            @"\{(\|([^\|]|\|[^\}])*\||\{([^\}]|\}[^\}])*\})\}",
            @"(?>\[\[([^\[:\|\]]+):)([^\[\]]|\[\[[^\[\]]*\]\])*\]\]",
            @"\[([^ \[\]]+( (?<txt>[^\[\]]*))?|\[((?<txt>:[^\[\]]*)|(?<txt>[^\[\]:\|]*)|[^\[\]:\|]*\|(?<txt>[^\[\]]*))\])\]",
            @"</?[a-zA-Z]+( [^>]*?)?/?>",
        };
        const RegexOptions options = RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.CultureInvariant;
        static Regex markupStripper = new Regex(RegexHelper.Combine(regexes1), options);
        static Regex markupReplace = new Regex(RegexHelper.Combine(regexesReplace), options);
    }
}
