module Utils

open System
open System.Linq

//---------------------------------------------------BASICS
type SampleDistribution = { Mean:float; Count: int; Variance: float; } member this.StdErr = Math.Sqrt( this.Variance / float this.Count);

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
    if corrDistr.Count < 2 then
        (false, 1.0)
    else
        let t = corrDistr.Mean / corrDistr.StdErr
        let p = alglib.studenttdistr.studenttdistribution(corrDistr.Count - 1, -Math.Abs(t)) * 2.0
        (corrDistr.Mean < 0.0, p)

let twoTailedOneSampleTtest xs =
    let corrDistr = sampleDistribution xs
    if corrDistr.Count < 2 then
        (false, 1.0)
    else
        let t = corrDistr.Mean / corrDistr.StdErr
        let p = alglib.studenttdistr.studenttdistribution(corrDistr.Count - 1, -Math.Abs(t)) * 2.0
        (corrDistr.Mean < 0.0, p)


let unequalVarianceTtest xs ys = 
    let xDistr = sampleDistribution xs
    let yDistr = sampleDistribution ys
    let corrDistr = List.zip xs ys |>List.map (fun (x,y) -> x - y) |> sampleDistribution
    let xNVar = xDistr.Variance / float xDistr.Count
    let yNVar = yDistr.Variance / float yDistr.Count
    let scaledVar = xNVar + yNVar
    let t = (xDistr.Mean - yDistr.Mean) / Math.Sqrt scaledVar
    let sqr x = x * x
    let nu = sqr scaledVar / (sqr xNVar / (float xDistr.Count - 1.) + sqr yNVar / (float yDistr.Count - 1.) )

    let p = alglib.studenttdistr.studenttdistribution(int nu, -Math.Abs(t)) * 2.0
    (corrDistr.Mean < 0.0, p)


let welchTwoTailedPairedTtest xs ys = 
    let xArr = xs |> Array.ofList
    let yArr = ys |> Array.ofList

    let mutable tt = 0.
    let mutable tx = 0.
    let mutable ty = 0.

    alglib.unequalvariancettest(xArr, xArr.Length, yArr, yArr.Length, &tt, &tx,&ty)
    (tx<ty,tt)

let mannWhitneyUTest xs ys = 
    let xArr = xs |> Array.ofList
    let yArr = ys |> Array.ofList

    let mutable tt = 0.
    let mutable tx = 0.
    let mutable ty = 0.

    alglib.mannwhitneyutest(xArr, xArr.Length, yArr, yArr.Length, &tt, &tx,&ty)
    (tx<ty,tt)


let xs = [for i in 1..10 -> float i]
let ys = [for i in 1..10 -> 2.0 * float i - 3.0]
(*
#I @"bin\ReleaseMingw"
#r "ResultsAnalysis"
#r "EmnExtensions"
#time "on"

open EmnExtensions.Text
open EmnExtensions
open System.Collections.Generic
open System.Linq
open System
open Utils
*)


let shuffle seq =
    let arr = Seq.toArray seq
    EmnExtensions.Algorithms.ShuffleAlgorithm.Shuffle arr
    arr

let inline getMaybe (dict:System.Collections.Generic.IDictionary<'a,'b>) key =
    let value = ref Unchecked.defaultof<'b>
    if dict.TryGetValue(key, value) then
        Some(value.Value)
    else 
        None

let toDict keyf valf xs = (Seq.groupBy keyf xs).ToDictionary(fst, snd >> valf)
let orDefault defaultValue = 
    function
    | None -> defaultValue
    | Some(value) -> value
