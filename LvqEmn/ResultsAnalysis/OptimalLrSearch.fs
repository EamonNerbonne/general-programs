﻿module OptimalLrSearch
open System
open System.IO
open System.Threading
open System.Threading.Tasks
open EmnExtensions.Text

open LvqGui
open LvqLibCli
open Utils
open Microsoft.FSharp.Collections

let datasets = 
    [
        for datasetFactory in CreateDataset.StandardDatasets() do
            datasetFactory.NormalizeDimensions <- true
            datasetFactory.InstanceSeed <- 1000u

            let infiniteVariations = 
                Seq.initInfinite (fun i ->
                        datasetFactory.IncInstanceSeed()
                        datasetFactory.CreateDataset()
                    )
            yield LazyList.ofSeq infiniteVariations
    ]

type TestResults = { GeoMean:float; Results:float list list []; Settings:LvqModelSettingsCli; }
let extractGeoMeanDistrFromResults =  Array.toList >> Utils.flipList  >> List.map (List.concat >> List.averageBy Math.Log >> Math.Exp) >> Utils.sampleDistribution

type MeanTestResults = { GeoMean:float * float; Training: float * float; Test: float * float; NN: float * float; Settings:LvqModelSettingsCli; }


let toMeanResults (results:TestResults) = 
    let combineDistributions distributions = (distributions |> Array.averageBy (fun distr->distr.Mean), (distributions |> Array.sumBy (fun distr -> distr.StdErr *  distr.StdErr)) / float ( Array.length distributions) |> Math.Sqrt)
    let extractNthErrorDistribution errorRateIndex =
        results.Results 
        |> Array.map (List.map  (fun es -> List.nth es errorRateIndex) >> Utils.sampleDistribution)
        |> combineDistributions

    let geoMeans = extractGeoMeanDistrFromResults results.Results


    let trainErr = extractNthErrorDistribution 0
   //  List.concat |> List.map  (fun es -> List.nth es 0) |> Utils.sampleDistribution

    let testErr = extractNthErrorDistribution 1
    let isNnErrorMeasured = (results.Results.[0] |> List.head) |> List.length = 3
    let nnErr = if isNnErrorMeasured then extractNthErrorDistribution 2 else (Double.NaN, Double.NaN)
    {
        GeoMean = (geoMeans.Mean, geoMeans.StdErr)
        Training = trainErr
        Test = testErr
        NN = nnErr
        Settings = results.Settings
    }

let printMeanResults results =
    if results.NN |> fst |> Double.IsNaN then 
        sprintf "%s GeoMean: %f ~ %f; Training: %f ~ %f; Test: %f ~ %f" (results.Settings.ToShorthand ()) (fst results.GeoMean) (snd results.GeoMean) (fst results.Training) (snd results.Training) (fst results.Test) (snd results.Test)
    else 
        sprintf "%s GeoMean: %f ~ %f; Training: %f ~ %f; Test: %f ~ %f; NN: %f ~ %f" (results.Settings.ToShorthand ()) (fst results.GeoMean) (snd results.GeoMean)  (fst results.Training) (snd results.Training) (fst results.Test) (snd results.Test) (fst results.NN) (snd results.NN)

let printResults = toMeanResults >> printMeanResults



let testSettings parOverride rndSeed iterCount (settings : LvqModelSettingsCli)  =
    let foldGroupCount = (parOverride+9)/10
    let results =
        [
            for datasetGenerator in datasets -> 
                [for (foldGroup, dataset) in LazyList.take foldGroupCount datasetGenerator |> LazyList.toList |> List.zip (List.init foldGroupCount id) ->
                    Task.Factory.StartNew( (fun () ->
                            let groupOffset = foldGroup * 10
                            let mutable parsettings = settings
                            parsettings.ParallelModels <- Math.Min(parOverride - groupOffset,10)
                            let rndSeedOffset = 2u * rndSeed + uint32 groupOffset
                            parsettings.ParamsSeed <- rndSeedOffset + 1u
                            parsettings.InstanceSeed <- rndSeedOffset

                            let model = new LvqMultiModel(dataset,parsettings,false)
                            model.TrainUptoIters(iterCount, CancellationToken.None)
                            model.TrainUptoEpochs(1, CancellationToken.None)//always train at least 1 epoch
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
                ] //we have a list (per dataset) of a list (per 10-fold group) of a Task returing a list per fold of a list per error type
        ] |> List.map (List.map (fun task -> task.Result) >> List.concat) 
        |> List.toArray
    { 
        Settings = settings
        Results = results
        GeoMean = results |> extractGeoMeanDistrFromResults |> (fun distr->distr.Mean)
    }

let finalTestSettings = testSettings 30 1u 1e7


let logscale steps (v0, v1) = 
    let lnScale = Math.Log(v1 / v0)
    [ for i in [0..steps-1] -> v0 * Math.Exp(lnScale * (float i / (float steps - 1.))) ]

    //[0.001 -> 0.1]

let lrsChecker rndSeed lr0range settingsFactory iterCount = 
    [ for lr0 in lr0range ->  Task.Factory.StartNew ((fun () -> lr0 |> settingsFactory |> testSettings 2 rndSeed iterCount), TaskCreationOptions.LongRunning) ]
    |> Array.ofList
    |> Array.map (fun task -> task.Result)
    |> Array.sortBy (fun res -> res.GeoMean)

type ControllerDef = { Name: string; Unpacker: LvqModelSettingsCli -> float; Packer: LvqModelSettingsCli -> float -> LvqModelSettingsCli; }
type ControllerState = { Controller: ControllerDef;  LrLogDevScale: float }

(*let muControl = { 
        Unpacker = (fun settings-> settings.MuOffset)
        Packer = 
            fun (settings:LvqModelSettingsCli) mu -> 
                let mutable settingsCopy = settings
                settingsCopy.MuOffset <- mu
                settingsCopy
        DegradedCount = 0
        LrLogDevScale = 1.
    }*)

let lrBcontrol = { LrLogDevScale = 2.;  Controller =
                            { 
                                Name = "LrB"
                                Unpacker = (fun settings-> settings.LrScaleB)
                                Packer = fun (settings:LvqModelSettingsCli) lrB -> settings.WithLr(settings.LR0, settings.LrScaleP, lrB)
                            } }
let lrPcontrol =  { LrLogDevScale = 2.;  Controller =
                            {
                                Name = "LrP"
                                Unpacker = fun settings -> settings.LrScaleP
                                Packer = fun settings lrP -> settings.WithLr(settings.LR0, lrP, settings.LrScaleB)
                            } }
let lr0control =  { LrLogDevScale = 2.;  Controller =
                            {
                                Name = "Lr0"
                                Unpacker = fun settings -> settings.LR0
                                Packer = fun settings lr0 -> settings.WithLr(lr0, settings.LrScaleP, settings.LrScaleB)
                            } }

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

    let effRelevance = List.zip relevance linearlyScaledRelevance |> List.map (fun (a,b) -> (a + b)*Math.Sqrt(a+b))
    
    //printfn "%A" (bestToWorst |> List.map (fun res->lrUnpack res.Settings) |> List.zip relevance)
    let logLrDistr = List.zip logLrs effRelevance |> List.fold (fun (ss:SmartSum) (lr, rel) -> ss.CombineWith lr rel) (new SmartSum ())
    let (logLrmean, logLrdev) = (logLrDistr.Mean, Math.Sqrt logLrDistr.Variance)
    (Math.Exp logLrmean, logLrdev)

let improvementStep (controller:ControllerState) (initialSettings:LvqModelSettingsCli) degradedCount =
    let currSeed = EmnExtensions.MathHelpers.RndHelper.ThreadLocalRandom.NextUInt32 ()
    let iterCount = Math.Min(1e7, Math.Pow(1.5, float degradedCount) * 7.7e4) * Math.Min(1.0,2.5 / initialSettings.EstimateCost(10,32))
    let baseLr = controller.Controller.Unpacker initialSettings
    let lowLr = baseLr * Math.Exp(-Math.Sqrt(3.) * controller.LrLogDevScale)
    let highLr = baseLr * Math.Exp(Math.Sqrt(3.) * controller.LrLogDevScale)
    printfn "  %s [%f..%f]@%d: %f -> " controller.Controller.Name lowLr highLr (int iterCount) baseLr
    let initResults = testSettings 8 currSeed iterCount initialSettings
    let results = lrsChecker (currSeed + 2u) (logscale 15 (lowLr,highLr)) (controller.Controller.Packer initialSettings) iterCount
    let (newBaseLr, newLrLogDevScale) = improveLr (List.ofArray results) (controller.Controller.Unpacker, controller.Controller.Packer)
    let logLrDiff_LrDevScale = 2. * Math.Abs(Math.Log(baseLr / newBaseLr))
    let effNewLrDevScale =Math.Max(0.2, 0.25*newLrLogDevScale + 0.5*controller.LrLogDevScale + 0.25*logLrDiff_LrDevScale)
    let newSettings = controller.Controller.Packer initialSettings newBaseLr
    let finalResults =  testSettings 8 currSeed iterCount newSettings

    let newDegradedCount = degradedCount + (if finalResults.GeoMean > initResults.GeoMean || effNewLrDevScale <= 0.2 then 1 else 0 )
    let newControllerState = { Controller = controller.Controller; LrLogDevScale = effNewLrDevScale; }
    printfn "                                       %f: %f -> %f (%d,×%f)" newBaseLr initResults.GeoMean finalResults.GeoMean newDegradedCount (Math.Exp(Math.Sqrt(3.) * effNewLrDevScale))
    (newControllerState, (newSettings, newDegradedCount))

let improvementSteps (controllers:ControllerState list) (initialSettings:LvqModelSettingsCli) degradedCount=
    List.fold (fun (controllerStates, (settings, currDegradedCount)) nextController ->
            improvementStep nextController settings currDegradedCount
                |> apply1st (fun newControllerState -> newControllerState :: controllerStates)
        ) ([], (initialSettings, degradedCount)) controllers
    |> apply1st List.rev

let rec fullyImprove (controllers, (initialSettings, degradedCount)) =
    if degradedCount > 11 then
        improvementSteps controllers initialSettings degradedCount |> Utils.apply2nd fst
    else
        improvementSteps controllers initialSettings degradedCount |> fullyImprove

let uniformResultsFilePath = LrOptimizer.resultsDir.FullName + "\\uniform-results.txt"

let improveAndTest skipOptRounds (initialSettings:LvqModelSettingsCli) =
    printfn "Optimizing %s" (initialSettings.ToShorthand ())
    let needsB = [LvqModelType.G2m; LvqModelType.Ggm ; LvqModelType.Gpq] |> List.exists (fun modelType -> initialSettings.ModelType = modelType)
    let controllers = 
        [
           // if initialSettings.MuOffset <> 0. && LvqModelType.Ggm = initialSettings.ModelType then yield muControl
            yield lr0control
            yield lrPcontrol
            if needsB then yield lrBcontrol
       ]
    let improvedSettings = fullyImprove (controllers, (initialSettings,skipOptRounds)) |> snd
    let testedResults = finalTestSettings improvedSettings
    let resultString = printResults testedResults
    Console.WriteLine resultString
    File.AppendAllText (uniformResultsFilePath, resultString + "\n")
    testedResults

let isTested (lvqSettings:LvqModelSettingsCli) = 
    let canonicalSettings = (lvqSettings.ToShorthand() |> CreateLvqModelValues.ParseShorthand).WithDefaultLr()
    File.ReadAllLines uniformResultsFilePath
        |> Array.map (fun line -> (line.Split [|' '|]).[0] |> CreateLvqModelValues.TryParseShorthand)
        |> Seq.filter (fun settingsOrNull -> settingsOrNull.HasValue)
        |> Seq.map (fun settingsOrNull -> settingsOrNull.Value.WithDefaultSeeds().WithDefaultLr())
        |> Seq.exists canonicalSettings.Equals

let cleanupShorthand = CreateLvqModelValues.ParseShorthand >> (fun s->s.ToShorthand())
let allUniformResults () = 
    let parseLine (line:string) =
        let maybeSettings = line.SubstringUntil " " |> CreateLvqModelValues.TryParseShorthand
        if not maybeSettings.HasValue then 
            None
        else
            let parseChunk prefix = 
                if line.Contains(prefix) then
                    let arr = ((line.SubstringAfterFirst prefix).SubstringUntil ";").Split([|" ~ "|], StringSplitOptions.None) |> Array.map float
                    (arr.[0], if arr.Length >1 then arr.[1] else Double.NaN)
                else
                    (Double.NaN, Double.NaN)
                        
            Some({
                        GeoMean = parseChunk  "GeoMean: "
                        Training = parseChunk "Training: "
                        Test = parseChunk "Test: "
                        NN = parseChunk "NN: "
                        Settings = maybeSettings.Value
            })

    File.ReadAllLines (LrOptimizer.resultsDir.FullName + "\\uniform-results.txt")
        |> Seq.map parseLine
        |> Seq.filter Option.isSome
        |> Seq.map Option.get
        |> Seq.toList

let withDefaultLr (settings:LvqModelSettingsCli) = 
    let withlr = 
        match settings.ModelType with
            | LvqModelType.Gm -> settings.WithLr(0.002, 2., 0.)
            | LvqModelType.Ggm -> settings.WithLr(0.03, 0.05, 4.)
            | _ -> settings.WithLr(0.01, 0.4, 0.006)
    if withlr.LrRaw then withlr.WithLr(withlr.LR0, withlr.LrScaleP * withlr.LR0, withlr.LrScaleB* withlr.LR0) else withlr
