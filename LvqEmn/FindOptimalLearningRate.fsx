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
open EmnExtensions.Text
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
    tmp.NGi <- true
    tmp.SlowK <- true
    tmp.Ppca <- false
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
        sprintf "%s GeoMean: %f; Training: %f ~ %f; Test: %f ~ %f; NN: %f ~ %f" (results.Settings.ToShorthand ()) results.GeoMean trainDistr.Mean trainDistr.StdErr testDistr.Mean testDistr.StdErr nnDistr.Mean nnDistr.StdErr
    else 
        sprintf "%s GeoMean: %f; Training: %f ~ %f; Test: %f ~ %f" (results.Settings.ToShorthand ()) results.GeoMean trainDistr.Mean trainDistr.StdErr testDistr.Mean testDistr.StdErr


    

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

    let relLength = List.length relevance
    let linearlyScaledRelevance = List.init relLength (fun i -> float (relLength - i) / float relLength)

    let effRelevance = List.zip relevance linearlyScaledRelevance |> List.map (fun (a,b) -> a + b)
    
    //printfn "%A" (bestToWorst |> List.map (fun res->lrUnpack res.Settings) |> List.zip relevance)
    let logLrDistr = List.zip logLrs effRelevance |> List.fold (fun (ss:SmartSum) (lr, rel) -> ss.CombineWith lr rel) (new SmartSum ())
    let (logLrmean, logLrdev) = (logLrDistr.Mean, Math.Sqrt logLrDistr.Variance)
    (Math.Exp logLrmean, logLrdev)

let improvementStep (controller:ControllerState) (initialSettings:LvqModelSettingsCli) =
    let currSeed = rnd.NextUInt32 ()
    let initResults = testSettings 10 currSeed initialSettings
    let baseLr = controller.Unpacker initialSettings
    let lowLr = baseLr * Math.Exp(-Math.Sqrt(6.) * controller.LrLogDevScale)
    let highLr = baseLr * Math.Exp(Math.Sqrt(6.) * controller.LrLogDevScale)
    let results = lrsChecker (currSeed + 2u) (logscale 40 (lowLr,highLr)) (controller.Packer initialSettings)
    let (newBaseLr, newLrLogDevScale) = improveLr (List.ofArray results) (controller.Unpacker, controller.Packer)
    let logLrDiff_LrDevScale = Math.Abs(Math.Log(baseLr / newBaseLr))
    let effNewLrDevScale = 0.3*newLrLogDevScale + 0.4*controller.LrLogDevScale + 0.4*logLrDiff_LrDevScale
    let newSettings = controller.Packer initialSettings newBaseLr
    let finalResults =  testSettings 10 currSeed newSettings
    printfn "   [%f..%f]: %f -> %f: %f -> %f"  lowLr highLr baseLr newBaseLr initResults.GeoMean finalResults.GeoMean
    if finalResults.GeoMean > initResults.GeoMean then
        ({ Unpacker = controller.Unpacker; Packer = controller.Packer; DegradedCount = controller.DegradedCount + 1; LrLogDevScale = effNewLrDevScale }, newSettings)
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
    let testedResults = testSettings 100 1u improvedSettings
    let resultString = printResults testedResults
    Console.WriteLine resultString
    System.IO.File.AppendAllText (TestLr.resultsDir.FullName + "\\uniform-results.txt", resultString + "\n")
    testedResults

let resPath = TestLr.resultsDir.FullName + "\\uniform-results.txt"

let newRes=
    System.IO.File.ReadAllLines resPath 
        |> List.ofArray 
        |> List.map (fun s-> if s.Contains " " then  s.Substring(0, s.IndexOf " ") else s)
        |> List.filter (String.IsNullOrEmpty >> not)
        |> List.map CreateLvqModelValues.TryParseShorthand 
        |> List.filter (fun settings -> settings.HasValue)
        |> List.map (fun settings -> settings.Value)
        //|> List.map (fun settings -> settings.ToShorthand())
        |> List.map (testSettings 100 1u >> printResults >> (fun resline -> System.IO.File.AppendAllText (TestLr.resultsDir.FullName + "\\uniform-results2.txt", resline + "\n"); resline ))

[ "Gm+,1,!lr00.002,lrP0.7,"; "Gm+,5,NGi+,!lr00.003,lrP5.0,";  "G2m+,1,!lr00.01,lrP0.2,lrB0.003,"; "G2m+,5,NGi+,!lr00.01,lrP0.1,lrB0.004,"; "Ggm+,1,!lr00.03,lrP0.05,lrB2.0,"; "Ggm+,5,NGi+,!lr00.04,lrP0.05,lrB10.0,"]
    |> List.map CreateLvqModelValues.ParseShorthand |> List.map improveAndTest
[ "Gm+,1,lr00.002,lrP0.7,"; "Gm+,5,NGi+,lr00.003,lrP5.0,";  "G2m+,1,lr00.01,lrP0.2,lrB0.003,"; "G2m+,5,NGi+,lr00.01,lrP0.1,lrB0.004,"; "Ggm+,1,lr00.03,lrP0.05,lrB2.0,"; "Ggm+,5,NGi+,lr00.04,lrP0.05,lrB10.0,"]
    |> List.map CreateLvqModelValues.ParseShorthand |> List.map improveAndTest
[ "G2m+,1,Bi+,!lr00.01,lrP0.2,lrB0.003,"; "G2m+,5,NGi+,Bi+,!lr00.01,lrP0.1,lrB0.004,";  "Ggm+,1,Bi+,!lr00.03,lrP0.05,lrB2.0,"; "Ggm+,5,NGi+,Bi+,!lr00.04,lrP0.05,lrB10.0,"]
    |> List.map CreateLvqModelValues.ParseShorthand |> List.map improveAndTest
[ "Gm+,5,lr00.003,lrP5.0,"; "G2m+,5,lr00.01,lrP0.1,lrB0.004,"; "Ggm+,5,lr00.04,lrP0.05,lrB10.0,"]
     |> List.map CreateLvqModelValues.ParseShorthand |> List.map improveAndTest
[ "Gm+,5,!lr00.003,lrP5.0,"; "G2m+,5,!lr00.01,lrP0.1,lrB0.004,"; "Ggm+,5,!lr00.04,lrP0.05,lrB10.0,"]
     |> List.map CreateLvqModelValues.ParseShorthand |> List.map improveAndTest
[ "Gm+,1,rP,!lr00.002,lrP0.7,"; "Gm+,5,rP,NGi+,!lr00.003,lrP5.0,";  "G2m+,1,rP,!lr00.01,lrP0.2,lrB0.003,"; "G2m+,5,rP,NGi+,!lr00.01,lrP0.1,lrB0.004,"; "Ggm+,1,rP,!lr00.03,lrP0.05,lrB2.0,"; "Ggm+,5,rP,NGi+,!lr00.04,lrP0.05,lrB10.0,"]
    |> List.map CreateLvqModelValues.ParseShorthand |> List.map improveAndTest
[ "Gm+,1,rP,Pi+,!lr00.002,lrP0.7,"; "Gm+,5,rP,NGi+,Pi+,!lr00.003,lrP5.0,";  "G2m+,1,rP,Pi+,!lr00.01,lrP0.2,lrB0.003,"; "G2m+,5,rP,NGi+,Pi+,!lr00.01,lrP0.1,lrB0.004,"; "Ggm+,1,rP,Pi+,!lr00.03,lrP0.05,lrB2.0,"; "Ggm+,5,rP,NGi+,Pi+,!lr00.04,lrP0.05,lrB10.0,"]
    |> List.map CreateLvqModelValues.ParseShorthand |> List.map improveAndTest
[ "G2m+,1,rP,Bi+,!lr00.01,lrP0.2,lrB0.003,"; "G2m+,5,rP,NGi+,Bi+,!lr00.01,lrP0.1,lrB0.004,";  "Ggm+,1,rP,Bi+,!lr00.03,lrP0.05,lrB2.0,"; "Ggm+,5,rP,NGi+,Bi+,!lr00.04,lrP0.05,lrB10.0,"]
    |> List.map CreateLvqModelValues.ParseShorthand |> List.map improveAndTest
