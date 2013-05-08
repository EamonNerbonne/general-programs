module OptimalLrSearch
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
            datasetFactory.InstanceSeed <- 0x1000u

            let infiniteVariations = 
                Seq.initInfinite (fun i ->
                        datasetFactory.IncInstanceSeed()
                        datasetFactory.CreateDataset()
                    )
            yield LazyList.ofSeq infiniteVariations
    ]

let sqr (x:float) = x * x

type TestResults = { Mean2:float; Results:float list list []; Settings:LvqModelSettingsCli; }
let extractMean2DistrFromResults = 
    Array.toList 
    >> Utils.flipList  
    >> List.map (
        List.concat 
        >> List.map (fun x -> x + 0.01) 
        >> List.averageBy Math.Log 
        >> (fun x-> Math.Exp x - 0.01)
        )
    >> Utils.sampleDistribution

type MeanTestResults = { Mean2:float * float; Training: float * float; Test: float * float; NN: float * float; Settings:LvqModelSettingsCli; }


let toMeanResults (results:TestResults) = 
    let combineDistributions distributions = (distributions |> Array.averageBy (fun distr->distr.Mean), (distributions |> Array.sumBy (fun distr -> distr.StdErr *  distr.StdErr)) / float ( Array.length distributions) |> Math.Sqrt)
    let extractNthErrorDistribution errorRateIndex =
        results.Results 
        |> Array.map (List.map  (fun es -> List.nth es errorRateIndex) >> Utils.sampleDistribution)
        |> combineDistributions

    let mean2s = extractMean2DistrFromResults results.Results


    let trainErr = extractNthErrorDistribution 0
   //  List.concat |> List.map  (fun es -> List.nth es 0) |> Utils.sampleDistribution

    let testErr = extractNthErrorDistribution 1
    let isNnErrorMeasured = (results.Results.[0] |> List.head) |> List.length = 3
    let nnErr = if isNnErrorMeasured then extractNthErrorDistribution 2 else (Double.NaN, Double.NaN)
    {
        Mean2 = (mean2s.Mean, mean2s.StdErr)
        Training = trainErr
        Test = testErr
        NN = nnErr
        Settings = results.Settings
    }

let printMeanResults results =
    if results.NN |> fst |> Double.IsNaN then 
        sprintf "%s GeoMean: %f ~ %f; Training: %f ~ %f; Test: %f ~ %f" (results.Settings.ToShorthand ()) (fst results.Mean2) (snd results.Mean2) (fst results.Training) (snd results.Training) (fst results.Test) (snd results.Test)
    else 
        sprintf "%s GeoMean: %f ~ %f; Training: %f ~ %f; Test: %f ~ %f; NN: %f ~ %f" (results.Settings.ToShorthand ()) (fst results.Mean2) (snd results.Mean2)  (fst results.Training) (snd results.Training) (fst results.Test) (snd results.Test) (fst results.NN) (snd results.NN)

let printResults = toMeanResults >> printMeanResults



let testSettings parOverride rndSeed iterCount (settings : LvqModelSettingsCli)  =
    let foldGroupCount = (parOverride+9)/10
    let relevantDatasets =  if settings.IsProjectionModel() then datasets |> List.rev |> List.tail |> List.rev else datasets
    let results =
        [
            for datasetGenerator in datasets -> 
                [for (foldGroup, dataset) in LazyList.take foldGroupCount datasetGenerator |> Seq.toList |> List.zip (List.init foldGroupCount id) ->
                    Task.Factory.StartNew( (fun () ->
                            let groupOffset = foldGroup * 10
                            let mutable parsettings = settings
                            parsettings.ParallelModels <- Math.Min(parOverride - groupOffset,10)
                            let rndSeedOffset = 2u * rndSeed + uint32 groupOffset
                            parsettings.ParamsSeed <- rndSeedOffset + 1u
                            parsettings.InstanceSeed <- rndSeedOffset
                            //printfn "INIT %s %f %s" dataset.DatasetLabel iterCount (parsettings.ToShorthand())
                            let model = new LvqMultiModel(dataset,parsettings,false)
                            //printfn "OPTI %s %f %s" dataset.DatasetLabel iterCount (parsettings.ToShorthand())
                            model.TrainUptoIters(iterCount, CancellationToken.None)
                            model.TrainUptoEpochs(1, CancellationToken.None)//always train at least 1 epoch
                            //printfn "EVAL %s %f %s" dataset.DatasetLabel iterCount (parsettings.ToShorthand())
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
                            //printfn "STOP %s %f %s" dataset.DatasetLabel iterCount (parsettings.ToShorthand())
                            errs
                        ), TaskCreationOptions.LongRunning)
                ] //we have a list (per dataset) of a list (per 10-fold group) of a Task returing a list per fold of a list per error type
        ] |> List.map (List.map (fun task -> task.Result) >> List.concat) 
        |> List.toArray
    { 
        Settings = settings
        Results = results
        Mean2 = results |> extractMean2DistrFromResults |> (fun distr->distr.Mean)
    }




let logscale steps (v0, v1) = 
    let lnScale = Math.Log(v1 / v0)
    [ for i in [0..steps-1] -> v0 * Math.Exp(lnScale * (float i / (float steps - 1.))) ]

    //[0.001 -> 0.1]

let lrsChecker rndSeed lr0range settingsFactory iterCount = 
    [ 
        for (lr0, index) in Seq.zip lr0range (Seq.initInfinite id) -> 
            Task.Factory.StartNew(
                (fun () -> lr0 |> settingsFactory |> testSettings 1 (rndSeed + uint32 index) iterCount),
                TaskCreationOptions.LongRunning)
    ]
    |> Array.ofList
    |> Array.map (fun task -> task.Result)
    |> Array.sortBy (fun res -> res.Mean2)

type ControllerDef = { Name: string; Unpacker: LvqModelSettingsCli -> float; Packer: LvqModelSettingsCli -> float -> LvqModelSettingsCli; InitLrLogDevScale: float; Applies: LvqModelSettingsCli->bool; StartAt: int32 }
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

let lrBcontrol = { 
                            Name = "LrB"
                            Unpacker = (fun settings-> settings.LrScaleB)
                            Packer = fun (settings:LvqModelSettingsCli) lrB -> settings.WithLr(settings.LR0, settings.LrScaleP, lrB)
                            Applies = fun settings -> [LvqModelType.G2m; LvqModelType.Ggm ; LvqModelType.Gpq ; LvqModelType.Fgm] |> List.exists (fun modelType -> settings.ModelType = modelType)
                            InitLrLogDevScale = 1.5
                            StartAt = 0
                        }
let lrPcontrol =  {
                            Name = "LrP"
                            Unpacker = fun settings -> settings.LrScaleP
                            Packer = fun settings lrP -> settings.WithLr(settings.LR0, lrP, settings.LrScaleB)
                            Applies = fun _ -> true
                            InitLrLogDevScale = 1.5
                            StartAt = 0
                        } 
let lr0control =  {
                                Name = "Lr0"
                                Unpacker = fun settings -> settings.LR0
                                Packer = fun settings lr0 -> settings.WithLr(lr0, settings.LrScaleP, settings.LrScaleB)
                                Applies = fun _ -> true
                                InitLrLogDevScale = 1.5
                                StartAt = 0
                       }
let iterScaleControl =  {
                                Name = "iterScale"
                                Unpacker = fun settings -> settings.iterScaleFactor
                                Packer = fun settings iterScale -> settings.WithIterScale iterScale
                                Applies = fun _ -> true
                                InitLrLogDevScale = 5.
                                StartAt = 14
                       }
let decayControl =  {
                                Name = "decay"
                                Unpacker = fun settings -> settings.decay
                                Packer = fun settings decay -> settings.WithDecay decay
                                Applies = fun _ -> true
                                InitLrLogDevScale = 4.
                                StartAt = 10
                       }
let muControl =  {
                                Name = "mu"
                                Unpacker = fun settings -> settings.MuOffset
                                Packer = fun settings mu -> 
                                                    let mutable retval = settings
                                                    retval.MuOffset <- mu
                                                    retval
                                Applies = fun settings -> settings.MuOffset > 0.0
                                InitLrLogDevScale = 1.5
                                StartAt = 3
                       }

let learningRateControllers = [lr0control; lrPcontrol; lrBcontrol]
let decayControllers = [iterScaleControl; decayControl; lr0control]
let allControllers = [lr0control; muControl; iterScaleControl; lrPcontrol; lrBcontrol; decayControl ]

let improveLr (testResultList:TestResults list) (lrUnpack, lrPack) =
    let errMargin = 0.01
    let unpackLogErrs testResults = testResults.Results |> Seq.concat |> Seq.concat |> List.ofSeq |> List.map (fun err -> Math.Log (err + errMargin))
    let bestToWorst = testResultList |> List.sortBy (unpackLogErrs >> List.average)
    let bestToWorstErrs = bestToWorst |> List.map (unpackLogErrs >> List.average)
    let logErrDistribution = Utils.sampleDistribution bestToWorstErrs
    let lowestLogErr =bestToWorstErrs |> List.head
    let highestLogErr =bestToWorstErrs |> (List.rev >>List.head)
    let bestLogErrs = List.head bestToWorst |> unpackLogErrs
    //extract list of error rates from each testresult
    let logLrs = testResultList |> List.map (fun res-> lrUnpack res.Settings |> Math.Log)
    printfn "%s" <| (List.head bestToWorst).Settings.ToShorthand()
    printfn "%A" <| (List.head bestToWorst).Results
    let byTtestRelevance = List.Cons (1.0, bestToWorst |> List.tail |> List.map (unpackLogErrs >> Utils.twoTailedPairedTtest bestLogErrs >> snd))
    //note that tTestRelevance isn't exactly ideal since different seeds were used in different results; however at least the same dataset was used...

    let testResultCount = List.length testResultList
    let linearlyScaledRelevance = List.init testResultCount (fun i -> float (testResultCount - i) / float testResultCount)
    let byErrRelevance = bestToWorstErrs |> List.map (fun logErr ->1. -  (logErr - lowestLogErr) / (highestLogErr - lowestLogErr)  )
    let byErrDistrRelevance = bestToWorstErrs |> List.map (fun logErr -> 0.5 * (1. - Math.Tanh ((logErr - logErrDistribution.Mean) / logErrDistribution.StdDev )))

    let effRelevance = [byTtestRelevance; linearlyScaledRelevance; byErrRelevance; byErrDistrRelevance] |> Utils.flipList |> List.map (List.average >> (fun rel -> Math.Pow(rel, 1.5)))
    //let effRelevance = List.zip3 relevance linearlyScaledRelevance byErrRelevance |> List.map (fun (a,b,c) -> Math.Sqrt(a * b * c))
    //[byTtestRelevance; linearlyScaledRelevance; byErrRelevance; byErrDistrRelevance] |> Utils.flipList |> printfn "%A"
    //printfn "%A" (bestToWorst |> List.map (fun res->lrUnpack res.Settings) |> List.zip3 effRelevance bestToWorstErrs)
    let logLrDistr = List.zip logLrs effRelevance |> List.fold (fun (ss:SmartSum) (lr, rel) -> ss.CombineWith lr (rel + 0.2)) (new SmartSum ())
    let (logLrmean, logLrdev) = (logLrDistr.Mean, Math.Sqrt logLrDistr.Variance)

    let geoMeanDistr = List.zip bestToWorst effRelevance |> List.fold (fun (ss:SmartSum) (tr, rel) -> ss.CombineWith tr.Mean2 rel) (new SmartSum ())
    let (geoMeanMean, geoMeanDev) = (geoMeanDistr.Mean, Math.Sqrt geoMeanDistr.Variance)

    ((Math.Exp logLrmean, logLrdev),(geoMeanMean, geoMeanDev))

let estimateRelativeCost (settings:LvqModelSettingsCli) = Math.Max(1.0, Math.Min( 10.0, settings.EstimateCost(10,32) / 2.5))
let finalTestSettings settings = testSettings 50 1u (1e7 / estimateRelativeCost settings) settings

let improvementStep (controller:ControllerState) (initialSettings:LvqModelSettingsCli) degradedCount =
    let stepCount = 20
    let currSeed = initialSettings.InstanceSeed //EmnExtensions.MathHelpers.RndHelper.ThreadLocalRandom.NextUInt32 ()
    let iterCount = Math.Min(1e7, Math.Pow(1.36, float degradedCount) * 7.4e4) / estimateRelativeCost initialSettings
    
    let workingSettings =
        //we'll drop the LR more quickly than usual for small runs.
        let safeIterCount = 1300000.0
        if iterCount >= safeIterCount || controller.Controller.Name = iterScaleControl.Name then
            initialSettings
        else
            let iterScaleCorrection = safeIterCount / iterCount
            iterScaleControl.Unpacker initialSettings * iterScaleCorrection
                |> iterScaleControl.Packer initialSettings

    let baseLr = controller.Controller.Unpacker initialSettings
    let lowLr = baseLr * Math.Exp(-Math.Sqrt(3.) * controller.LrLogDevScale)
    let highLr = baseLr * Math.Exp(Math.Sqrt(3.) * controller.LrLogDevScale)
    //let initResults = testSettings 5 currSeed iterCount initialSettings
    let results = lrsChecker (currSeed + 2u) (logscale stepCount (lowLr,highLr)) (controller.Controller.Packer workingSettings) iterCount
    let ((newBaseLr, newLrLogDevScale), (errM,errDV)) = improveLr (List.ofArray results) (controller.Controller.Unpacker, controller.Controller.Packer)
    let logLrDiff_LrDevScale = 2. *1. * Math.Abs(Math.Log(baseLr / newBaseLr))
    let minimalLrDevScale = 0.2 * controller.Controller.InitLrLogDevScale
    let maximalLrDevScale = 2. * controller.Controller.InitLrLogDevScale
    let effNewLrDevScale = Math.Max(minimalLrDevScale, Math.Min(0.4*newLrLogDevScale + 0.5*controller.LrLogDevScale + (0.1+0.2)*logLrDiff_LrDevScale, maximalLrDevScale))
    let mutable newSettings = controller.Controller.Packer initialSettings newBaseLr
    newSettings.InstanceSeed <- currSeed + uint32 stepCount * 2u
    //let finalResults =  testSettings 5 currSeed iterCount newSettings

    let newControllerState = { Controller = controller.Controller; LrLogDevScale = effNewLrDevScale; }
    printfn "  %d: %s [×%f: %f..%f]@%d:   %f -> %f;   (%f ~ %f) %s"  
        degradedCount controller.Controller.Name (Math.Exp(Math.Sqrt(3.) * controller.LrLogDevScale)) lowLr highLr (int iterCount) 
        baseLr newBaseLr errM errDV (newSettings.WithCanonicalizedDefaults().ToShorthand())
    (newControllerState, newSettings)

let improvementSteps (controllers:ControllerState list, initialSettings:LvqModelSettingsCli) degradedCount =
    let controllerOrdering = if degradedCount % 2 = 0 then controllers else List.rev controllers
    List.fold (fun (controllerStates, settings) nextController ->
            let newState = 
                if degradedCount < nextController.Controller.StartAt then
                    (nextController, settings)
                else
                    improvementStep nextController settings degradedCount
            newState |> apply1st (fun newControllerState -> newControllerState :: controllerStates)
        ) ([], initialSettings) controllerOrdering
    |> apply1st List.rev

let rec fullyImprove degradedCount state  =
    if degradedCount > 16 then
        state
    else
        improvementSteps state degradedCount |> fullyImprove (degradedCount + 1)


let mutable hasBeenLowered = false
let lowerPriority () = 
    if not hasBeenLowered then
        hasBeenLowered <- true
        use proc = System.Diagnostics.Process.GetCurrentProcess ()
        proc.PriorityClass <- System.Diagnostics.ProcessPriorityClass.Idle

let saveResults (filename:string) (results:TestResults) =
   let resultString:string = printResults results 
   File.AppendAllText (LrGuesser.resultsDir.FullName + "\\" + filename, resultString + "\n")
   Console.WriteLine resultString
   Console.WriteLine DateTime.Now


let improveAndTestWithControllers offset scaleSearchRange controllersToOptimize filename (initialSettings:LvqModelSettingsCli) =
    lowerPriority ()
    printfn "Optimizing %s" (initialSettings.ToShorthand ())
    let needsB = [LvqModelType.G2m; LvqModelType.Ggm ; LvqModelType.Gpq] |> List.exists (fun modelType -> initialSettings.ModelType = modelType)
    let states = controllersToOptimize 
                               |> List.filter (fun controller->controller.Applies initialSettings) 
                               |> List.map (fun controller -> { Controller = controller; LrLogDevScale = controller.InitLrLogDevScale * scaleSearchRange})
    let improvedSettings = fullyImprove offset (states, initialSettings) |> snd
    let testedResults = finalTestSettings improvedSettings
    saveResults filename testedResults
    testedResults

let improveAndTest = improveAndTestWithControllers 0 1.0 learningRateControllers

let isTested filename (lvqSettings:LvqModelSettingsCli) = 
    let canonicalSettings = (lvqSettings.ToShorthand() |> CreateLvqModelValues.ParseShorthand).WithCanonicalizedDefaults()
    let path = LrGuesser.resultsDir.FullName + "\\" + filename
    File.Exists path &&
        File.ReadAllLines (LrGuesser.resultsDir.FullName + "\\" + filename)
        |> Array.map (fun line -> (line.Split [|' '|]).[0] |> CreateLvqModelValues.TryParseShorthand)
        |> Seq.filter (fun settingsOrNull -> settingsOrNull.HasValue)
        |> Seq.map (fun settingsOrNull -> settingsOrNull.Value.WithCanonicalizedDefaults())
        |> Seq.exists canonicalSettings.Equals

let cleanupShorthand = CreateLvqModelValues.ParseShorthand >> (fun s->s.ToShorthand())

let allUniformResults filename = 
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
                        Mean2 = parseChunk  "GeoMean: "
                        Training = parseChunk "Training: "
                        Test = parseChunk "Test: "
                        NN = parseChunk "NN: "
                        Settings = maybeSettings.Value
            })
    let path = LrGuesser.resultsDir.FullName + "\\"+filename
    if not <| File.Exists path then
        []
    else
        File.ReadAllLines path
            |> Seq.map parseLine
            |> Seq.filter Option.isSome
            |> Seq.map Option.get
            |> Seq.toList

let withDefaultLr (settings:LvqModelSettingsCli) = 
    if settings.LR0 <> 0. then settings
    else
        let withlr = 
            match settings.ModelType with
                | LvqModelType.Gm -> settings.WithLr(0.002, 2., 0.)
                | LvqModelType.Ggm -> settings.WithLr(0.03, 0.05, 4.)
                | _ -> settings.WithLr(0.01, 0.4, 0.006)
        if withlr.LrRaw then withlr.WithLr(withlr.LR0, withlr.LrScaleP * withlr.LR0, withlr.LrScaleB* withlr.LR0) else withlr
