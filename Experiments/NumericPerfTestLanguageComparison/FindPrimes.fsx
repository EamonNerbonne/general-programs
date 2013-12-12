#r @"packages\FSharpx.Core.1.8.28\lib\40\FSharpx.Core.dll"
open FSharpx.Collections
open System.Collections.Generic
#time

let findPrimes =
    let mutablePrimes = new List<int>()
    let rec findPrimes next =
        seq {
            if not <| Seq.exists (fun p -> next % p = 0) mutablePrimes then
                yield next
                mutablePrimes.Add next
            yield! findPrimes (next + 1)
        }
    findPrimes 2 |> LazyList.ofSeq

findPrimes 
    //|> LazyList.map (fun p -> printfn "%d" p; p) 
    |> Seq.takeWhile (fun p -> p < 300000) |> Seq.toList |> List.rev |> List.head
