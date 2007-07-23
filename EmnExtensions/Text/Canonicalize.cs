using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace EamonExtensionsLinq.Text
{
    public static class Canonicalize
    {
        public static string Basic(string input)
        {
            //splits accents and the likes from the characters.  FormKD even splits a single char ¾ into 3/4.
            string temp = input.ToLowerInvariant().Normalize(NormalizationForm.FormD);//Normalize is fast! Much faster that ToLower for instance.
            StringBuilder output = new StringBuilder();
            foreach (char c in temp) {
                switch (CharUnicodeInfo.GetUnicodeCategory(c)) //this is REALLY slow: takes 66% of the of this function.
                {
                    case UnicodeCategory.NonSpacingMark:
                    case UnicodeCategory.Control:
                        continue;
                    case UnicodeCategory.FinalQuotePunctuation:
                    case UnicodeCategory.InitialQuotePunctuation:
                        output.Append('\''); 
                        break;
                    case UnicodeCategory.OpenPunctuation:
                        output.Append('('); 
                        break;
                    case UnicodeCategory.ClosePunctuation: 
                        output.Append(')'); 
                        break;
                    default: 
                        if (c == '\"') 
                            output.Append('\''); 
                        else if ((int)c < 256) 
                            output.Append(c);
                        break;
                }
            }
            return output.ToString();
        }
    }
}
