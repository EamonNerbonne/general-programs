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
let Lgm1 = makeLvqSettings LvqModelType.Lgm 1 0.
let Lgm5 = makeLvqSettings LvqModelType.Lgm 5 0.

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

let lrsGgm5_6 = lrsChecker (logscale 60 (0.02,0.3)) (fun lrP ->  Ggm5 0.32685526 lrP 0.05413183669)
let lrsGm5_5b = lrsChecker (logscale 40 (0.2, 1.0)) (fun lrp -> Gm5 lrp 0.02)


let lrsGgm5_7 = lrsChecker (logscale 50 (0.1,1.0)) (fun lrB ->  Ggm5 lrB 0.05243711009 0.05413183669)
let lrsGm5_6 = lrsChecker (logscale 30 (0.003, 0.02)) (fun lr0 -> Gm5 0.9236708572 lr0)

let lrsGm5_7 = lrsChecker (logscale 40 (0.3, 3.0)) (fun lrp -> Gm5 lrp 0.01110028628)
let lrsGgm5_8 = lrsChecker (logscale 50 (0.01,0.1)) (fun lr0 ->  Ggm5 1.0 0.05243711009 lr0)

let lrsGgm5_9a = lrsChecker (logscale 50 (0.02,0.2)) (fun lrP ->  Ggm5 1.0 lrP 0.0429193426)
let lrsGgm5_9b = lrsChecker (logscale 50 (0.3,3.0)) (fun lrB ->  Ggm5 lrB 0.05243711009 0.0429193426)

let lrsGgm5_10 = lrsChecker (logscale 10 (0.03,0.06)) (fun lr0 ->  Ggm5 2.730894534 0.02413585281 lr0)

let lrsGgm5_10a = lrsChecker (logscale 50 (0.01,0.1)) (fun lrP ->  Ggm5 2.730894534 lrP 0.04409203477)
let lrsGgm5_10b = lrsChecker (logscale 50 (1.0,9.0)) (fun lrB ->  Ggm5 lrB 0.02413585281 0.04409203477)

let lrsGgm5_11a = lrsChecker (logscale 50 (0.01,0.1)) (fun lrP ->  Ggm5 4.909932517 lrP 0.04409203477)
let lrsGgm5_11b = lrsChecker (logscale 50 (1.0,9.0)) (fun lrB ->  Ggm5 lrB 0.03806885289 0.04409203477)
let lrsGgm5_11c = lrsChecker (logscale 50 (0.01,0.1)) (fun lr0 ->  Ggm5 4.909932517 0.03806885289 lr0)

testSettings (Ggm5 5.151758465 0.04770197608 0.03084017782)

let lrsGgm5_12a = lrsChecker (logscale 50 (0.005,0.1)) (fun lrP ->  Ggm5 5.151758465 lrP 0.03084017782)
//let lrsGgm5_12b = lrsChecker (logscale 50 (1.0,9.0)) (fun lrB ->  Ggm5 lrB 0.04770197608 0.03084017782)
let lrsGgm5_12c = lrsChecker (logscale 50 (0.005,0.05)) (fun lr0 ->  Ggm5 5.151758465 0.04770197608 lr0)

testSettings (Ggm5 5.151758465 0.05351299581 0.03422167947)


let lrsG2m5_1a = lrsChecker (logscale 100 (0.003,0.3)) (fun lrP ->  G2m5 0.003 lrP 0.02)
let lrsG2m5_1b = lrsChecker (logscale 100 (0.0001,0.1)) (fun lrB ->  G2m5 lrB 0.03 0.02)
let lrsG2m5_1c = lrsChecker (logscale 100 (0.001,0.1)) (fun lr0 ->  G2m5 0.003 0.03 lr0)

testSettings (G2m5 0.004865286995 0.06450102881 0.02036034817)


let lrsG2m5_2a = lrsChecker (logscale 30 (0.02,0.2)) (fun lrP ->  G2m5 0.004865286995 lrP 0.02036034817)
let lrsG2m5_2b = lrsChecker (logscale 40 (0.001,0.02)) (fun lrB ->  G2m5 lrB 0.06450102881 0.02036034817)
let lrsG2m5_2c = lrsChecker (logscale 30 (0.005,0.08)) (fun lr0 ->  G2m5 0.004865286995 0.06450102881 lr0)


testSettings (G2m5 0.003391984072 0.06729917063 0.01864307978)

let lrsG2m5_3a = lrsChecker (logscale 25 (0.03,0.15)) (fun lrP ->  G2m5 0.003391984072 lrP 0.01864307978)
let lrsG2m5_3b = lrsChecker (logscale 35 (0.001,0.01)) (fun lrB ->  G2m5 lrB 0.06729917063 0.01864307978)
let lrsG2m5_3c = lrsChecker (logscale 20 (0.01,0.04)) (fun lr0 ->  G2m5 0.003391984072 0.06729917063 lr0)


testSettings (G2m5 0.00405022465 0.06614955977 0.01644100473)
//let lrsG2m5_4a = lrsChecker (logscale 30 (0.03,0.15)) (fun lrP ->  G2m5 0.00405022465 lrP 0.01644100473)
let lrsG2m5_4b = lrsChecker (logscale 40 (0.001,0.01)) (fun lrB ->  G2m5 lrB 0.06614955977 0.01644100473)
let lrsG2m5_4c = lrsChecker (logscale 30 (0.006,0.03)) (fun lr0 ->  G2m5 0.00405022465 0.06614955977 lr0)


testSettings (G2m5 0.005944046903 0.06614955977 0.02107393867)

let lrsG2m5_5a = lrsChecker (logscale 60 (0.04,0.1)) (fun lrP ->  G2m5 0.005944046903 lrP 0.02107393867)
let lrsG2m5_5b = lrsChecker (logscale 70 (0.003,0.012)) (fun lrB ->  G2m5 lrB 0.06614955977 0.02107393867)
let lrsG2m5_5c = lrsChecker (logscale 60 (0.01,0.04)) (fun lr0 ->  G2m5 0.005944046903 0.06614955977 lr0)

testSettings (G2m5 0.004919133927 0.07368224314 0.01397332419)

let lrsG2m5_6a = lrsChecker (logscale 40 (0.05,0.1)) (fun lrP ->  G2m5 0.004919133927 lrP 0.01397332419)
let lrsG2m5_6b = lrsChecker (logscale 40 (0.003,0.0075)) (fun lrB ->  G2m5 lrB 0.07368224314 0.01397332419)
let lrsG2m5_6c = lrsChecker (logscale 40 (0.01,0.03)) (fun lr0 ->  G2m5 0.004919133927 0.07368224314 lr0)

testSettings (G2m5 0.005233059919 0.0676296965 0.01450902498)

let lrsG2m5_7a = lrsChecker (logscale 30 (0.055,0.08)) (fun lrP ->  G2m5 0.005233059919 lrP 0.01450902498)
let lrsG2m5_7b = lrsChecker (logscale 30 (0.004,0.007)) (fun lrB ->  G2m5 lrB 0.0676296965 0.01450902498)
let lrsG2m5_7c = lrsChecker (logscale 40 (0.012,0.022)) (fun lr0 ->  G2m5 0.005233059919 0.0676296965 lr0)

testSettings (G2m5 0.005360131131 0.06698813151 0.01633390101)

let lrsLgm5_1a = lrsChecker (logscale 30 (0.001,1.0)) (fun lr0 ->  Lgm5 0.005233059919 lr0)
let lrsLgm5_1b = lrsChecker (logscale 30 (0.001,1.0)) (fun lrP ->  Lgm5 lrP 0.01450902498)


testSettings (Lgm5  0.428222218 0.08082231124)

let lrsLgm5_2a = lrsChecker (logscale 40 (0.01,1.0)) (fun lr0 ->  Lgm5 0.428222218 lr0)
let lrsLgm5_2b = lrsChecker (logscale 40 (0.03,3.0)) (fun lrP ->  Lgm5 lrP 0.08082231124)

testSettings (Lgm5  0.4020495836 0.01800782036)

let lrsLgm5_3a = lrsChecker (logscale 30 (0.002,0.05)) (fun lr0 ->  Lgm5 0.4020495836 lr0)
let lrsLgm5_3b = lrsChecker (logscale 30 (0.05,2.0)) (fun lrP ->  Lgm5 lrP 0.01800782036)

testSettings (Lgm5  0.656526238 0.008685645737)

