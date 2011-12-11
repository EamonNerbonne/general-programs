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
open Utils

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

type TestResults = { GeoMean:float; Mean:float;  Results:float list list []; Settings:LvqModelSettingsCli;}

let iterCount = 1e7

let testSettings settings =
    let results =
        [
            for dataset in datasets ->
                async{
                    let model = new LvqMultiModel(dataset,settings,false)
                    model.TrainUptoIters(iterCount,dataset, CancellationToken.None)
                    let errs = 
                        model.EvaluateFullStats() 
                        |> Seq.map (fun stat-> 
                            [
                                yield stat.values.[LvqTrainingStatCli.TrainingErrorI]
                                yield stat.values.[LvqTrainingStatCli.TestErrorI]
                                if model.nnErrIdx > 0 then
                                    yield stat.values.[model.nnErrIdx]    
                            ]
                        ) |> List.ofSeq
                    return errs
                }
        ] |> Async.Parallel |> Async.RunSynchronously

    let averageErr = results |> Array.averageBy (List.averageBy List.average)
    let geomAverageErr= results  |> Array.averageBy (List.averageBy List.average >> Math.Log) |> Math.Exp
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

let lrsGm5_6 = lrsChecker (logscale 30 (0.003, 0.02)) (fun lr0 -> Gm5 0.9236708572 lr0)
let lrsGm5_7 = lrsChecker (logscale 40 (0.3, 3.0)) (fun lrp -> Gm5 lrp 0.01110028628)

let lrsGgm5_12a = lrsChecker (logscale 50 (0.005,0.1)) (fun lrP ->  Ggm5 5.151758465 lrP 0.03084017782)
//let lrsGgm5_12b = lrsChecker (logscale 50 (1.0,9.0)) (fun lrB ->  Ggm5 lrB 0.04770197608 0.03084017782)
let lrsGgm5_12c = lrsChecker (logscale 50 (0.005,0.05)) (fun lr0 ->  Ggm5 5.151758465 0.04770197608 lr0)

testSettings (Ggm5 5.151758465 0.05351299581 0.03422167947)

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
testSettings (G2m5 0.005360131131 0.06698813151 0.01633390101)

let lrsG2m1 = lrsChecker (logscale 10 (0.004,0.02)) (fun lrB ->  G2m1 lrB 0.06698813151 0.01633390101)

let improveLr (testResultList:TestResults list) (lrUnpack, lrPack) =
    let errMargin = 0.0000001
    let unpackLogErrs testResults = testResults.Results |> Seq.concat |> Seq.concat |> List.ofSeq |> List.map (fun err -> Math.Log (err + errMargin))
    let bestToWorst = testResultList |> List.sortBy (unpackLogErrs >> List.average)
    let bestLogErrs = List.head bestToWorst |> unpackLogErrs
           

    //extract list of error rates from each testresult
    let logLrs = testResultList |> List.map (fun res-> lrUnpack res.Settings |> Math.Log)
    let relevance = List.Cons (1.0, bestToWorst |> List.tail |> List.map (unpackLogErrs >> Utils.unequalVarianceTtest bestLogErrs >> snd))
    let logLrDistr = List.zip logLrs relevance |> List.fold (fun (ss:SmartSum) (lr, rel) -> ss.CombineWith lr rel) (new SmartSum())
    let (logLrmean,logLrdev) = (logLrDistr.Mean, Math.Sqrt logLrDistr.Variance)
    (Math.Exp logLrmean, Math.Exp logLrdev)

type ControllerState = { Unpacker: LvqModelSettingsCli -> float; Packer: LvqModelSettingsCli -> float -> LvqModelSettingsCli; DegradedCount: int; LrDevScale: float }
let lrBcontrol = { 
        Unpacker = (fun settings-> settings.LrScaleB)
        Packer = fun (settings:LvqModelSettingsCli) lrB -> settings.WithLrChanges(settings.LR0, settings.LrScaleP, lrB)
        DegradedCount = 0
        LrDevScale = 3.
    }
let lrPcontrol = {
        Unpacker = fun settings -> settings.LrScaleP
        Packer = fun settings lrP -> settings.WithLrChanges(settings.LR0, lrP, settings.LrScaleB)
        DegradedCount = 0
        LrDevScale = 3.
    }
let lr0control = {
        Unpacker = fun settings -> settings.LR0
        Packer = fun settings lr0 -> settings.WithLrChanges(lr0, settings.LrScaleP, settings.LrScaleB)
        DegradedCount = 0
        LrDevScale = 3.
    }

let improvementStep (controller:ControllerState) (initialSettings:LvqModelSettingsCli) =
    let initResults = testSettings initialSettings
    let baseLr = controller.Unpacker initialSettings
    let lowLr = baseLr / (controller.LrDevScale ** 3.)
    let highLr = baseLr * (controller.LrDevScale ** 3.)
    let results = lrsChecker (logscale 30 (lowLr,highLr)) (controller.Packer initialSettings)
    let (newBaseLr, newLrDevScale) = improveLr (List.ofArray results) (controller.Unpacker, controller.Packer)
    let newSettings = controller.Packer initialSettings newBaseLr
    let finalResults =  testSettings newSettings
    let degradedCount = controller.DegradedCount + (if finalResults.GeoMean > initResults.GeoMean then 3 else -1)
    let newState = { Unpacker = controller.Unpacker; Packer = controller.Packer; DegradedCount = degradedCount; LrDevScale = newLrDevScale }
    (newState, newSettings)

let improvementSteps (controllers:ControllerState list) (initialSettings:LvqModelSettingsCli) =
    List.fold (fun (controllerStates, settings) nextController ->
            let (newControllerState, newSettings) = improvementStep nextController settings
            (newControllerState :: controllerStates, newSettings)
        ) ([], initialSettings) controllers

let rec fullyImprove (controllers:ControllerState list) (initialSettings:LvqModelSettingsCli) =
    if (controllers |> List.forall (fun controllerState -> controllerState.DegradedCount > 5)) then
        (initialSettings, controllers)
    else
        let (nextControllers, nextSettings) = improvementSteps controllers initialSettings
        fullyImprove nextControllers nextSettings

let optimizedGm1 = fullyImprove [lrPcontrol; lr0control] (Gm1 0.1 0.01)