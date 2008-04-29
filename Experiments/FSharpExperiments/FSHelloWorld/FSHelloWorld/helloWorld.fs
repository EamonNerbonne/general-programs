#light

#I @"C:\Users\Eamon\eamonhome\docs-trunk\VS.NET2008\EamonExtensionsLinq\bin"
#r @"Debug\EamonExtensionsLinq.dll"

open EamonExtensionsLinq.DebugTools
open System
open System.Drawing

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
ConsoleExtension.PrintAllDebug( url);
ConsoleExtension.PrintProperties(tryit,"tryit");
System.Console.ReadKey();

let bmpPath = @"C:\Users\Eamon\HWR\NL_HaNa_H2_7823_0227.tif"
let bmp = Bitmap.FromFile(bmpPath);

let myconvert (x:int16)  = uint16 x 

let copyArr2 (arrSrc:int16[,]) (arrTgt:uint16[,]) (translate:int16->uint16) = 
  for i = 0 to arrSrc.GetLength(0)-1 do
    for j = 0 to arrSrc.GetLength(1)-1 do
      arrTgt.[i,j] <- translate arrSrc.[i,j]


//printf read_line;
//printf "\n";
//new System.Net.WebClient()
//System.Net.WebRequest.Create("http://google.com/").