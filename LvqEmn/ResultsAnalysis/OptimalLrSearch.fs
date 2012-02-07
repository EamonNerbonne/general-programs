module OptimalLrSearch
open System
open System.IO
open System.Threading
open System.Threading.Tasks
open EmnExtensions.Text

open LvqGui
open LvqLibCli
open Utils

let datasets = 
    Lazy.Create (fun () ->
        [
            for datasetFactory in CreateDataset.StandardDatasets() do
                datasetFactory.NormalizeDimensions <- true
                yield datasetFactory.CreateDataset()
        ]
    )

type TestResults = { GeoMean:float; Mean:float;  Results:float list list []; Settings:LvqModelSettingsCli; }

type MeanTestResults = { GeoMean:float; Training: float * float; Test: float * float; NN: float * float; Settings:LvqModelSettingsCli; }

let toMeanResults (results:TestResults) = 
    let trainDistr = results.Results |> List.concat |> List.map  (fun es -> List.nth es 0) |> Utils.sampleDistribution
    let trainErr = (trainDistr.Mean, trainDistr.StdErr)

    let testDistr = results.Results |> List.concat |> List.map  (fun es -> List.nth es 1) |> Utils.sampleDistribution
    let testErr = (testDistr.Mean, testDistr.StdErr)
    let x = (results.Results.[0] |> List.head) |> List.length
    let nnErr = 
        if (results.Results.[0] |> List.head) |> List.length = 3 then 
            let nnDistr = results.Results |> List.concat |> List.map  (fun es -> List.nth es 2) |> Utils.sampleDistribution
            (nnDistr.Mean, nnDistr.StdErr)
        else 
            (Double.NaN, Double.NaN)
    {
        GeoMean = results.GeoMean
        Training = trainErr
        Test = testErr
        NN = nnErr
        Settings = results.Settings
    }

let printMeanResults results =
    if results.NN |> fst |> Double.IsNaN then 
        sprintf "%s GeoMean: %f; Training: %f ~ %f; Test: %f ~ %f" (results.Settings.ToShorthand ()) results.GeoMean (fst results.Training) (snd results.Training) (fst results.Test) (snd results.Test)
    else 
        sprintf "%s GeoMean: %f; Training: %f ~ %f; Test: %f ~ %f; NN: %f ~ %f" (results.Settings.ToShorthand ()) results.GeoMean (fst results.Training) (snd results.Training) (fst results.Test) (snd results.Test) (fst results.NN) (snd results.NN)

let printResults = toMeanResults >> printMeanResults

let testSettings parOverride rndSeed iterCount (settings : LvqModelSettingsCli)  =
    let results =
        [
            for dataset in datasets.Value ->
                Task.Factory.StartNew( (fun () ->
                    let mutable parsettings = settings
                    parsettings.ParallelModels <- parOverride
                    parsettings.ParamsSeed <- 2u * rndSeed + 1u
                    parsettings.InstanceSeed <- 2u * rndSeed
                    let model = new LvqMultiModel(dataset,parsettings,false)
                    model.TrainUptoIters(iterCount,dataset, CancellationToken.None)
                    model.TrainUptoEpochs(1,dataset, CancellationToken.None)//always train at least 1 epoch
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
    let initResults = testSettings 10 currSeed iterCount initialSettings
    let baseLr = controller.Controller.Unpacker initialSettings
    let lowLr = baseLr * Math.Exp(-Math.Sqrt(3.) * controller.LrLogDevScale)
    let highLr = baseLr * Math.Exp(Math.Sqrt(3.) * controller.LrLogDevScale)
    let results = lrsChecker (currSeed + 2u) (logscale 20 (lowLr,highLr)) (controller.Controller.Packer initialSettings) iterCount
    let (newBaseLr, newLrLogDevScale) = improveLr (List.ofArray results) (controller.Controller.Unpacker, controller.Controller.Packer)
    let logLrDiff_LrDevScale = 2. * Math.Abs(Math.Log(baseLr / newBaseLr))
    let effNewLrDevScale =Math.Max(0.2, 0.3*newLrLogDevScale + 0.3*controller.LrLogDevScale + 0.4*logLrDiff_LrDevScale)
    let newSettings = controller.Controller.Packer initialSettings newBaseLr
    let finalResults =  testSettings 10 currSeed iterCount newSettings

    let newDegradedCount = degradedCount + (if finalResults.GeoMean > initResults.GeoMean || effNewLrDevScale <= 0.2 then 1 else 0 )
    let newControllerState = { Controller = controller.Controller; LrLogDevScale = effNewLrDevScale; }
    printfn "  %s [%f..%f]@%d: %f -> %f: %f -> %f (%d,×%f)" controller.Controller.Name lowLr highLr (int iterCount) baseLr newBaseLr initResults.GeoMean finalResults.GeoMean newDegradedCount (Math.Exp(Math.Sqrt(3.) * effNewLrDevScale))
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

let improveAndTest (initialSettings:LvqModelSettingsCli) =
    printfn "Optimizing %s" (initialSettings.ToShorthand ())
    let needsB = [LvqModelType.G2m; LvqModelType.Ggm ; LvqModelType.Gpq] |> List.exists (fun modelType -> initialSettings.ModelType = modelType)
    let controllers = 
        [
           // if initialSettings.MuOffset <> 0. && LvqModelType.Ggm = initialSettings.ModelType then yield muControl
            yield lr0control
            yield lrPcontrol
            if needsB then yield lrBcontrol
       ]
    let improvedSettings = fullyImprove (controllers, (initialSettings,0)) |> snd
    let testedResults = testSettings 100 1u 1e7 improvedSettings
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
            let trnChunkTraining = ((line.SubstringAfterFirst "Training: ").SubstringUntil ";" ).Split([|" ~ "|], StringSplitOptions.None) |> Array.map float
            let tstChunkTraining = ((line.SubstringAfterFirst "Test: ").SubstringUntil ";").Split([|" ~ "|], StringSplitOptions.None) |> Array.map float
            let nnChunkTraining = 
                if line.Contains("NN: ") then
                    ((line.SubstringAfterFirst "NN: ")).Split([|" ~ "|], StringSplitOptions.None) |> Array.map float
                else
                    [|Double.NaN; Double.NaN|]

            Some({
                        GeoMean = (line.SubstringAfterFirst "GeoMean: ").SubstringUntil ";" |> float
                        Training = (trnChunkTraining.[0], trnChunkTraining.[1])
                        Test = (tstChunkTraining.[0], tstChunkTraining.[1])
                        NN = (nnChunkTraining.[0], nnChunkTraining.[1])
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
