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
    [ for lr0 in lr0range ->  Task.Factory.StartNew ((fun () -> lr0 |> settingsFactory |> testSettings 2 rndSeed), TaskCreationOptions.LongRunning) ]
    |> Array.ofList
    |> Array.map (fun task -> task.Result)
    |> Array.sortBy (fun res -> res.GeoMean)


type ControllerState = { Unpacker: LvqModelSettingsCli -> float; Packer: LvqModelSettingsCli -> float -> LvqModelSettingsCli; DegradedCount: int; LrLogDevScale: float }
let lrBcontrol = { 
        Unpacker = (fun settings-> settings.LrScaleB)
        Packer = fun (settings:LvqModelSettingsCli) lrB -> settings.WithLrChanges(settings.LR0, settings.LrScaleP, lrB)
        DegradedCount = 0
        LrLogDevScale = 1.
    }
let lrPcontrol = {
        Unpacker = fun settings -> settings.LrScaleP
        Packer = fun settings lrP -> settings.WithLrChanges(settings.LR0, lrP, settings.LrScaleB)
        DegradedCount = 0
        LrLogDevScale = 1.
    }
let lr0control = {
        Unpacker = fun settings -> settings.LR0
        Packer = fun settings lr0 -> settings.WithLrChanges(lr0, settings.LrScaleP, settings.LrScaleB)
        DegradedCount = 0
        LrLogDevScale = 1.
    }

let improveLr (testResultList:TestResults list) (lrUnpack, lrPack) =
    let errMargin = 0.0000001
    let unpackLogErrs testResults = testResults.Results |> Seq.concat |> Seq.concat |> List.ofSeq |> List.map (fun err -> Math.Log (err + errMargin))
    let bestToWorst = testResultList |> List.sortBy (unpackLogErrs >> List.average)
    let bestLogErrs = List.head bestToWorst |> unpackLogErrs
           

    //extract list of error rates from each testresult
    let logLrs = testResultList |> List.map (fun res-> lrUnpack res.Settings |> Math.Log)
    let relevance = List.Cons (1.0, bestToWorst |> List.tail |> List.map (unpackLogErrs >> Utils.twoTailedPairedTtest bestLogErrs >> snd))
    //printfn "%A" (bestToWorst |> List.map (fun res->lrUnpack res.Settings) |> List.zip relevance)
    let logLrDistr = List.zip logLrs relevance |> List.fold (fun (ss:SmartSum) (lr, rel) -> ss.CombineWith lr rel) (new SmartSum())
    let (logLrmean, logLrdev) = (logLrDistr.Mean, Math.Sqrt logLrDistr.Variance)
    (Math.Exp logLrmean, logLrdev)

let improvementStep (controller:ControllerState) (initialSettings:LvqModelSettingsCli) =
    let currSeed = rnd.NextUInt32 ()
    let initResults = testSettings 10 currSeed initialSettings
    let baseLr = controller.Unpacker initialSettings
    let lowLr = baseLr * Math.Exp(-2. * controller.LrLogDevScale)
    let highLr = baseLr * Math.Exp(2. * controller.LrLogDevScale)
    let results = lrsChecker (currSeed+2u) (logscale 40 (lowLr,highLr)) (controller.Packer initialSettings)
    let (newBaseLr, newLrLogDevScale) = improveLr (List.ofArray results) (controller.Unpacker, controller.Packer)
    let logLrDiff_LrDevScale = Math.Abs(Math.Log(baseLr / newBaseLr))
    let effNewLrDevScale = 0.3*newLrLogDevScale + 0.4*controller.LrLogDevScale + 0.4*logLrDiff_LrDevScale
    let newSettings = controller.Packer initialSettings newBaseLr
    let finalResults =  testSettings 10 currSeed newSettings
    printfn "   [%f..%f]: %f -> %f: %f -> %f"  lowLr highLr baseLr newBaseLr initResults.GeoMean finalResults.GeoMean
    if finalResults.GeoMean > initResults.GeoMean then
        ({ Unpacker = controller.Unpacker; Packer = controller.Packer; DegradedCount = controller.DegradedCount + 1; LrLogDevScale = controller.LrLogDevScale }, initialSettings)
    else
        ({ Unpacker = controller.Unpacker; Packer = controller.Packer; DegradedCount = controller.DegradedCount; LrLogDevScale = effNewLrDevScale }, newSettings)

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

let improveAndTest (initialSettings:LvqModelSettingsCli) =
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


[ "Gm+,1,!lr00.002,lrP0.7,"; "Gm+,5,NGi+,!lr00.003,lrP5.0,";  "G2m+,1,!lr00.01,lrP0.2,lrB0.003,"; "G2m+,5,NGi+,!lr00.01,lrP0.1,lrB0.004,"; "Ggm+,1,!lr00.03,lrP0.05,lrB2.0,"; "Ggm+,5,NGi+,!lr00.04,lrP0.05,lrB10.0,"]
    |> List.map CreateLvqModelValues.ParseShorthand |> List.map improveAndTest

[ "Gm+,1,lr00.002,lrP0.7,"; "Gm+,5,NGi+,lr00.003,lrP5.0,";  "G2m+,1,lr00.01,lrP0.2,lrB0.003,"; "G2m+,5,NGi+,lr00.01,lrP0.1,lrB0.004,"; "Ggm+,1,lr00.03,lrP0.05,lrB2.0,"; "Ggm+,5,NGi+,lr00.04,lrP0.05,lrB10.0,"]
    |> List.map CreateLvqModelValues.ParseShorthand |> List.map improveAndTest

[ "G2m+,1,Bi+,!lr00.01,lrP0.2,lrB0.003,"; "G2m+,5,NGi+,Bi+,!lr00.01,lrP0.1,lrB0.004,";  "Ggm+,1,Bi+,!lr00.03,lrP0.05,lrB2.0,"; "Ggm+,5,NGi+,Bi+,!lr00.04,lrP0.05,lrB10.0,"]
    |> List.map CreateLvqModelValues.ParseShorthand |> List.map improveAndTest

[ "Gm+,5,lr00.003,lrP5.0,"; "G2m+,5,lr00.01,lrP0.1,lrB0.004,"; "Ggm+,5,lr00.04,lrP0.05,lrB10.0,"]
     |> List.map CreateLvqModelValues.ParseShorthand |> List.map improveAndTest


//old manually found generally optimal lrs.
//G2m+,5,NGi+,!lr00.01633390101,lrP0.06698813151,lrB0.005360131131, GeoMean: 0.112609; Training: 0.107573 ~ 0.005773; Test: 0.117510 ~ 0.006234; NN: 0.157420 ~ 0.008930
//Ggm+,5,NGi+,!lr00.03422167947,lrP0.05351299581,lrB5.151758465, GeoMean: 0.108734; Training: 0.099645 ~ 0.005651; Test: 0.115992 ~ 0.006615; NN: 0.156203 ~ 0.008947
//Lgm[2],5,NGi+,!lr00.008685645737,lrP0.656526238, GeoMean: 0.013450; Training: 0.013415 ~ 0.001996; Test: 0.025856 ~ 0.002333

//opt results found with slightly buggy lr-searching code:
//Gm+,1,!lr00.002198585515,lrP0.6836046038, GeoMean: 0.198965; Training: 0.231766 ~ 0.016449; Test: 0.235295 ~ 0.016556; NN: 0.231495 ~ 0.012812
//Gm+,5,NGi+,!lr00.002672680891,lrP4.536289905, GeoMean: 0.146610; Training: 0.139755 ~ 0.005796; Test: 0.150053 ~ 0.006342; NN: 0.191839 ~ 0.009377
//G2m+,1,!lr00.021797623944739782,lrP0.17013535127904061,lrB0.0028710442546792839, GeoMean: 0.132753; Training: 0.153857 ~ 0.013621; Test: 0.161181 ~ 0.013461; NN: 0.183951 ~ 0.013869
//G2m+,5,NGi+,!lr00.014854479268703827,lrP0.12643192802795739,lrB0.003687418675856426, GeoMean: 0.110085; Training: 0.107585 ~ 0.005728; Test: 0.115503 ~ 0.006225; NN: 0.152425 ~ 0.009088
//Ggm+,1,!lr00.029892794513821885,lrP0.054767623178213938,lrB2.3443026990433924, GeoMean: 0.130877; Training: 0.147363 ~ 0.013599; Test: 0.159768 ~ 0.013463; NN: 0.184574 ~ 0.013776
//Ggm+,5,NGi+,!lr00.041993068719849549,lrP0.05551136786774067,lrB11.462570954856234, GeoMean: 0.109760; Training: 0.100846 ~ 0.005662; Test: 0.115616 ~ 0.006224; NN: 0.156829 ~ 0.008567

//Gm+,1,lr00.0015362340577901401,lrP10.716927113263273, GeoMean: 0.204712; Training: 0.254759 ~ 0.023125; Test: 0.258960 ~ 0.023035; NN: 0.238742 ~ 0.015294
//Gm+,5,NGi+,lr00.0010506456510214184,lrP10.86820020351132, GeoMean: 0.145488; Training: 0.139510 ~ 0.005856; Test: 0.146856 ~ 0.006128; NN: 0.189431 ~ 0.009042
//G2m+,1,lr00.011351487563176185,lrP0.37880915860796677,lrB0.019197822041416398, GeoMean: 0.136813; Training: 0.176091 ~ 0.019647; Test: 0.183255 ~ 0.019495; NN: 0.178905 ~ 0.013180
//G2m+,5,NGi+,!lr00.008485565595514527,lrP0.20435996513932222,lrB0.0080282005308666866, GeoMean: 0.112069; Training: 0.107313 ~ 0.005653; Test: 0.115319 ~ 0.006189; NN: 0.157401 ~ 0.008932
//Ggm+,1,lr00.026198578230780471,lrP0.13652588690969647,lrB1.2496647995734971, GeoMean: 0.136166; Training: 0.156286 ~ 0.015549; Test: 0.166200 ~ 0.015428; NN: 0.189529 ~ 0.013612
