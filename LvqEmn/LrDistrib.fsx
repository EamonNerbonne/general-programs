#I @"ResultsAnalysis\bin\ReleaseMingw"
#r "ResultsAnalysis"
#r "LvqLibCli"
#r "LvqGui"

open LvqLibCli
open LvqGui    


type HeuristicsSettings = 
    { DataSettings: string; ModelSettings: LvqModelSettingsCli }
    member this.Equiv other = this.DataSettings = other.DataSettings && this.ModelSettings.ToShorthand() = other.ModelSettings.ToShorthand()
    member this.Key = this.DataSettings + "|" + (this.ModelSettings.ToShorthand())

let normalizeDatatweaks str = new System.String(str |> Seq.sortBy (fun c -> - int32 c) |> Seq.distinct |> Seq.toArray)

let getSettings (modelResults:ResultAnalysis.ModelResults) = { DataSettings = normalizeDatatweaks modelResults.DatasetTweaks; ModelSettings = modelResults.ModelSettings.WithDefaultLr().WithDefaultNnTracking()  }


type Heuristic = 
    { Name:string; Code:string; Activator: HeuristicsSettings -> (HeuristicsSettings * HeuristicsSettings); }
    //member this.IsActive settings =  (this.Activator settings |> fst).Equiv settings

let applyHeuristic (heuristic:Heuristic) settings = 
    let (on, off) = heuristic.Activator settings
    if off.Equiv settings && on.Equiv settings |> not && on.ModelSettings = CreateLvqModelValues.ParseShorthand(on.ModelSettings.ToShorthand()) then Some(on) else None

let isHeuristicApplied (heuristic:Heuristic) settings = 
    let (on, off) = heuristic.Activator settings
    on.Equiv settings && off.Equiv settings |> not && on.ModelSettings = CreateLvqModelValues.ParseShorthand(on.ModelSettings.ToShorthand())


let heuristics = 
    let heur name code activator = { Name=name; Code=code; Activator = activator; }
    let heurD name letter = { Name = name; Code = letter; Activator = (fun s -> 
        let on = normalizeDatatweaks (s.DataSettings + letter)
        let off = s.DataSettings.Replace(letter,"")

        ({ DataSettings = on; ModelSettings = s.ModelSettings}, { DataSettings = off; ModelSettings = s.ModelSettings}))}
    let heurM name code activator = 
        {
            Name = name;
            Code = code;
            Activator = (fun s ->
                let (on, off) = activator s.ModelSettings
                ({ DataSettings = s.DataSettings; ModelSettings = on }, { DataSettings = s.DataSettings; ModelSettings = off }))
        }
    [
        heurM @"Initializing prototype positions by neural gas" "NGi+" (fun s  -> 
            let mutable on = s
            let mutable off = s
            on.NgInitializeProtos <- true
            off.NgInitializeProtos <- false
            (on,off))
        heurM @"Using neural gas-like prototype updates" "NG+" (fun s  -> 
            let mutable on = s
            let mutable off = s
            on.NgUpdateProtos <- true
            off.NgUpdateProtos <- false
            (on, off))
        heurM @"Initializing $P$ by PCA" "rP" (fun s  -> 
            let mutable on = s
            let mutable off = s
            on.RandomInitialProjection <- false
            off.RandomInitialProjection <- true
            (on, off))
        heurM @"Initially using a lower learning rate for incorrect prototypes" "!" (fun s  -> 
            let mutable on = s
            let mutable off = s
            on.SlowStartLrBad <- true
            off.SlowStartLrBad <- false
            (on, off))
        heurM @"Using the gm-lvq update rule for prototype positions in g2m-lvq models" "noB+" (fun s  -> 
            let mutable on = s
            let mutable off = s
            on.UpdatePointsWithoutB <- true
            off.UpdatePointsWithoutB <- false
            (on, off))
        heurM @"Optimizing $P$ initially by minimizing $\sqrt{d_J} - \sqrt{d_K}$" "Pi+" (fun s  -> 
            let mutable on = s
            let mutable off = s
            on.ProjOptimalInit <- true
            off.ProjOptimalInit <- false
            (on, off))
        heurM @"Initializing $B_i$ to the local covariance" "Bi+" (fun s  -> 
            let mutable on = s
            let mutable off = s
            on.BLocalInit <- true
            off.BLocalInit <- false
            (on, off))
        heurD "Extend dataset by correlations (x)" "x"
        heurD "Normalize each dimension (n)" "n"
        heurD "pre-normalized segmentation dataset (N)" "N"
    ]

let datasetResults =
        TestLr.resultsDir.GetDirectories()
        |> Seq.filter (fun dir -> dir.Name <> "base")
        |> Seq.collect (fun dir-> dir.GetFiles("*.txt"))
        |> Seq.map LvqGui.DatasetResults.ProcFile
        |> Seq.filter (fun res -> res <> null && res.trainedIterations > 2.e7 && res.trainedIterations < 4.e7)
        |> Seq.toList

let lrTestingResults = 
    [
        for dr in datasetResults do
            let dFactory = CreateDataset.CreateFactory dr.resultsFile.Directory.Name
            let (dataKey, dataHeur) =  ResultAnalysis.decodeDataset dFactory
            yield (dataKey, dataHeur, dr.unoptimizedSettings,  dr.GetLrs() |> Seq.toArray)
    ]

let hasNoHeuristics (settings:LvqModelSettingsCli) = (LvqModelSettingsCli()).WithChanges(settings.ModelType, settings.PrototypesPerClass, settings.ParamsSeed, settings.InstanceSeed).Equals(settings)

let basicTypes = new System.Collections.Generic.HashSet<LvqModelSettingsCli>(GeneralSettings.basicTypes)
let plainLrTestingResults =
    lrTestingResults |> List.filter
        (fun (_,dHeur,settings,_) -> 
            (dHeur = "" || dHeur="n")
                && basicTypes.Contains(settings)
        )   


let plainCompleteLrTestingResults = 
    plainLrTestingResults 
    |> Utils.groupList (fun (dKey,_,_,_) -> dKey) id
    |> List.filter (fun rs -> List.length (snd rs) = 12) // only include datasets with all basic combos
    |> List.collect snd

let trainingErr (errs:TestLr.ErrorRates) = errs.training

let meanBestLrLookup = 
    plainCompleteLrTestingResults
        |> Utils.groupList (fun (dKey,dHeur,mSettings,lrs) -> (dHeur, mSettings.ToShorthand()))  (fun (dKey,dHeur,mSettings,lrs) -> lrs)
        |> List.map 
            (fun (key, lrs) ->
                let (lr, trainErr) = 
                    Seq.collect id lrs 
                        |> List.ofSeq
                        |> ResultParsing.groupErrorsByLr
                        |> List.map (Utils.apply2nd (ResultParsing.meanStderrOfErrs >> trainingErr))
                        |> List.sortBy snd
                        |> List.head
                let trainErrOfBestLr = 
                    lrs |> List.map 
                        (fun oneDatasetResultArr -> 
                            oneDatasetResultArr 
                            |> Array.map (fun lrAndErr -> lrAndErr.Errors.training)
                            |> Array.min
                        )
                    |> List.average
                (key,(lr, trainErr,trainErrOfBestLr))
            )
        
