open System.IO

#r @"packages\Newtonsoft.Json.4.5.11\lib\net40\Newtonsoft.Json.dll";

let table = 
    __SOURCE_DIRECTORY__ + @"\dominion - Card Names (translated).csv"
    |> File.ReadAllLines 
    |> Array.map (fun line -> line.Split ',')

let colCount = table |> Array.map (fun line -> line.Length) |> Array.max

let groupWhen f xs = 
    let rec loop grps grp xs = 
        match grp, xs with 
        | [], [] -> grps
        | ls, [] -> List.rev ls :: grps
        | a::ls, x::rest when f x -> 
            loop (List.rev grp ::grps) [x] rest
        | _, x::rest -> loop grps (x::grp) rest
    loop [] [] xs |> List.rev

type TranslatedName = { English:string; German:string; Dutch:string; French:string }
let EmptyName = { English = ""; German = ""; Dutch = ""; French = "" }

let mkName ls = 
    let add scratch = function
    |  ("English", s) -> { scratch with English = s }
    |  ("Deutsch", s) -> { scratch with German = s }
    |  ("Nederlands", s) -> { scratch with Dutch = s }
    |  ("Français", s) -> { scratch with French = s }
    | (lang, s) -> failwith ("Language " + lang + " not recognized (s: " + s + ")")
    List.fold add EmptyName ls

type DominionCard = { Name : TranslatedName; Price: string }
type DominionSet = { Name : TranslatedName; Cards: DominionCard list }


let cols = 
    let tryGet arr i = if Array.length arr > i then Some arr.[i] else None
    let orDefault def maybe = defaultArg maybe def
    [0..colCount]
    |> List.map (fun cI ->
            table 
                |> Array.map ((fun row -> tryGet row cI) >> Option.bind (fun str -> if str.Length = 0 then None else Some str))
                |> Array.rev
                |> Seq.skipWhile Option.isNone
                |> Array.ofSeq 
                |> Array.rev
                |> Array.map (orDefault "")
            )
    |> Seq.takeWhile (fun c -> c.Length = 0  || c.[0] <> "Guilds")
    |> List.ofSeq
    |> groupWhen (fun col -> col.Length >= 2  && col.[1] = "English")
    |> List.map (fun cols ->
            let rowIndexes = [2..cols|>List.head|>Array.length]
            let priceCol :: nameCols = cols |> List.rev
            let cards = 
                    rowIndexes 
                    |> List.map (fun i -> 
                            { 
                                Name = 
                                    nameCols
                                    |> List.map (fun arr ->  arr.[1],  tryGet arr i |> orDefault "")
                                    |> mkName 
                                Price = tryGet priceCol i |> orDefault ""
                            }
                         )
                    |> List.filter (fun card -> card.Name.English <> "")
            {
                Name = nameCols |> List.map (fun arr -> arr.[1], arr.[0]) |> mkName
                Cards = cards
            }
        )
    //|> Seq.gr

File.WriteAllText (__SOURCE_DIRECTORY__ + @"\cards.json", Newtonsoft.Json.JsonConvert.SerializeObject (cols, Newtonsoft.Json.Formatting.Indented))

