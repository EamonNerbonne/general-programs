using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;

namespace EmnExtensions.Text
{
    public static class Canonicalize
    {
        static readonly byte[] categorycache;
        static readonly BitArray reasonablechar;

        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        [SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
        static Canonicalize()
        {
            foreach (UnicodeCategory cat in Enum.GetValues(typeof(UnicodeCategory))) {
                var val = (int)cat;
                if (val < 0 || val > 255) {
                    throw new ApplicationException("Programming Error: Invalid category number!  Can't cache in byte: " + cat);
                }
            }

            categorycache = new byte[char.MaxValue + 1];
            reasonablechar = new(char.MaxValue + 1);
            //translatedchar = new char[(int)char.MaxValue + 1];
            foreach (var c in Enumerable.Range(0, char.MaxValue + 1)) {
                var cat = CharUnicodeInfo.GetUnicodeCategory((char)c);
                categorycache[c] = (byte)(int)cat;
                reasonablechar[c] = IsSafeChar((char)c) && cat != UnicodeCategory.Format && cat != UnicodeCategory.Control && cat != UnicodeCategory.OtherNotAssigned && cat != UnicodeCategory.PrivateUse;
            }

            reasonablechar['\n'] = true;
            reasonablechar['\t'] = true;
        }

        public static UnicodeCategory FastGetUnicodeCategory(char c)
            => (UnicodeCategory)categorycache[c];

        /// <summary>
        /// Determines whether a character is safe to use, essentially meaning "can be present in an XML file".
        /// This allows the surrogate pair ranges, though they aren't necessarily valid in xml (they must come in pairs) and simply bans really bad things you don't ever want,
        /// being control characters below 0x20 (except tab, carriage return and newline), 0xfffe and 0xffff.
        /// </summary>
        public static bool IsSafeChar(char c)
            => c >= 0x20 && c < 0xfffe || c == '\n' || c == '\t' || c == '\r';

        /// <summary>
        /// Determines whether a character is safe to use and not ' ','\t','\r' or '\n' - see IsSafeChar.
        /// </summary>
        public static bool IsSafeNonWhitespaceChar(char c)
            => c > 0x20 && c < 0xfffe;

        /// <summary>
        /// Determines whether a character is a "reasonable" text character.  Basically, this means any kind of symbol, spacing, surrogate or newline,
        /// but not unassigned, format, control or private characters (except newline and tab, which are OK).
        /// </summary>
        public static bool IsReasonableChar(char c)
            => reasonablechar[c];

        /// <summary>
        /// Strips all characters deemed unsafe by IsSafeChar.  Returns null if the input is null or empty.
        /// </summary>
        public static string TrimAndMakeSafe(string input)
        {
            if (input == null) {
                return null;
            }

            var retval = new char[input.Length];
            int pos = 0, srcPos = 0;
            while (srcPos < input.Length && !IsSafeNonWhitespaceChar(input[srcPos])) {
                srcPos++; //skip inital space or junk
            }

            while (srcPos < input.Length) {
                var c = input[srcPos];
                if (IsSafeChar(c)) {
                    retval[pos++] = c;
                }

                srcPos++;
            }

            while (pos > 0 && !IsSafeNonWhitespaceChar(retval[pos - 1])) {
                pos--; //skip trailing space or junk
            }

            return pos == 0 ? null : new string(retval, 0, pos);
        }

        public static string CanonicalizeBasic(this string input)
            => Basic(input);

        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        public static string Basic(string input)
        {
            //splits accents and the likes from the characters.  FormKD even splits a single char ¾ into 3/4.
            var temp = input.ToLowerInvariant().Normalize(NormalizationForm.FormD); //Normalize is fast! Much faster than ToLower for instance.
            var output = new StringBuilder(temp.Length);
            foreach (var c in temp) {
                if (c >= 'a' && c <= 'z') {
                    output.Append(c);
                } else if (c >= '1' && c <= '9') {
                    output.Append(c);
                } else if (c == '\t' || c == ' ') {
                    output.Append(' ');
                } else if (c == '\n') {
                    output.Append('\n');
                } else if (c == '0') {
                    output.Append('o'); //normalize the number 0 and the letter 'o' - controversial.
                } else {
                    switch (FastGetUnicodeCategory(c)) {
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
                            switch (c) {
                                case '\"':
                                case '`':
                                case (char)180:
                                    output.Append('\'');
                                    break; //180 '''
                                case 'æ':
                                    output.Append("ae");
                                    break;
                                case (char)248:
                                    output.Append('o');
                                    break; // 'o'  as opposed to 'o'
                                case (char)240:
                                    output.Append('d');
                                    break; //240 'd'   as opposed to 'd'
                                case (char)215:
                                    output.Append('x');
                                    break; // 'x'  as opposed to 'x'
                                case (char)222:
                                case (char)254:
                                case (char)190:
                                case (char)160:
                                case '·':
                                case '_':
                                case '^':
                                case (char)175:
                                    output.Append(' ');
                                    break; // 254 '_',222 '_',190 '_',175 '_',//160 ' '
                                case 'ß':
                                    output.Append("ss");
                                    break;
                                case '¿':
                                    output.Append('?');
                                    break;
                                case '½':
                                    output.Append("1/2");
                                    break;
                                case '¼':
                                    output.Append("1/4");
                                    break;
                                case (char)185:
                                    output.Append('1');
                                    break; //185: '1'
                                case (char)184:
                                    output.Append(',');
                                    break;
                                case (char)179:
                                    output.Append('3');
                                    break; //179: '3'
                                case '²':
                                    output.Append('2');
                                    break;
                                case (char)174:
                                    output.Append('r');
                                    break; //174 'r'
                                case 'µ':
                                    output.Append('u');
                                    break;
                                case '¢':
                                    output.Append('c');
                                    break;
                                case 'ª':
                                    output.Append('a');
                                    break;
                                case '£':
                                    output.Append('l');
                                    break;
                                case 'º':
                                    output.Append('o');
                                    break; //use letter, not number, for canonicalization purposes.
                                case '$':
                                    output.Append('s');
                                    break;
                                case '\\':
                                    output.Append('/');
                                    break; //for stupid path stuff
                                case '<':
                                    output.Append('(');
                                    break;
                                case '>':
                                    output.Append(')');
                                    break;
                                case '÷':
                                    output.Append('/');
                                    break;
                                case (char)173:
                                    output.Append('-');
                                    break;
                                case '¡':
                                    output.Append('!');
                                    break; //this one's difficult
                                case (char)169:
                                    output.Append('c');
                                    break; //169 'c'
                                default:
                                    if (c < 128) {
                                        output.Append(c);
                                    }

                                    break;
                            } //Unclear: should I normalize any of:  | # % & * + ; : = ? @ ~

                            break;
                    }
                }
            }

            return output.ToString();
        }
    }
}
