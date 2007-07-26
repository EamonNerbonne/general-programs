using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using EamonExtensionsLinq;

namespace SongDataLib {
    public static class SongUtil {


        public static int? StringToNullableInt(string num) {
            return FuncUtil.Swallow<int?>(()=> int.Parse(num), ()=>null);
        }


        public static bool Contains(byte[] elem, byte[] substring) {
            for (int i = 0; i <= elem.Length - substring.Length; i++) {
                bool match = true;
                for (int j = 0; j < substring.Length; j++) {
                    if (elem[i + j] != substring[j]) {
                        match = false;
                        break;
                    }
                }
                if (match)
                    return true;

            }
            return false;
        }
        public static IEnumerable<byte> makeCanonical(string input) {
            foreach (char c in makeCanonicalHelper(input)) {
                int ord = Convert.ToInt32(c);//this is kinda slow, plain casting to short better?
                if (ord < 256)
                    yield return (byte)ord;
            }
        }

        static IEnumerable<char> makeCanonicalHelper(string input) {
            //splits accents and the likes from the characters.  FormKD even splits a single char ¾ into 3/4.
            string temp = input.ToLower().Normalize(NormalizationForm.FormD);//Normalize is fast! Much faster that ToLower for instance.
            foreach (char c in temp) {
                UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(c); //this is REALLY slow: takes 66% of the of this function.
                if (c == '\t')
                    yield return c;
                else if (uc == UnicodeCategory.NonSpacingMark)
                    continue;
                else if (uc == UnicodeCategory.FinalQuotePunctuation || uc == UnicodeCategory.InitialQuotePunctuation || c == '\"')
                    yield return '\'';
                else if (uc == UnicodeCategory.OpenPunctuation)
                    yield return '(';
                else if (uc == UnicodeCategory.ClosePunctuation)
                    yield return ')';
                else if (uc == UnicodeCategory.Control)
                    continue;
                else
                    yield return c;
            }
        }


    }
}
