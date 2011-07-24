module Utils

open System

//---------------------------------------------------BASICS
let pass x f = f x 

let rec flipList lstOfLists = 
    let lstOfNonemptyLists = List.filter (List.isEmpty >> not) lstOfLists
    if List.isEmpty lstOfNonemptyLists then []
    else List.map List.head lstOfNonemptyLists :: (List.map List.tail lstOfNonemptyLists |> flipList)
    
let apply2nd f (v1,v2) = (v1,f v2)
let apply1st f (v1,v2) = (f v1, v2)
let groupList keyF valF = 
    List.toSeq >> Seq.groupBy keyF >> Seq.map (apply2nd (Seq.map valF >> Seq.toList)) >> Seq.toList
    
let countMeanVar xs = 
    let count = List.length xs
    let mean = List.sum xs / float count
    let var = List.sumBy (fun x -> (x-mean)**2.0) xs / (float count - 1.0)
    (count, mean, var)

let meanStderr xs = 
    let (count, mean, var) = countMeanVar xs
    (mean, Math.Sqrt( var / float count))

let latexstderr (mean,stderr) =  "$ "+EmnExtensions.MathHelpers.Statistics.GetFormatted(mean,stderr,-0.4).Replace("~",@"\pm")+"$"

let sampleCorrelation listA listB =
    let (countA, meanA, varA) = countMeanVar listA
    let (countB, meanB, varB) = countMeanVar listB
    assert (countA = countB)
    (List.zip listA listB
        |> List.sumBy (fun (a,b) -> (a - meanA) * (b - meanB))) / (float countA - 1.0) / Math.Sqrt(varA * varB)

let twoTailedPairedTtest xs ys = 
    let (count, mean, var) = List.zip xs ys |>List.map (fun (x,y) -> x - y) |> countMeanVar
    let t = mean / Math.Sqrt(var / float count)
    let p = alglib.studenttdistr.studenttdistribution(count - 1, -Math.Abs(t)) * 2.0
    (p, mean < 0.0)


