WikiParser is a program that finds sentences in wikipedia which text_cat misidentifies as a non-english language.

text_cat is by Gertjan van Noord, and is not directly used by WikiParser.

Run WikiParser.exe without any parameters for usage information.  The directory containing WikiParser should contain a directory LM with *.lm language model files and an (optional) english.ngl english word list.

WikiParser.exe requires the .NET framework 3.5 (or a compatible implementation such as mono 2.0 on linux).

WikiParserParallel.exe is compiled against System.Threading.dll, the microsoft parallel extensions (as of June CTP, to be found at http://www.microsoft.com/downloads/details.aspx?FamilyId=348F73FD-593D-4B3C-B055-694C50D2B0F3&displaylang=en).  If you the parallel extensions installed, use the parallel version - performance scales almost linearly with the number of CPU's in your system.  

by Eamon Nerbonne.