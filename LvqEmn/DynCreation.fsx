#I @"ResultsAnalysis\bin\Release"
#r "ResultsAnalysis"
#r "LvqLibCli"
#r "LvqGui"
#time "on"

open LvqLibCli
open LvqGui
open System.Threading

let pagenR = ResultParsing.loadAllResults "page-blocks.data-10Dn-5,5473[0]^10"
let bestRes = 
    pagenR
    |> List.filter (fun res -> res.GetLrs() |> Seq.isEmpty |> not)
    |> List.sortBy (fun res -> (res.GetLrs() |> Seq.min).Errors)
    |> List.head
    

let dataset = CreateDataset.CreateFromShorthand(bestRes.resultsFile.Directory.Name)
let model = new LvqMultiModel(dataset, bestRes.GetOptimizedSettings(Utils.nullable 1u, Utils.nullable 0u))

model.TrainUptoIters(30000000. , dataset, CancellationToken.None)

model.CurrentStatsString(dataset)
model.CurrentFullStatsString(dataset)
bestRes.GetOptimizedSettings(Utils.nullable 1u, Utils.nullable 0u).ToShorthand()
new TestLr.ErrorRates(model.EvaluateFullStats(dataset) |> Seq.take 3 |> LvqMultiModel.MeanStdErrStats, model.nnErrIdx)

//pagenR
//    |> List.map (fun res -> (res.GetLrs() |> Seq.sortBy (fun lrE -> lrE.Errors.ErrorMean) |> Seq.toList, res.unoptimizedSettings))
//    |> List.filter (fst >> List.isEmpty >> not)
//    |> List.map (Utils.apply1st List.head >> Utils.apply2nd (fun settings -> settings.ToShorthand()))
//    |> List.sortBy fst
//    |> Seq.take 20 |> Seq.toList

