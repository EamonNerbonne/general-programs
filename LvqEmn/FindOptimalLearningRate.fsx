﻿#I @"ResultsAnalysis\bin\ReleaseMingw"
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
open System.Threading.Tasks

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

type TestResults = { GeoMean:float; Mean:float;  Results:float list list []; Settings:LvqModelSettingsCli; }

let printResults results =
    let trainDistr = results.Results |> List.concat |> List.map  (fun es -> List.nth es 0) |> Utils.sampleDistribution
    let testDistr = results.Results |> List.concat |> List.map  (fun es -> List.nth es 1) |> Utils.sampleDistribution
    let x = (results.Results.[0] |> List.head) |> List.length

    if x = 3 then 
        let nnDistr = results.Results |> List.concat |> List.map  (fun es -> List.nth es 2) |> Utils.sampleDistribution
        printfn "%s GeoMean: %f; Training: %f ~ %f; Test: %f ~ %f; NN: %f ~ %f" (results.Settings.ToShorthand ()) results.GeoMean trainDistr.Mean trainDistr.StdErr testDistr.Mean testDistr.StdErr nnDistr.Mean nnDistr.StdErr
    else 
        printfn "%s GeoMean: %f; Training: %f ~ %f; Test: %f ~ %f" (results.Settings.ToShorthand ()) results.GeoMean trainDistr.Mean trainDistr.StdErr testDistr.Mean testDistr.StdErr


    

let iterCount = 1e7

let rnd = new EmnExtensions.MathHelpers.MersenneTwister ()

let testSettings parOverride rndSeed (settings : LvqModelSettingsCli) =
    let results =
        [
            for dataset in datasets ->
                Task.Factory.StartNew( (fun () ->
                    let mutable parsettings = settings
                    parsettings.ParallelModels <- parOverride
                    parsettings.ParamsSeed <- 2u * rndSeed + 1u
                    parsettings.InstanceSeed <- 2u * rndSeed
                    
                    let model = new LvqMultiModel(dataset,parsettings,false)
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
                    errs
                ), TaskCreationOptions.LongRunning)
        ] |> List.map (fun task -> task.Result) |> List.toArray

    let averageErr = results |> Array.averageBy (List.averageBy List.average)
    let geomAverageErr= results  |> Array.averageBy (List.averageBy List.average >> Math.Log) |> Math.Exp
    { GeoMean = geomAverageErr; Mean = averageErr;Settings = settings; Results = results}



let logscale steps (v0, v1) = 
    let lnScale = Math.Log(v1 / v0)
    [ for i in [0..steps-1] -> v0 * Math.Exp(lnScale * (float i / (float steps - 1.))) ]

    //[0.001 -> 0.1]

let lrsChecker rndSeed lr0range settingsFactory = 
    [ for lr0 in lr0range ->  Task.Factory.StartNew ((fun () -> lr0 |> settingsFactory |> testSettings 3 rndSeed), TaskCreationOptions.LongRunning) ]
    |> Array.ofList
    |> Array.map (fun task -> task.Result)
    |> Array.sortBy (fun res -> res.GeoMean)


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

let improveLr (testResultList:TestResults list) (lrUnpack, lrPack) =
    let errMargin = 0.0000001
    let unpackLogErrs testResults = testResults.Results |> Seq.concat |> Seq.concat |> List.ofSeq |> List.map (fun err -> Math.Log (err + errMargin))
    let bestToWorst = testResultList |> List.sortBy (unpackLogErrs >> List.average)
    let bestLogErrs = List.head bestToWorst |> unpackLogErrs
           

    //extract list of error rates from each testresult
    let logLrs = testResultList |> List.map (fun res-> lrUnpack res.Settings |> Math.Log)
    let relevance = List.Cons (1.0, bestToWorst |> List.tail |> List.map (unpackLogErrs >> Utils.twoTailedPairedTtest bestLogErrs >> snd))
    printfn "%A" (bestToWorst |> List.map (fun res->lrUnpack res.Settings) |> List.zip relevance)
    let logLrDistr = List.zip logLrs relevance |> List.fold (fun (ss:SmartSum) (lr, rel) -> ss.CombineWith lr rel) (new SmartSum())
    let (logLrmean, logLrdev) = (logLrDistr.Mean, Math.Sqrt logLrDistr.Variance)
    (Math.Exp logLrmean, Math.Exp logLrdev)

let improvementStep (controller:ControllerState) (initialSettings:LvqModelSettingsCli) =
    let currSeed = rnd.NextUInt32 ()
    let initResults = testSettings 10 currSeed initialSettings
    let baseLr = controller.Unpacker initialSettings
    let lowLr = baseLr / (controller.LrDevScale ** 2.)
    let highLr = baseLr * (controller.LrDevScale ** 2.)
    let results = lrsChecker (currSeed+2u) (logscale 40 (lowLr,highLr)) (controller.Packer initialSettings)
    let (newBaseLr, newLrDevScale) = improveLr (List.ofArray results) (controller.Unpacker, controller.Packer)
    let logLrDiff_LrDevScale = Math.Abs(Math.Log(baseLr / newBaseLr))
    let effNewLrDevScale = 0.3*newLrDevScale + 0.4*controller.LrDevScale + 0.4*logLrDiff_LrDevScale
    let newSettings = controller.Packer initialSettings newBaseLr
    let finalResults =  testSettings 10 currSeed newSettings
    printfn "[%f..%f]: %f -> %f: %f -> %f"  lowLr highLr baseLr newBaseLr initResults.GeoMean finalResults.GeoMean
    if finalResults.GeoMean > initResults.GeoMean then
        ({ Unpacker = controller.Unpacker; Packer = controller.Packer; DegradedCount = controller.DegradedCount + 1; LrDevScale = controller.LrDevScale }, initialSettings)
    else
        ({ Unpacker = controller.Unpacker; Packer = controller.Packer; DegradedCount = controller.DegradedCount; LrDevScale = effNewLrDevScale }, newSettings)

let improvementSteps (controllers:ControllerState list) (initialSettings:LvqModelSettingsCli) =
    List.fold (fun (controllerStates, settings) nextController ->
            let (newControllerState, newSettings) = improvementStep nextController settings
            (newControllerState :: controllerStates, newSettings)
        ) ([], initialSettings) controllers
    |> apply1st List.rev

let rec fullyImprove (controllers:ControllerState list) (initialSettings:LvqModelSettingsCli) =
    if controllers |> List.sumBy (fun controllerState -> controllerState.DegradedCount) > 3 * (List.length controllers) then
        (initialSettings, controllers)
    else
        let (nextControllers, nextSettings) = improvementSteps controllers initialSettings
        fullyImprove nextControllers nextSettings

let rec improveAndTest (initialShorthand:string) =
    let initialSettings = CreateLvqModelValues.ParseShorthand initialShorthand
    let needsB = [LvqModelType.G2m; LvqModelType.Ggm ; LvqModelType.Gpq] |> List.exists (fun modelType -> initialSettings.ModelType = modelType)
    let controllers = 
        [
            if needsB then yield lrBcontrol
            yield lrPcontrol
            yield lr0control
       ]
    let improvedSettings = fullyImprove controllers initialSettings |> fst
    let testedResults = testSettings 10 1u improvedSettings //GeoMean: 0.1981672332 Mean: 0.2310214097
    printResults testedResults
    testedResults

//old manually found generally optimal lrs.
//testSettings 10 1u (G2m5 0.005360131131 0.06698813151 0.01633390101) |> printResults
//G2m+,5,NGi+,!lr00.01633390101,lrP0.06698813151,lrB0.005360131131, GeoMean: 0.112609; Training: 0.107573 ~ 0.005773; Test: 0.117510 ~ 0.006234; NN: 0.157420 ~ 0.008930
//testSettings 10 1u (Ggm5 5.151758465 0.05351299581 0.03422167947) |> printResults
//Ggm+,5,NGi+,!lr00.03422167947,lrP0.05351299581,lrB5.151758465, GeoMean: 0.108734; Training: 0.099645 ~ 0.005651; Test: 0.115992 ~ 0.006615; NN: 0.156203 ~ 0.008947
//testSettings 10 1u (Lgm5  0.656526238 0.008685645737) |> printResults
//Lgm[2],5,NGi+,!lr00.008685645737,lrP0.656526238, GeoMean: 0.013450; Training: 0.013415 ~ 0.001996; Test: 0.025856 ~ 0.002333



//let optimizedGm1a = fullyImprove [lrPcontrol; lr0control] (Gm1 1.0 0.001)  //Gm+,1,!lrP0.6836046038,lr00.002198585515,
//Gm+,1,!lr00.002198585515,lrP0.6836046038, GeoMean: 0.198965; Training: 0.231766 ~ 0.016449; Test: 0.235295 ~ 0.016556; NN: 0.231495 ~ 0.012812

//let optimizedGm5a = fullyImprove [lrPcontrol; lr0control] (Gm5 1.0 0.001) //Gm+,5,NGi+,!lrP4.536289905,lrB0.002672680891,
//Gm+,5,NGi+,!lr00.002672680891,lrP4.536289905, GeoMean: 0.146610; Training: 0.139755 ~ 0.005796; Test: 0.150053 ~ 0.006342; NN: 0.191839 ~ 0.009377

//let optimizedG2m1a = fullyImprove [lrBcontrol; lrPcontrol; lr0control] (G2m1 0.005 0.06 0.02)
//G2m+,1,!lr00.021797623944739782,lrP0.17013535127904061,lrB0.0028710442546792839, GeoMean: 0.132753; Training: 0.153857 ~ 0.013621; Test: 0.161181 ~ 0.013461; NN: 0.183951 ~ 0.013869

//let optimizedG2m5a = fullyImprove [lrBcontrol; lrPcontrol; lr0control] (G2m5 0.005 0.06 0.02) 
//G2m+,5,NGi+,!lr00.014854479268703827,lrP0.12643192802795739,lrB0.003687418675856426, GeoMean: 0.110085; Training: 0.107585 ~ 0.005728; Test: 0.115503 ~ 0.006225; NN: 0.152425 ~ 0.009088

//let optimizedGgm1a = fullyImprove [lrBcontrol; lrPcontrol; lr0control] (Ggm1 5.0 0.05 0.03) 
//Ggm+,1,!lr00.029892794513821885,lrP0.054767623178213938,lrB2.3443026990433924, GeoMean: 0.130877; Training: 0.147363 ~ 0.013599; Test: 0.159768 ~ 0.013463; NN: 0.184574 ~ 0.013776

//let optimizedGgm5a = fullyImprove [lrBcontrol; lrPcontrol; lr0control] (Ggm5 5.0 0.05 0.03) 
//Ggm+,5,NGi+,!lr00.041993068719849549,lrP0.05551136786774067,lrB11.462570954856234, GeoMean: 0.109760; Training: 0.100846 ~ 0.005662; Test: 0.115616 ~ 0.006224; NN: 0.156829 ~ 0.008567

//"Gm+,1,!lr00.002198585515,lrP0.6836046038," |> CreateLvqModelValues.ParseShorthand |>  testSettings 10 1u  |> printResults
//"Gm+,5,NGi+,!lr00.002672680891,lrP4.536289905," |> CreateLvqModelValues.ParseShorthand |>  testSettings 10 1u |> printResults//GeoMean: 0.1389671982 Mean: 0.1519136112
//"G2m+,1,!lr00.021797623944739782,lrP0.17013535127904061,lrB0.0028710442546792839," |> CreateLvqModelValues.ParseShorthand |> testSettings 10 1u  |> printResults  //geomean: 0.1325558145 mean: 0.1666461318
//"G2m+,5,NGi+,!lr00.014854479268703827,lrP0.12643192802795739,lrB0.003687418675856426," |> CreateLvqModelValues.ParseShorthand |> testSettings 10 1u |> printResults //GeoMean: 0.1112603019 Mean: 0.1257493826
//"Ggm+,1,NGi+,!lr00.029892794513821885,lrP0.054767623178213938,lrB2.3443026990433924," |> CreateLvqModelValues.ParseShorthand |> testSettings 10 1u  |> printResults  //GeoMean: 0.1298214422 Mean: 0.162846497
//"Ggm+,5,NGi+,!lr00.041993068719849549,lrP0.05551136786774067,lrB11.462570954856234," |> CreateLvqModelValues.ParseShorthand |> testSettings 10 1u |> printResults  //GeoMean: 0.1105839335 Mean: 0.124578113


improveAndTest "Gm+,1,lr00.002,lrP0.7,"
improveAndTest "Gm+,5,NGi+,lr00.003,lrP5.0,"
improveAndTest "G2m+,1,lr00.01,lrP0.2,lrB0.003,"
improveAndTest "G2m+,5,NGi+,!lr00.01,lrP0.1,lrB0.004,"
improveAndTest "Ggm+,1,lr00.03,lrP0.05,lrB2.0,"
improveAndTest "Ggm+,5,NGi+,lr00.04,lrP0.05,lrB10.0,"


