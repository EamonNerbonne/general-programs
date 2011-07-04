// Learn more about F# at http://fsharp.net

open System.IO;
open System;
open LvqGui;

//---------------------------------------------------CONSTANTS

let filepattern = "*.txt"

//---------------------------------------------------BASICS

let rec flipList lstOfLists = 
    let lstOfNonemptyLists = List.filter (List.isEmpty >> not) lstOfLists
    if List.isEmpty lstOfNonemptyLists then []
    else List.map List.head lstOfNonemptyLists :: (List.map List.tail lstOfNonemptyLists |> flipList)
    
let apply2nd f (v1,v2) = (v1,f v2)
let groupList keyF valF = 
    List.toSeq >> Seq.groupBy keyF >> Seq.map (apply2nd (Seq.map valF >> Seq.toList)) >> Seq.toList
    
let countMeanVar xs = 
    let count = float(List.length xs)
    let mean = List.sum xs / count
    let var = List.sumBy (fun x -> (x-mean)**2.0) xs / (count - 1.0)
    (count, mean, var)

let meanStderr xs = 
    let (count, mean, var) = countMeanVar xs
    (mean, Math.Sqrt( var / count))

let latexstderr (mean,stderr) = EmnExtensions.MathHelpers.Statistics.GetFormatted(mean,stderr).Replace("~",@"\pm")

let sampleCorrelation listA listB =
    let (countA, meanA, varA) = countMeanVar listA
    let (countB, meanB, varB) = countMeanVar listB
    assert (countA = countB)
    (List.zip listA listB
        |> List.sumBy (fun (a,b) -> (a - meanA) * (b - meanB))) / (float countA - 1.0) / Math.Sqrt(varA * varB)

//---------------------------------------------------PARSING

let loadResultsBySettings fileprefix = 
    TestLr.resultsDir.GetFiles(filepattern)
    |> Array.toList
    |> List.map LvqGui.DatasetResults.ProcFile
