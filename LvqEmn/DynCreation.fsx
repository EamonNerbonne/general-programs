#I @"ResultsAnalysis\bin\Release"
#r "ResultsAnalysis"
#r "LvqLibCli"
#r "LvqGui"
#r "EmnExtensions"
#time "on"

open LvqLibCli
open LvqGui
open System.Threading

let saveGraphs model dataset = 
    use statPlots = new LvqStatPlotsContainer(CancellationToken.None, true)
    statPlots.DisplayModel(dataset,model,model.GetBestSubModelIdx(dataset),StatisticsViewMode.CurrentOnly, true,true).Wait()
    statPlots.SaveAllGraphs(true).Wait()
    statPlots.ShowCurrentProjectionStats(StatisticsViewMode.CurrentAndMean).Wait()
    statPlots.SaveAllGraphs(false).Wait()
    statPlots.ShowCurrentProjectionStats(StatisticsViewMode.MeanAndStderr).Wait()
    statPlots.SaveAllGraphs(false).Wait()

let createConfirmationDataset resultsName = 
    let factory = CreateDataset.CreateFactory(resultsName)
    if factory <> null then
        factory.IncInstanceSeed()
        let dataset = factory.CreateDataset()
        if dataset <> null then 
            Some(dataset)
        else
            None
    else
        None


let datasets = 
    TestLr.resultsDir.GetDirectories() 
    |> Seq.map (fun dir -> (dir.Name, dir.Name |> createConfirmationDataset))
    |> Seq.filter (fun (_,dataset) -> dataset.IsSome)
    |> Seq.filter (fun (_,dataset) -> dataset.Value.TestSet = null)
    |> Seq.map (fun (dirname,dataset) -> (dirname,dataset.Value))
    |> Seq.toArray

let bestModelsForDataset (dirname:string, dataset:LvqDatasetCli) = 
    ResultParsing.loadAllResults dirname
    |> Seq.map  (fun res -> (dirname, dataset, res.GetOptimizedSettings(Utils.nullable 1u, Utils.nullable 0u)))

let shuffle seq =
    let arr = Seq.toArray seq
    EmnExtensions.Algorithms.ShuffleAlgorithm.Shuffle arr
    arr

let iters = 100000000L


for (dirname, dataset) in shuffle datasets do
    Async.Parallel 
        [ for (_, _, settings) in shuffle (bestModelsForDataset (dirname,dataset)) ->
            async
                {
                    if LvqStatPlotsContainer.AnnouncePlotGeneration(dataset, settings.ToShorthand(), iters) then
                        printfn "Starting %s / %s" dataset.DatasetLabel (settings.ToShorthand())
                        let model = new LvqMultiModel(dataset, settings)
                        model.TrainUptoIters(float iters, dataset, CancellationToken.None)
                        saveGraphs model dataset
                    else
                        printfn "Already done %s / %s" dataset.DatasetLabel (settings.ToShorthand())
                }
        ]
    |> Async.RunSynchronously |> ignore

let pagenR = ResultParsing.loadAllResults "star-8D-9x10000,3(5Dr)x10i0.8n7g5[a9cd2154,1]^10"
let bestRes = 
    pagenR
    |> List.filter (fun res -> res.GetLrs() |> Seq.isEmpty |> not)
    |> List.sortBy (fun res -> (res.GetLrs() |> Seq.min).Errors)
    |> List.head
    

let factory = CreateDataset.CreateFactory(bestRes.resultsFile.Directory.Name)
factory.IncInstanceSeed()
let dataset = factory.CreateDataset()
let model = new LvqMultiModel(dataset, bestRes.GetOptimizedSettings(Utils.nullable 1u, Utils.nullable 0u))

model.TrainUptoIters(3000000. , dataset, CancellationToken.None)

//model.CurrentStatsString(dataset)
//model.CurrentFullStatsString(dataset)
//bestRes.GetOptimizedSettings(Utils.nullable 1u, Utils.nullable 0u).ToShorthand()
//new TestLr.ErrorRates(model.EvaluateFullStats(dataset) |> Seq.take 3 |> LvqMultiModel.MeanStdErrStats, model.nnErrIdx)


saveGraphs model dataset
model.ModelLabel
//pagenR
//    |> List.map (fun res -> (res.GetLrs() |> Seq.sortBy (fun lrE -> lrE.Errors.ErrorMean) |> Seq.toList, res.unoptimizedSettings))
//    |> List.filter (fst >> List.isEmpty >> not)
//    |> List.map (Utils.apply1st List.head >> Utils.apply2nd (fun settings -> settings.ToShorthand()))
//    |> List.sortBy fst
//    |> Seq.take 20 |> Seq.toList

//float(System.GC.GetTotalMemory(false)) / 1024.0 / 1024.0