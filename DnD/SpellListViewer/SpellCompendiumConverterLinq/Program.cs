using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using System.IO;
using EamonExtensionsLinq.Filesystem;
using EamonExtensionsLinq.DebugTools;
using EamonExtensionsLinq;

namespace SpellCompendiumConverterLinq {
    class WordSplit:IComparable<WordSplit> {
        public string word;
        public WordSplit next;
        public int val;
        public WordSplit(string word,WordSplit next,int val){this.word=word;this.val=val;this.next=next;}
        public WordSplit(){}
        public int CompareTo(WordSplit other) {return this.val.CompareTo(other.val);}
        public override string ToString() {
            List<string> words= new List<string>();
            WordSplit t = this;
            while(t!=null) {
                words.Add(t.word);
                t=t.next;
            }
            return string.Join(" ",words.ToArray());
        }
    }
    class Program {
        static string[] sectionNames = new string[]{"Level","Components","Casting Time", "Range","Area","Effect", "Target", "Duration",  "Saving Throw","Spell Resistance"};
        static DirectoryInfo basepath;
        static Dictionary<string,string> sectionNameSet = new Dictionary<string,string>();
        static Dictionary<string,int> dictionary;
        const int CUTOFF = 1000000;//don't search more than this number of possible splits of texts.

        public static bool IsTitle(string line){
            return  line.All(c  =>  c < 'a' || c > 'z') && line.Count(c  =>  c >= 'A' && c <= 'Z') > 2 && line.Count(c=> char.IsLetter(c))*2+1 >= line.Length;
        }

        public static IEnumerable<string> MergeTitle(IEnumerable<string> lines) {
            StringBuilder sb = null;
            foreach(var line in lines) {
                if(IsTitle(line)) {
                    if(sb == null) sb = new StringBuilder();
                    sb.Append(line);
                    sb.Append(" ");
                } else {
                    if(sb != null) {
                        yield return sb.ToString();
                        sb = null;
                    }
                    yield return line;
                }
            }
            if(sb != null) {
                yield return sb.ToString();
                sb = null;
            }
        }

        public static IEnumerable<string> MergeHyphenated(IEnumerable<string> lines) {
            string lastline=null;
            foreach(var currentline in lines) {
                string line = currentline;
                if(lastline!=null) {
                    if(line.Length>0 && char.IsLower(line[0]))
                        line = lastline + line;
                    else
                        yield return lastline;
                    lastline = null;
                }
                if(line.EndsWith("-"))
                    lastline = line.Substring(0,line.Length-1);//strip trailing dash
                /*else if(char.IsLower(line[line.Length-1]))
                    lastline = line;*/
                else
                    yield return line;
            }
            if(lastline!=null) yield return lastline;
        }


        static IEnumerable<WordSplit> TryCorrect(string word,string lowword,int begin, int end) {
            while (begin!=end) {
                string subword = lowword.Substring(begin,end-begin);
                if(dictionary.ContainsKey(subword)){//found prefixed-word
                    if(end == word.Length)
                        yield return new WordSplit(word.Substring(begin,end-begin),null,1);
                    foreach(WordSplit solution in TryCorrect(word, lowword,end,word.Length)) {
                        yield return new WordSplit(word.Substring(begin,end-begin),solution,solution.val+1);
                    }
                } 
                end--;
            }
        }
        static Dictionary<string,int> unknownWord = new Dictionary<string,int>();
        static string Correct(string word) {//adds spaces if that means the word is then in the dictionary.
            string lowword = word.ToLower();
            if(dictionary.ContainsKey(lowword)) return word;
            var solution = TryCorrect(word,lowword,0,word.Length).Take(CUTOFF).Aggregate((WordSplit)null,(a,b) => a==null?b:a.CompareTo(b)<=0?a:b);
            if(solution!=null) {// we found a complete split ya!
                return solution.ToString();
            }else {//no luck, no fix...
                if(unknownWord.ContainsKey(word.ToLower())) {
                    unknownWord[word.ToLower()]++;
                } else{
                    unknownWord[word.ToLower()]=1;
                }
                return word;
            }
        }

        static Regex wordSplit=  new Regex(@"^([\p{Lu}\p{Lt}\p{Ll}\p{Lm}]+|.)*$",RegexOptions.Compiled);

        static IEnumerable <string> splitWord(string word) {
            foreach(Capture cap in wordSplit.Match(word).Groups[1].Captures) {
                yield return cap.Value;
            }
        }
        enum Tok{
            Num,D,Word,Punc,Symbol
        }
        static XElement spelltext2xml(string[] spell,int pos,int ofTotal) {
            var header = spell.TakeWhile(s => !s.StartsWith("Level:"));
            var content = spell.SkipWhile(s => !s.StartsWith("Level:")).ToArray();
            var fields = new Dictionary<string,string>();
            var text = new List<string>();
            //The spell's name
            string title = header.First();

            //the spells type (e.g. "Enchantment (Compulsion) [Evil, Mind-Affecting]"
            string type = string.Join("  ", header.Skip(1).ToArray());

            //lines starting with a capital letter might be new paragraphs.  It's a best guess.
            //so, we'll iterate through the spells content backwards.
            // whenever we find a line starting with a capital we'll assume it's the beginning of a paragraph
            //      and store whatever we'ld encountered (re-reversing to restore the order)
            // whenever we find a known field terminated by a colon, we'll assume we're in the first half of the content
            //      which we'll split by field name.
            var tmp = new List<string>();
            bool firstHit = false;
            foreach(string line in content.Reverse()) {//iterate in reverse
                if(line.Contains(":") && sectionNameSet.ContainsKey(line.Substring(0,line.IndexOf(':'))) ) {
                    string fieldName = line.Substring(0,line.IndexOf(':'));
                    string fieldContent = line.Substring(line.IndexOf(':')+1)
                        + string.Join(" ",tmp.AsEnumerable().Reverse().ToArray());
                    tmp = new List<string>();
                    firstHit = true;
                    fields[fieldName] = fieldContent;
                } else {
                    tmp.Add(line);
                    if(!firstHit && line[0]>='A' && line[0]<='Z') {
                        string[] words = string.Join(" ",tmp.AsEnumerable().Reverse().ToArray()).Split(' ');

                        tmp = new List<string>();
                        foreach(var word in words.SelectMany(w=>splitWord(w))) {
                            if(char.IsLetter(word[0])){
                                tmp.Add(Correct(word));
                            } else {
                                tmp.Add(word);
                            }
                        }
                        StringBuilder text2add=new StringBuilder();
                        Tok last = Tok.Symbol;
                        foreach(string token in tmp) {
                            if(token.Length ==0) continue;
                            UnicodeCategory cat = char.GetUnicodeCategory(token[0]);
                            if (token[0] == '’' || token[0]=='\'')
                            {
                                text2add.Append(token);
                                last = Tok.Symbol;
                            }else if(cat == UnicodeCategory.OpenPunctuation) {
                                text2add.Append(' ');
                                text2add.Append(token);
                                last = Tok.Symbol;
                            } else if(token =="d"){
                                if(last != Tok.Num && last!=Tok.Symbol) {
                                    text2add.Append(' ');
                                }
                                text2add.Append(token);
                                last = Tok.D;
                            }else if(char.IsLetter(token[0])) {
                                if(last!=Tok.Symbol) text2add.Append(' ');
                                text2add.Append(token);
                                last = Tok.Word;
                            } else if(char.IsPunctuation(token[0]) || char.IsSymbol(token[0])) {
                                if(token[0] =='-'||token[0]=='+' && last==Tok.Word) text2add.Append(' ');
                                text2add.Append(token);
                                last = Tok.Punc;
                            } else if (char.IsNumber(token[0])) {
                                if(last == Tok.Word) {
                                    text2add.Append(' ');
                                }
                                text2add.Append(token);
                                last = Tok.Num;
                            } else {//should never be reached, but for safety.
                                text2add.Append(' ');
                                text2add.Append(token);
                                last = Tok.Word;
                            }
                        }
                        string[] bulletedSplit = text2add.ToString().Split('•');
                        if (bulletedSplit.Length > 0) 
                            foreach (string textBit in bulletedSplit.Skip(1).Reverse()) text.Add("• "+textBit);
                        text.Add(bulletedSplit[0]);
                        tmp = new List<string>();
                    }
                }
            }
            if(tmp.Count>0){
                Console.WriteLine(string.Join(" ",tmp.ToArray()));
                Console.ReadLine();
            }
            text.Reverse();
            StringBuilder capTitle = new StringBuilder();
            bool inword = false;
            foreach(char c in title) {
                if(char.IsLetter(c)) {
                    capTitle.Append(inword?char.ToLower(c):c);
                    inword = true;
                } else {
                    capTitle.Append(c);
                    inword = false;
                }
            }
            title = capTitle.ToString().Trim();
            if(pos*100/ofTotal != (pos-1)*100/ofTotal) Console.WriteLine("{0}% done.",pos*100/ofTotal);
            //Console.WriteLine("{0} of {1}",pos,ofTotal);
            //so now we've set: fields[], title, type
            return new XElement("div",
                new XAttribute("spell",title),
                new XElement("h2",title),
                new XElement("strong",type),
                new XElement("table",new XAttribute("class","statBlock"),
                    sectionNames.Where(n => fields.ContainsKey(n)).Select(n=>
                    new XElement("tr",
                        new XElement("th",n+":"),
                        new XElement("td",fields[n])
                    ))),
                text.Select(p=>new XElement("p",p.AsEnumerable())));

        }

        static void Main(string[] args) {
            basepath = new DirectoryInfo(args[0]);
            if(!basepath.Exists) throw new DirectoryNotFoundException("You must specify the directory containing all data files as a first parameter");
            foreach (var name in sectionNames) sectionNameSet[name] = name;
            
            Console.Write("Processing...");


            IEnumerable<string> lines =basepath.GetFiles("*.input.txt").Single().GetLines();

            IEnumerable<string> words = from file in basepath.GetFiles("*.include.txt") from line in file.GetLines() select line;
            IEnumerable<string> excludeWords = from file in basepath.GetFiles("*.exclude.txt") from line in file.GetLines() select line;

            dictionary = new Dictionary<string,int>();
            Console.Write("[init] ");
            foreach(var word in words) dictionary[word]=0;
            Console.Write("[includeDict] ");
            foreach (var word in excludeWords) dictionary.Remove(word);
            Console.Write("[excludeDict] [dict] ");


            //select the right region of the file:
            lines = lines.Select((s) => s.Trim()).SkipWhile(s => s!= "SPELL DESCRIPTIONS").Skip(1);
            lines = lines.TakeWhile((s) => s!="CHAPTER 2: SPELL LISTS");

            //strip out gunk text and page transitions
            lines = lines.Where((s) => 
                (!s.StartsWith("Illus.")) && 
                s!= "DESCRIPTIONS" && 
                s!="1" && 
                s!="" &&
                s!="SPELL" && 
                s !="DESCRIPTIONS" && 
                (!s.Contains("CHAPTER")));

            //merge titles that were split across lines
            lines = MergeTitle(lines);

            //merge hyphenated words across line boundaries.
            lines = MergeHyphenated(lines);

            //split by spell here!
            string[][] spelltexts = lines.SplitWhen(IsTitle).Select(sp => sp.ToArray()).ToArray();
            Console.WriteLine("[split]");
            Console.WriteLine("{0} spells found.  converting:",spelltexts.Length);

            
            new XElement("html",new XElement("body",
                spelltexts.Select((st,i) => spelltext2xml(st,i,spelltexts.Length)))).Save(Path.Combine(basepath.FullName,"SpellCompendiumOCR.xml"));
            Console.WriteLine("done.");
        }
    }
}
