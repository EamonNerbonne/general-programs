// Learn more about F# at http://fsharp.net
open EmnExtensions.Filesystem
open System.IO
open Microsoft.FSharp.Collections

 
let clearReadOnly d = File.SetAttributes (d,  File.GetAttributes d &&& ~~~FileAttributes.ReadOnly &&& ~~~FileAttributes.System)

let isInSet setElems normalize = normalize >> (setElems |> List.map normalize |>  Set.ofList).Contains
let getLowerExtension path = (Path.GetExtension path).ToLowerInvariant()

let isBad = isInSet [".m3u"; ".nfo"; ".ini"; ".txt"; ".sfv"; ".bak"; ".url"; ".old"; ".pls"; ] getLowerExtension
let isIgnores =  isInSet [".jpg"; ".jpeg"; ".db" ] getLowerExtension

let MusicDir = "E:\\Music"
let Keepers = MusicDir :: List.ofSeq (Directory.EnumerateDirectories MusicDir)
let isKeeper =  isInSet Keepers (fun s -> Path.GetFullPath (s + @"\"))

let moveToParent kid =
    let newPath = Path.GetDirectoryName kid + @"\..\" + Path.GetFileName kid |> Path.GetFullPath 
    if File.Exists newPath then
        File.Delete newPath
    Directory.Move (kid, newPath)


let delete x =
    File.Delete x

let rec processDir d =
    let contents = Directory.EnumerateFileSystemEntries d //lazily evaluated!
    contents |> Seq.iter clearReadOnly 
    contents |> Seq.filter Directory.Exists |> Seq.iter processDir
    try 
        contents |> Seq.filter File.Exists |> Seq.filter isBad |> Seq.iter delete
        if not <| isKeeper d then
            match contents |> Seq.filter (isIgnores >> not) |> List.ofSeq with
                | [] -> Directory.Delete (d, true) //only contains ignored items, OK to delete.
                | [ onlyKid ] ->
                    if Directory.Exists onlyKid then
                        onlyKid |> Directory.EnumerateFileSystemEntries |> Seq.iter moveToParent
                        Directory.Delete (onlyKid, false)
                    else
                        contents |> Seq.iter moveToParent
                        if Seq.length contents > 0 then
                            failwith "whaaaa?"
                        Directory.Delete (d, false)
                | _ -> ()
    with
        | :? System.IO.IOException as e ->
            printfn "error in %s: %s" d e.Message
            File.AppendAllText (@"C:\errlog.txt", d+"\n")
          

processDir MusicDir
