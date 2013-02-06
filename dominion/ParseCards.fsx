open System.IO

let table = 
    __SOURCE_DIRECTORY__ + @"\dominion - Card Names (translated).csv"
    |> File.ReadAllLines 
    |> Array.map (fun line -> line.Split ',')

let colCount = table |> Array.map (fun line -> line.Length) |> Array.max

let cols = 
    let tryGet arr i = if Array.length arr > i then Some arr.[i] else None
    [0..colCount]
    |> List.map (fun cI ->
            table 
                |> Array.map ((fun row -> tryGet row cI) >> Option.bind (fun str -> if str.Length = 0 then None else Some str))
                |> Array.rev
                |> Seq.skipWhile Option.isNone
                |> Array.ofSeq 
                |> Array.rev
                |> Array.map (fun maybeString -> defaultArg maybeString "")
            )
//				.Select(row=>row.Skip(ci).FirstOrDefault())
//				.Reverse().SkipWhile(s=>string.IsNullOrWhiteSpace(s)).Reverse()
//				.ToArray())
//		.ToArray();
//		
//	
//	
//	var badCols = new[]{ "Basic Cards", "Dark Ages", "Seaside", "Guilds" ,"Promo"};
//	var extraCards = new[]{"stash"};
//	
//	var englishCols = 
//		cols.Where(col=>col[1]=="English")
//		.Select(col=>col.Take(1).Concat(col.Skip(2)).ToArray())
//		.ToArray();
//	var setsIHave = englishCols.Where(col=>!badCols.Contains(col[0])).ToArray();
//	setsIHave.Select(col=>col.First()).Dump();
//	
//	setsIHave.SelectMany(set=>set.Skip(1).Select(card=>new { card, set=set.First() })).OrderBy(c=>c.card) .Dump();
//}
//
//class Set {
//	public string English, Deutsch, Nederlands, Francais;
//	public Card[] Cards;
//}
//
//class Card {
//	public string Set;
//	public string English, Deutsch, Nederlands, Francais;
//	public string Price;
//}
//
//// Define other methods and classes here
//