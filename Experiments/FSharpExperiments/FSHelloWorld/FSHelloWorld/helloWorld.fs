#light
module HelloWorld
#I @"C:\Users\Eamon\eamonhome\docs-trunk\VS.NET2008\EamonExtensionsLinq\bin\Debug"
#I @"C:\Users\Eamon\eamonhome\docs-trunk\VS.NET2008\EmnImaging\EmnImaging\bin\Debug"
#R @"EamonExtensionsLinq.dll"
#R @"EmnImaging.dll"

// note #r vs. #R: capital copies file locally, needed for non-GAC stuff, not allowed for FSI.exe

#I @"C:\Program Files\Reference Assemblies\Microsoft\Framework\v3.0"
#r @"WindowsBase.dll"
#r @"PresentationCore.dll"
#r @"PresentationFramework.dll"


open System
open System.Drawing
open EamonExtensionsLinq
open EmnImaging
open System.Windows

let f = (fun x -> x+x)
let url = "http://ws.audioscrobbler.com/1.0/track/Saint%20Etienne/Message%20in%20a%20Bottle/similar.xml"

type ('q,'p) TryEmn =
  | Success of 'q
  | Error of 'p

let tryit f = 
  try 
    Success f
  with 
    | exn as e -> Error e 
;;

Printf.printf "Hello World: %s\n" url;
//ConsoleExtension.PrintAllDebug( url);

EamonExtensionsLinq.DebugTools.ConsoleExtension.PrintProperties(tryit,"tryit");

let bmpPath = @"C:\Users\Eamon\HWR\NL_HaNa_H2_7823_0227.tif"
//Also Possible to use NativePtr and NativeArray instead of util lib, but then I can't declare structs!
let image = ImageIO.Load(bmpPath)

let bitmapWpfWindow = new ShowImg.ShowMyBitmap(image)

bitmapWpfWindow.Show()

#if COMPILED
[<STAThread()>]
do 
    let app =  new Application() in
    app.Run() |> ignore
#endif

//printf read_line;
//printf "\n";
//new System.Net.WebClient()
//System.Net.WebRequest.Create("http://google.com/").