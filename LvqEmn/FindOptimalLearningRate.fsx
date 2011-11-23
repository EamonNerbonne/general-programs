#I @"ResultsAnalysis\bin\ReleaseMingw"
#r "ResultsAnalysis"
#r "LvqLibCli"
#r "LvqGui"
#r "EmnExtensions"
#time "on"

open LvqLibCli
open LvqGui
open System.Threading
open System

let datasets = 
    [
        for datasetFactory in CreateDataset.StandardDatasets() do
            datasetFactory.NormalizeDimensions <- true
            yield datasetFactory.CreateDataset()
    ]
           
let makeLvqSettings modelType prototypes lrB lrP lr0 = 
    let mutable tmp = new LvqModelSettingsCli()
    tmp.NgInitializeProtos <- true
    tmp.SlowStartLrBad <- true
    tmp.RandomInitialProjection <- true
    tmp.ModelType <- modelType
    tmp.PrototypesPerClass <- prototypes
    tmp.LR0 <- lr0
    tmp.LrScaleB <- lrP
    tmp.LrScaleP <- lrB
    tmp
    

let Ggm1 = makeLvqSettings LvqModelType.Ggm 1
let Ggm5 = makeLvqSettings LvqModelType.Ggm 5
let G2m1 = makeLvqSettings LvqModelType.G2m 1
let G2m5 = makeLvqSettings LvqModelType.G2m 5
let Gm1 = makeLvqSettings LvqModelType.Gm 1 0.
let Gm5 = makeLvqSettings LvqModelType.Gm 5 0.

type TestResults = { GeoMean:float; Mean:float;  Results:TestLr.ErrorRates list; Settings:LvqModelSettingsCli;}

let iterCount = 1e7

let testSettings settings =
    let results =
        [
            for dataset in datasets do
                let model = new LvqMultiModel(dataset,settings,false)
                model.TrainUptoIters(iterCount,dataset, CancellationToken.None)
                yield model.CurrentErrorRates(dataset)
        ]
    let averageErr= results|> List.averageBy (fun res->res.CanonicalError)
    let geomAverageErr= results|> List.averageBy  (fun res-> Math.Log res.CanonicalError) |> Math.Exp
    { GeoMean = geomAverageErr; Mean = averageErr;Settings = settings; Results = results}

let lrs = 
    [
        for x in [0..100] ->
            async 
                {
                    let lr0 = float x * 0.001
                    return (Gm5 0.01 lr0 |> testSettings)
                }
    ]
    |> Async.Parallel 
    |> Async.RunSynchronously
    |> Array.sortBy (fun res -> res.GeoMean)
