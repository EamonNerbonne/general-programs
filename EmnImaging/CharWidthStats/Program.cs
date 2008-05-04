using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using HWRsplitter;

namespace CharWidthStats {
    class Program {
        class MeasuredWord {
            public string text;
            public double length;
        }

        static void Main(string[] args) {
            new Program().Exec();

        }
        const int SpaceCode = (int)' ';
        List<char> relevantChars = new List<char>();
        private void Exec() {
            double[] charLengths = new double[(int)char.MaxValue + 1];
            int[] charCounts = new int[(int)char.MaxValue + 1];

            foreach (MeasuredWord word in usefulWords) {
                double length = word.length;
                int num = word.text.Length + 1;
                double percharLength = length/num;
                foreach (char c in word.text) {
                    int charCode = (int)c;
                    charLengths[c] += percharLength;
                    charCounts[c]++;
                }
                charLengths[SpaceCode] += percharLength;
                charCounts[SpaceCode]++;
            }
            for (int i = 0; i < charCounts.Length; i++) {
                if (charCounts[i] > 0) {
                    relevantChars.Add((char)i);
                    charLengths[i] /= charCounts[i];
                }
            }
            char maxChar = relevantChars[relevantChars.Count - 1];
            charLengths = charLengths.Take((int)maxChar + 1).ToArray();
            charCounts = charCounts.Take((int)maxChar + 1).ToArray();
            double[] charError = new double[charLengths.Length];
            //OK, so now charLengths contains the "average" length of chars,
            //and charcount the count of these chars.
            //relevantChars nicely stores those we'll need to look at.

            //concept then:  find all those words which contain a char.
            //then, find out if the word is too short or two long.
            //propose that the char be changed in width as propotional to its participation in the word to compensate.


            //so, for each word, measure it's length using char estimates.
            //determine difference to "real" length.
            //then, distribute that error over these chars into charError.
            //finally, change charLengths a little and rerun.
            double[] lengthVar = new double[charLengths.Length];

            foreach (int iteration in Enumerable.Range(0,10000)) {
                double absWordError = 0,absCharError=0;
                Array.Clear(lengthVar,0,lengthVar.Length);

                foreach (MeasuredWord word in usefulWords) {
                    double realLength = word.length;
                    int num = word.text.Length + 1;
                    double predLength = charLengths[SpaceCode] + word.text.ToCharArray().Sum(c => charLengths[(int)c]);
                    double err = predLength - realLength;
                    double perCharErr = err/num;

                    foreach (char c in word.text) {
                        charError[(int)c] += perCharErr;
                        lengthVar[(int)c] += err * err / num;//assuming statistal independance - not really realistic.
                    }
                    charError[SpaceCode] += perCharErr;
                    lengthVar[SpaceCode] += err * err / num;//assuming statistal independance - not really realistic.

                    absWordError += Math.Abs(perCharErr*num);
                }
                foreach (char c in relevantChars) {
                    double avgErr = charError[(int)c] / charCounts[(int)c];
                    charError[(int)c] =0;
                    charLengths[(int)c] -= avgErr;
                    lengthVar[(int)c] /= charCounts[(int)c];

                    absCharError += Math.Abs(avgErr);
                }
                if (iteration % 100 == 0) 
                {
                    Console.WriteLine("After {0}th iteration: {1:f2} avg word error, {2:f2} avg char error.", iteration, absWordError / usefulWords.Length, absCharError / relevantChars.Count);
                    Console.WriteLine("' ':{0:f2}, 'W':{1:f2}, 'i':{2:f2}, 'r':{3:f2}, 'R':{4:f2}, 't':{5:f2}, '\\n':{6:f2}, 0:{7:f2}",
                        charLengths[SpaceCode], charLengths[(int)'W'], charLengths[(int)'i'], charLengths[(int)'r'], charLengths[(int)'R'], charLengths[(int)'t'], charLengths[(int)'\n'], charLengths[(int)0]);
                }
            }
            
            //Initial guess:

            FileInfo charLengthFile = new FileInfo( System.IO.Path.Combine(HWRsplitter.Program.DataPath, "char-width.txt"));

            using (var stream = charLengthFile.OpenWrite())
            using (var writer = new StreamWriter(stream))
                foreach (char c in relevantChars) {
                    writer.WriteLine("{0}, {1}, {2}, '{3}'", (int)c, charLengths[(int)c], lengthVar[(int)c], char.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.Control ? "" : c.ToString());
                    Console.WriteLine("{0}, {1}, {2}, '{3}'", (int)c, charLengths[(int)c], lengthVar[(int)c], char.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.Control ? "" : c.ToString());
                }

            Console.WriteLine("done.");
            Console.ReadLine();
        }
        Dictionary<int, WordsImage> wordImagesByPage,annotImagesByPage;
        public Program () {
            var annotFile=new FileInfo(System.IO.Path.Combine(HWRsplitter.Program.DataPath, "line_annot.txt"));
            var wordImages = from filepath in Directory.GetFiles(
                      System.IO.Path.Combine(HWRsplitter.Program.DataPath, "words-train"), "NL_HaNa_H2_7823_*.words")
                              let filename = System.IO.Path.GetFileName(filepath)
                              where Regex.Match(filename, @"^NL_HaNa_H2_7823_(?<num>\d+).words$").Success
                              select new WordsImage(new FileInfo(filepath)) ;

            wordImagesByPage = wordImages.ToDictionary(wordimage => wordimage.pageNum);
            annotImagesByPage = AnnotLinesParser.GetGuessWords(annotFile, wordImagesByPage.ContainsKey);

            //so now we have the "real" .words file loaded, and the plain annotations.
            usefulWords = GetUsefulWords().ToArray();
            Console.WriteLine("Discarded {0} of {1} lines ({2}%)", discardedLines, totalLines, discardedLines * 100.0 / totalLines);
            Console.WriteLine("Discarded {0} of {1} words ({2}%)", 
                discardedWords,totalWords,discardedWords*100.0/totalWords);
        }

        MeasuredWord[] usefulWords;

            int totalLines, discardedLines;
            int totalWords, discardedWords;
        IEnumerable<MeasuredWord> GetUsefulWords() {

            foreach (int pagenum in wordImagesByPage.Keys) {
                WordsImage userAnnot = wordImagesByPage[pagenum], machineAnnot = annotImagesByPage[pagenum];
                foreach (TextLine line in userAnnot.textlines) {
                    totalLines++;
                    double midpoint = (line.top + line.bottom) / 2.0;
                    TextLine machineLine =
                        machineAnnot.textlines.Where(ml => ml.top < midpoint && ml.bottom > midpoint).FirstOrDefault();
                    if (machineLine == null)
                        discardedLines++;
                    else {
                        foreach (MeasuredWord word in GetWordsFromLine(line, machineLine))
                            yield return word;
                    }
                }
            }
        }

        IEnumerable<MeasuredWord> GetWordsFromLine(TextLine annotLine, TextLine machineLine) {
            var lookupMachineIndex= machineLine.words
                .Select((w, i) => new { Text = w.text, Index = i })
                .ToLookup(pair => pair.Text, pair => pair.Index);
            if (lookupMachineIndex.Contains("\\n")) {
                discardedLines++;
                yield break;
            }
            int lastmachineIndex = machineLine.words.Length - 1;

            
            foreach(Word word in annotLine.words) {
                totalWords++;
                if (lookupMachineIndex.Contains(word.text)) {
                    int bestIndex = -1;
                    double posError = double.MaxValue;
                    double targetPos = word.left + word.right;
                    foreach (int index in lookupMachineIndex[word.text]) {
                        Word mword = machineLine.words[index];
                        double pos = mword.left + mword.right;
                        double err = Math.Abs(pos - targetPos);
                        if (err < posError) {
                            posError = err;
                            bestIndex = index;
                        }
                    }

                    if ( bestIndex == lastmachineIndex) {

                        yield return new MeasuredWord {
                            text = word.text + (char)'\n',
                            length = word.right - word.left
                        };
                    } else if (bestIndex == 0) {
                        yield return new MeasuredWord {
                            text = word.text + (char)0,
                            length = word.right - word.left
                        };
                    } else { //word is present and not at start or end...
                        yield return new MeasuredWord {
                            text = word.text,
                            length = word.right - word.left
                        };
                    }
                } else {
                    discardedWords++;
                }
            }
        }
    }
}
