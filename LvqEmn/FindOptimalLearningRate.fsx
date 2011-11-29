#I @"ResultsAnalysis\bin\ReleaseMingw"
#r "ResultsAnalysis"
#r "LvqLibCli"
#r "LvqGui"
#r "EmnExtensions"
#r "PresentationCore"
#r "WindowsBase"
#r "EmnExtensionsWpf"
#time "on"

open LvqLibCli
open LvqGui
open System.Threading
open System
open System.Windows.Media
open EmnExtensions.Wpf

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
    tmp.LrScaleB <- lrB
    tmp.LrScaleP <- lrP
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


let logscale steps (v0, v1) = 
    let lnScale = Math.Log(v1 / v0)
    [ for i in [0..steps-1] -> v0 * Math.Exp(lnScale * (float i / (float steps - 1.))) ]

    //[0.001 -> 0.1]

let lrsChecker lr0range settingsFactory = 
    [ for lr0 in lr0range -> async { return (lr0 |> settingsFactory |> testSettings) } ]
    |> Async.Parallel 
    |> Async.RunSynchronously
    |> Array.sortBy (fun res -> res.GeoMean)


let seeTopResults results =results |> (Seq.take 10 >> Seq.map (fun res->((res.GeoMean, res.Mean), (res.Settings.LR0, (res.Settings.LrScaleP, res.Settings.LrScaleB))))  >> List.ofSeq)

let lrsGm5 = lrsChecker (logscale 30 (0.001,0.1)) (Gm5 0.01)


let lrsGgm5 = lrsChecker (logscale 100 (0.001,0.1)) (Ggm5 0.01 0.1)

let lrsGgm5_1 = lrsChecker (logscale 30 (0.05,0.5)) (Ggm5 0.1 0.01)
let lrsGm5_1 = lrsChecker (logscale 15 (0.02,0.04)) (Gm5 0.01)

let lrsGgm5_2 = lrsChecker (logscale 15 (0.0005,0.5)) (fun lrp ->  Ggm5 0.01 lrp 0.1519597691)
let lrsGm5_2 = lrsChecker (logscale 15 (0.0005, 0.5)) (fun lrp -> Gm5 lrp 0.02438027308)


let lrsGgm5_3 = lrsChecker (logscale 15 (0.005,0.1)) (fun lrp ->  Ggm5 0.01 lrp 0.1519597691)
let lrsGm5_3 = lrsChecker (logscale 15 (0.1, 1.0)) (fun lrp -> Gm5 lrp 0.02438027308)

let lrsGgm5_4 = lrsChecker (logscale 200 (0.005,1.0)) (fun lrb ->  Ggm5 lrb 0.06518363449 0.1519597691)
let lrsGm5_4 = lrsChecker (logscale 100 (0.005, 0.1)) (fun lr0 -> Gm5 0.719685673 lr0)

let lrsGm5_5 = lrsChecker (logscale 30 (0.1, 1.0)) (fun lrp -> Gm5 lrp 0.009158078185)
let lrsGgm5_5 = lrsChecker (logscale 30 (0.05,0.5)) (fun lr0 ->  Ggm5 0.32685526 0.06518363449 lr0)

Ggm5 0.01 0.1 0.1519597691