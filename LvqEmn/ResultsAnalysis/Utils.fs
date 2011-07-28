module Utils

open System

//---------------------------------------------------BASICS
type SampleDistribution = { Count: int; Mean:float; Variance: float; } member this.StdErr = Math.Sqrt( this.Variance / float this.Count);

let pass x f = f x 
let apply f x = f x 
let apply2 f (a, b) = (f a, f b)
let apply3 f (a, b, c) = (f a, f b, f c)
let apply4 f (a, b, c, d) = (f a, f b, f c, f d)

let nullable (x:'a) = new System.Nullable<'a>(x)

let rec flipList lstOfLists = 
    let lstOfNonemptyLists = List.filter (List.isEmpty >> not) lstOfLists
    if List.isEmpty lstOfNonemptyLists then []
    else List.map List.head lstOfNonemptyLists :: (List.map List.tail lstOfNonemptyLists |> flipList)
    
let apply2nd f (v1,v2) = (v1,f v2)
let apply1st f (v1,v2) = (f v1, v2)
let groupList keyF valF = 
    List.toSeq >> Seq.groupBy keyF >> Seq.map (apply2nd (Seq.map valF >> Seq.toList)) >> Seq.toList
    
let sampleDistribution xs = 
    let count = List.length xs
    let mean = List.sum xs / float count
    let var = List.sumBy (fun x -> (x-mean)**2.0) xs / (float count - 1.0)
    { Count = count; Mean = mean; Variance = var; }

let latexstderr distr =  "$" + EmnExtensions.MathHelpers.Statistics.GetFormatted(distr.Mean, distr.StdErr, -0.4).Replace("~", @"\pm") + "$"

let sampleCorrelation listA listB =
    let distrA = sampleDistribution listA
    let distrB = sampleDistribution listB
    assert (distrA.Count = distrB.Count)
    (List.zip listA listB
        |> List.sumBy (fun (a,b) -> (a - distrA.Mean) * (b - distrB.Mean))) / (float distrA.Count - 1.0) / Math.Sqrt(distrA.Variance * distrB.Variance)

let twoTailedPairedTtest xs ys = 
    let corrDistr = List.zip xs ys |>List.map (fun (x,y) -> x - y) |> sampleDistribution
    let t = corrDistr.Mean / corrDistr.StdErr
    let p = alglib.studenttdistr.studenttdistribution(corrDistr.Count - 1, -Math.Abs(t)) * 2.0
    (corrDistr.Mean < 0.0, p)


let shuffle seq =
    let arr = Seq.toArray seq
    EmnExtensions.Algorithms.ShuffleAlgorithm.Shuffle arr
    arr

let inline getMaybe (dict:System.Collections.Generic.Dictionary<'a,'b>) key =
    let value = ref Unchecked.defaultof<'b>
    if dict.TryGetValue(key, value) then
        Some(value.Value)
    else 
        None
