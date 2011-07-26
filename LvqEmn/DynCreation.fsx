#I @"ResultsAnalysis\bin\Release"
#r "ResultsAnalysis"
#r "LvqLibCli"
#r "LvqGui"

open LvqLibCli
open LvqGui
open System.Threading

let pagenR = ResultParsing.loadAllResults "page-blocks.data-10Dn-5,5473[0]^10"
let bestRes = 
    pagenR
    |> List.filter (fun res -> res.GetLrs() |> Seq.isEmpty |> not)
    |> List.sortBy (fun res -> (res.GetLrs() |> Seq.min).Errors)
    |> List.head
    

let nullable (x:'a) = new System.Nullable<'a>(x)

let dataset = CreateDataset.CreateFromShorthand(bestRes.resultsFile.Directory.Name)
let itersPerEpoch = LvqMultiModel.GetItersPerEpoch(dataset);
let model = new LvqMultiModel(dataset, bestRes.GetOptimizedSettings(nullable 1u, nullable 0u))

model.TrainUpto( 30.*1000.*1000. / itersPerEpoch + 0.5 |> int32, dataset, CancellationToken.None )

LoadDatasetImpl.ParseSettings(bestRes.resultsFile.Directory.Name)

//pagenR
//    |> List.map (fun res -> (res.GetLrs() |> Seq.sortBy (fun lrE -> lrE.Errors.ErrorMean) |> Seq.toList, res.unoptimizedSettings))
//    |> List.filter (fst >> List.isEmpty >> not)
//    |> List.map (Utils.apply1st List.head >> Utils.apply2nd (fun settings -> settings.ToShorthand()))
//    |> List.sortBy fst
//    |> Seq.take 20 |> Seq.toList

