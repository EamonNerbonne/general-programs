#light
#I @"C:\Program Files\Reference Assemblies\Microsoft\Framework\v3.5"
#I @"C:\Users\Eamon\eamonhome\docs-trunk\VS.NET2008\EamonExtensionsLinq\bin\Debug"
#I @"C:\Users\Eamon\eamonhome\docs-trunk\VS.NET2008\EmnImaging\HWRsplitter\bin\Debug"

#r "System.Xml.Linq.dll"

#if COMPILED
#R "EamonExtensionsLinq.dll"
#R "HWRsplitter.dll"
#else
#r "EamonExtensionsLinq.dll"
#r "HWRsplitter.dll"
#endif


open HWRsplitter

Program.Main(Array.init 0 (fun x-> ""))
EamonExtensionsLinq.DebugTools.ConsoleExtension.PrintProperties(Program.prog.linesAnnot.annotLines.[0],"annotline0")