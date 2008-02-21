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
            StringBuilder output = new StringBuilder(temp.Length);
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
								if(c >= 'a' && c <= 'z') output.Append(c);
								else
									switch(c) {
										case '\"':case '`':case (char)180: output.Append('\''); break; //180 '''
										case 'æ': output.Append("ae"); break;
										case (char)248: output.Append('o'); break;// 'o'  as opposed to 'o'
										case (char)240: output.Append('d'); break;//240 'd'   as opposed to 'd'
										case (char)215: output.Append('x'); break;// 'x'  as opposed to 'x'
										case (char)222:
										case (char)254:
										case (char)190:
										case (char)160:
										case '·':
										case '_':
										case '^'://TODO-OK?
										case (char)175: output.Append(' '); break;// 254 '_',222 '_',190 '_',175 '_',//160 ' '
										case 'ß': output.Append("ss"); break;
										case '¿': output.Append('?'); break;
										case '½': output.Append("1/2"); break;
										case '¼': output.Append("1/4"); break;
										case (char)185: output.Append('1'); break;//185: '1'
										case (char)184: output.Append(','); break;
										case (char)179: output.Append('3'); break;//179: '3'
										case '²': output.Append('2'); break;
										case (char)174: output.Append('r'); break;//174 'r'
										case 'µ': output.Append('u'); break;
										case '¢': output.Append('c'); break;
										case 'ª': output.Append('a'); break;
										case '£': output.Append('l'); break;
										case 'º': output.Append('0'); break;
										case '$': output.Append('s'); break;
										case '<': output.Append('('); break;
										case '\\': output.Append('/'); break;//for stupid path stuff
										case '>': output.Append(')'); break;
										case '÷': output.Append('/'); break;
										case (char)173: output.Append('-'); break;
										case '¡': output.Append('!'); break;//this one's difficult
										case (char)169: output.Append('c'); break;//169 'c'
										default: 
											if(c<128)output.Append(c); break;
									}//TODO: should I normalize any of:  | # % & * + ; : = ? @ ~
                        break;
                }
            }
            return output.ToString();
        }
    }
}
