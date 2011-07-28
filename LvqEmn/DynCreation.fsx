#I @"ResultsAnalysis\bin\ReleaseMingw2"
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
        [ for (_, _, settings) in shuffle (bestModelsForDataset (dirname, dataset)) ->
            async
                {
                    if LvqStatPlotsContainer.AnnouncePlotGeneration(dataset, settings, iters) then
                        printfn "Starting %s / %s" dataset.DatasetLabel (settings.ToShorthand())
                        let model = new LvqMultiModel(dataset, settings)
                        model.TrainUptoIters(float iters, dataset, CancellationToken.None)
                        saveGraphs model dataset
                    else
                        printfn "Already done %s / %s" dataset.DatasetLabel (settings.ToShorthand())
                }
        ]
    |> Async.RunSynchronously |> ignore
