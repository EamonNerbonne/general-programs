#light

#I @"C:\Users\Eamon\eamonhome\docs-trunk\VS.NET2008\EamonExtensionsLinq\bin"
#r @"Debug\EamonExtensionsLinq.dll"

open EamonExtensionsLinq.DebugTools

let f = (fun x -> x+x)
let url = "http://ws.audioscrobbler.com/1.0/track/Saint%20Etienne/Message%20in%20a%20Bottle/similar.xml"

type ('q,'p) TryEmn =
  | Success of 'q
  | Error of 'p

let tryit f = 
  try 
    Success f
  with 
    | e :? exn -> Error e 
;;

Printf.printf "Hello World: %s\n" url;
ConsoleExtension.PrintAllDebug( url);
System.Console.ReadKey();


//printf read_line;
//printf "\n";
//new System.Net.WebClient()
//System.Net.WebRequest.Create("http://google.com/").