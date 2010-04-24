// Learn more about F# at http://fsharp.net
open System.IO
open EmnExtensions.Filesystem
open Microsoft.FSharp.Collections

let moveToParent kid =
    let newPath = Path.GetDirectoryName kid + @"\..\" + Path.GetFileName kid |> Path.GetFullPath 
    Directory.Move (kid, newPath)
 
let clearReadOnly d = File.SetAttributes (d,  File.GetAttributes d &&& ~~~FileAttributes.ReadOnly &&& ~~~FileAttributes.System)

let isInSet setElems normalize = normalize >> (setElems |> List.map normalize |>  Set.ofList).Contains
let getLowerExtension path = (Path.GetExtension path).ToLowerInvariant()

let isBad = isInSet [".m3u"; ".nfo"; ".ini"; ".txt"; ".sfv"; ".bak"; ".url"; ".old"; ".pls"; ] getLowerExtension
let isIgnores =  isInSet [".jpg"; ".jpeg"; ] getLowerExtension

let MusicDir = "E:\\Music"
let Keepers = MusicDir :: List.ofSeq (Directory.EnumerateDirectories MusicDir)
let isKeeper =  isInSet Keepers (fun s -> Path.GetFullPath (s + @"\"))




let rec processDir d =
    let contents = Directory.EnumerateFileSystemEntries d
    contents |> Seq.iter clearReadOnly
    Seq.filter Directory.Exists contents |> Seq.iter processDir
    try 
        contents |> Seq.filter File.Exists |> Seq.filter isBad |> Seq.iter File.Delete
        if not <| isKeeper d then
            match List.ofSeq contents with
                | [] -> 
                    Directory.Delete d
                | [ onlyKid ] ->
                    if Directory.Exists onlyKid then
                        onlyKid |> Directory.EnumerateFileSystemEntries |> Seq.iter moveToParent
                        Directory.Delete (onlyKid, false)
                    else
                        moveToParent onlyKid
                | _ -> 
                    ignore 1
    with
        | :? System.IO.IOException as e ->
            printfn "error in %s: %s" d e.Message
            File.AppendAllText (@"C:\errlog.txt", d+"\n")
          

processDir MusicDir
