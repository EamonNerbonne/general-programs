module HeuristicAnalysis

open LvqLibCli
open System

type HeuristicsSettings = { DataSettings: string; ModelSettings: LvqModelSettingsCli }

let normalizeDatatweaks str = new System.String(str |> Seq.sortBy (fun c -> - int32 c) |> Seq.distinct |> Seq.toArray)

let getSettings (modelResults:LvqRunAnalysis.ModelResults) = { DataSettings = normalizeDatatweaks modelResults.DatasetTweaks; ModelSettings = modelResults.ModelSettings.WithCanonicalizedDefaults().WithDefaultNnTracking()  }

type Heuristic = 
    { Name:string; Code:string; Activator: HeuristicsSettings -> (HeuristicsSettings option * HeuristicsSettings ); }
    //member this.IsActive settings =  (this.Activator settings |> fst).Equiv settings

let applyHeuristic (heuristic:Heuristic) = heuristic.Activator >> fst
let unapplyHeuristic (heuristic:Heuristic) = heuristic.Activator >> snd
let reapplyHeuristic (heuristic:Heuristic) = unapplyHeuristic heuristic >> applyHeuristic heuristic
let isHeuristicApplied (heuristic:Heuristic) settings = reapplyHeuristic heuristic settings = Some(settings)

let heuristics = 
    let heur name code activator = { Name=name; Code=code; Activator = activator; }
    let heurD name code letter = { Name = name; Code = code; Activator = (fun s -> 
        let on = normalizeDatatweaks (s.DataSettings + letter)
        let off = s.DataSettings.Replace(letter,"")
        (
            ( if off = s.DataSettings && on <> s.DataSettings then Some({ DataSettings = on; ModelSettings = s.ModelSettings}) else None ),
            { DataSettings = off; ModelSettings = s.ModelSettings}
        ))}
        
    let heurM name code activator = 
        {
            Name = name;
            Code = code;
            Activator = (fun s ->
                let (on, off) = activator s.ModelSettings
                (
                    (if off = s.ModelSettings && on <> s.ModelSettings && on = on.Canonicalize() then Some({ DataSettings = s.DataSettings; ModelSettings = on }) else None), 
                    { DataSettings = s.DataSettings; ModelSettings = off }
                ))
        }
    let heurC a b =
        {
            Name = a.Name + ", " + b.Name;
            Code = a.Code + " + "+ b.Code;
            Activator = (fun s ->
                let (onA,offA) = a.Activator s
                let on = Option.bind (b.Activator>>fst) onA
                let (_,off) = b.Activator offA
                ((if off = s then on else None), off)
                )
        }
    let normHeur = heurD "Normalize each dimension" "normalize" "n"
    let normSnotNheur =
        heur "Scale features identically when normalizing" "S"
            (fun s -> 
                let on = normalizeDatatweaks (s.DataSettings.Replace("n","") + "S")
                let off = normalizeDatatweaks (s.DataSettings.Replace("S","") + "n")
                (
                    ( if off = s.DataSettings && on <> s.DataSettings then Some({ DataSettings = on; ModelSettings = s.ModelSettings}) else None ),
                    { DataSettings = off; ModelSettings = s.ModelSettings}
                )
            )
    let NGiHeur = heurM @"Initializing prototype positions by neural gas" "NGi" (fun s  -> 
            let mutable on = s
            let mutable off = s
            on.NGi <- true
            off.NGi <- false
            (on,off))
    let SlowK =         heurM @"Initially using a lower learning rate for incorrect prototypes" "SlowK" (fun s  -> 
            let mutable on = s
            let mutable off = s
            on.SlowK <- true
            off.SlowK <- false
            (on, off))
    let Ppca = heurM @"Initializing $P$ by PCA" "Ppca" (fun s  -> 
            let mutable on = s
            let mutable off = s
            on.Ppca <- true
            off.Ppca <- false
            (on, off))
    let extend = heurD "Extend dataset by correlations" "extend" "x"
    [
        normHeur
        SlowK
        NGiHeur
        Ppca
        heurM @"Using neural gas-like prototype updates" "NGu" (fun s  -> 
            let mutable on = s
            let mutable off = s
            on.NGu <- true
            off.NGu <- false
            (on, off))
        heurM @"Optimizing $P$ initially by minimizing $\sqrt{d_J} - \sqrt{d_K}$" "Popt" (fun s  -> 
            let mutable on = s
            let mutable off = s
            on.Popt <- true
            off.Popt <- false
            (on, off))
        heurM @"Initializing $B_i$ to the local covariance" "Bcov" (fun s  -> 
            let mutable on = s
            let mutable off = s
            on.Bcov <- true
            off.Bcov <- false
            (on, off))
        heurM @"Using the gm-lvq update rule for prototype positions in g2m-lvq models" "wGMu" (fun s  -> 
            let mutable on = s
            let mutable off = s
            on.wGMu <- true
            off.wGMu <- false
            (on, off))


        normSnotNheur
        heurC normHeur NGiHeur
        heurC normHeur SlowK
        heurC normHeur  Ppca
        SlowK |> heurC NGiHeur |> heurC normHeur
        Ppca |> heurC NGiHeur |> heurC normHeur
        Ppca |> heurC SlowK |> heurC normHeur
        Ppca |> heurC SlowK |> heurC NGiHeur |> heurC normHeur
        extend
        heurC extend normHeur
        heurM @"Initializing prototype positions by neural gas and seting $B_i$ to the local covariance" "NGi+Bcov" (fun s  -> 
            let mutable on = s
            let mutable off = s
            on.Bcov <- true
            off.Bcov <- false
            on.NGi <- true
            off.NGi <- false
            (on, off))
        (*heurM @"Optimizing $P$, setting $B_i$ to the local covariance, and initially using a lower learning rate for incorrect prototypes" "NGi+Bcov+SlowK" (fun s  -> 
            let mutable on = s
            let mutable off = s
            on.Bcov <- true
            off.Bcov <- false
            on.NGi <- true
            off.NGi <- false
            on.SlowK <- true
            off.SlowK <- false
            (on, off))*)
        heurM @"Neural gas prototype initialization followed by $P$ optimization" "NGi+Popt" (fun s  -> 
            let mutable on = s
            let mutable off = s
            on.Popt <- true
            off.Popt <- false
            on.NGi <- true
            off.NGi <- false
            (on, off))
        heurM @"$P$ optimization and neural gas-like updates" "NGu+Popt" (fun s  -> 
            let mutable on = s
            let mutable off = s
            on.NGu <- true
            off.NGu <- false
            on.Popt <- true
            off.Popt <- false
            (on, off))
        heurM @"Neural gas prototype initialization and initially using lower learning rates for incorrect prototypes" "NGi+SlowK" (fun s  -> 
            let mutable on = s
            let mutable off = s
            on.NGi <- true
            off.NGi <- false
            on.SlowK <- true
            off.SlowK <- false
            (on, off))
        heurM @"Neural gas-like updates and initially using lower learning rates for incorrect prototypes" "NGu+SlowK" (fun s  -> 
            let mutable on = s
            let mutable off = s
            on.NGu <- true
            off.NGu <- false
            on.SlowK <- true
            off.SlowK <- false
            (on, off))
        heurM @"Neural gas prototype initialization and neural gas-like updates" "NGi+NGu" (fun s  -> 
            let mutable on = s
            let mutable off = s
            on.NGi <- true
            off.NGi <- false
            on.NGu <- true
            off.NGu <- false
            (on, off))
            //*)
        //heurD "pre-normalized segmentation dataset (N)" "N"
    ]

type Difference = 
    | Better
    | Worse
    | Irrelevant

//let comparisonErrChange (baseErrs, heurErrs) =  
//    let meanBaseErr:float = List.average baseErrs
//    let meanHeurErr = List.average heurErrs
//    let scaleErr = Math.Max(meanBaseErr, meanHeurErr)
//    List.zip baseErrs heurErrs |> List.map (fun (baseErr,heurErr) -> (heurErr - baseErr) / scaleErr * 100.) |> List.average


let comparisonP  (baseErrs, heurErrs) = 
    if List.length baseErrs >= 2 && baseErrs <> heurErrs then
        Utils.twoTailedPairedTtest heurErrs baseErrs
    else
        (false, 1.)
let comparisonErrChange (baseErr:float, heurErr:float) =  (heurErr - baseErr) / Math.Max(heurErr, baseErr) * 100.
let comparisonRelevance (baseErrs, heurErrs) =
     let (isBetter, p) = comparisonP (baseErrs, heurErrs)
     if p > 0.01 * Math.Abs(comparisonErrChange (List.average baseErrs, List.average heurErrs)) then Irrelevant elif isBetter then Better else Worse
let comparisonBetterRatio (baseErrs, heurErrs) = float (List.zip heurErrs baseErrs |> List.filter (fun (hE,bE) -> hE < bE) |> List.length) / float (List.length heurErrs)
let scenarioLatexShorthand baseResults =
    let lvqSettings = getSettings baseResults
    let tweaksL = LvqRunAnalysis.latexLiteral (lvqSettings.DataSettings + " " + lvqSettings.ModelSettings.ToShorthand()) 
    LvqRunAnalysis.friendlyDatasetLatexName baseResults.DatasetBaseShorthand + @"\phantom{" + tweaksL + @"}&\llap{" + tweaksL + "}"


let compare (baseResults, heurResults) =
    let errs (model:LvqRunAnalysis.ModelResults) = model.Results |> Seq.map (fun res->res.CanonicalError*100.) |> Seq.toList 
    let bothErrs = (errs baseResults, errs heurResults)
    let resCount = bothErrs |> fst |> List.length
    let betterRatio = bothErrs |> comparisonBetterRatio
    let (isBetter, p) = bothErrs |> comparisonP
    let errChange = bothErrs |> Utils.apply2 List.average |> comparisonErrChange
    let bestErrChange = bothErrs |> Utils.apply2 List.min |> comparisonErrChange
    let difference = bothErrs |> comparisonRelevance
    let bothDistribs = bothErrs |> Utils.apply2 Utils.sampleDistribution
    let scenarioLatex = scenarioLatexShorthand baseResults
    ( (difference,scenarioLatex),  ((p,  bothDistribs),((errChange, bestErrChange),  (betterRatio, resCount))) )

let optCompare datasetResults modelResults heuristic =
    modelResults
    |> getSettings 
    |> applyHeuristic heuristic 
    //|> Option.map (fun settingsWithHeuristic -> settingsWithHeuristic.Key)
    |> Option.bind (Utils.getMaybe datasetResults)
    |> Option.map (fun (heuristicResults:LvqRunAnalysis.ModelResults) -> (modelResults, heuristicResults))


let resultsByDatasetByModel =
    LvqRunAnalysis.analyzedModels () 
        |> Seq.groupBy (fun modelRes -> modelRes.DatasetBaseShorthand) 
        |> Seq.map (Utils.apply2nd List.ofSeq)
        |> List.ofSeq
        |> List.filter (snd >> (fun xs->List.length xs >85))
        |> List.map
                (Utils.apply2nd
                    (Utils.toDict
                        (fun modelRes -> (getSettings modelRes)) 
                        (fun modelRess -> 
                            match Seq.toArray modelRess with
                            | [| modelRes |] -> modelRes
                            | modelResArr -> failwith (sprintf "whoops: %A" modelResArr)
                        )
                    )
                )
        |> dict
            

//        |> Utils.toDict (fun modelRes -> modelRes.DatasetBaseShorthand) 
//                (Utils.toDict 
//                    (fun modelRes -> (getSettings modelRes).Key) 
//                    (fun modelRess -> 
//                        match Seq.toArray modelRess with
//                        | [| modelRes |] -> modelRes
//                        | modelResArr -> failwith (sprintf "whoops: %A" modelResArr)
//                    )
//                )

//let allResults = resultsByDatasetByModel.Values |> Seq.collect (fun v-> v.Values) |> List.ofSeq
//
//let borkedResults = 
//    allResults 
//        |> List.filter
//            (fun res ->
//                res.Results |> Seq.forall (fun run -> run.TestError = run.TrainingError)
//            )
//    
//for result in borkedResults do
//    result.ModelStatFile.Delete()


let countActiveHeuristicsB (settings:HeuristicsSettings) =
    let ms = settings.ModelSettings
    let modelHeurs = [ms.Bcov; ms.NGi;  ms.NGu; ms.Popt; ms.Ppca; ms.SlowK;  ms.wGMu] |> List.filter id |> List.length
    modelHeurs + settings.DataSettings.Length

let countActiveHeuristics = getSettings >> countActiveHeuristicsB


let allFilters = 
    let simplifyName (name:string) = if name.Contains("-") then name.Substring(0, name.IndexOf("-")) else name
    let normOnlyFilter = (@"only \textsf{normalize}", (fun (mr:LvqRunAnalysis.ModelResults) -> mr.DatasetTweaks.Contains("n") && countActiveHeuristics mr = 1 ))
    let normFilter = ("\textsf{normalize}", (fun (mr:LvqRunAnalysis.ModelResults) -> mr.DatasetTweaks.Contains("n") ))
    let noFilter = ("", (fun (mr:LvqRunAnalysis.ModelResults) -> true))

    let singleHeurFilter = ("no other heuristics", (fun (mr:LvqRunAnalysis.ModelResults)  -> countActiveHeuristics mr = 0))
    let onlyHeurFilter heur = 
        (@"only \textsf{" + heur.Code + "}", (fun (mr:LvqRunAnalysis.ModelResults) ->
                let settings = getSettings mr
                let (on,off) = heur.Activator settings
                countActiveHeuristicsB off = 0 && (off |> heur.Activator |> fst) = Some(settings)
            ) 
        )
    let singleOrAnythingFilters = [
        ("all results", (fun (mr:LvqRunAnalysis.ModelResults) -> true));
        singleHeurFilter;
        ("1ppc, no other heuristics", (fun (mr:LvqRunAnalysis.ModelResults)  -> countActiveHeuristics mr = 0 && mr.ModelSettings.PrototypesPerClass = 1))
        ("5ppc, no other heuristics", (fun (mr:LvqRunAnalysis.ModelResults)  -> countActiveHeuristics mr = 0 && mr.ModelSettings.PrototypesPerClass = 5))
        normOnlyFilter;
        (@"1ppc, only \textsf{normalize}", (fun (mr:LvqRunAnalysis.ModelResults)  -> mr.DatasetTweaks.Contains("n") && countActiveHeuristics mr = 1 && mr.ModelSettings.PrototypesPerClass = 1))
        (@"5ppc, only \textsf{normalize}", (fun (mr:LvqRunAnalysis.ModelResults)  -> mr.DatasetTweaks.Contains("n") && countActiveHeuristics mr = 1 && mr.ModelSettings.PrototypesPerClass = 5))
        ]
    let modelFilters = [
        ("GM 1", (fun (mr:LvqRunAnalysis.ModelResults) -> mr.ModelSettings.ModelType = LvqModelType.Gm && mr.ModelSettings.PrototypesPerClass = 1));
        ("G2M 1", (fun mr -> mr.ModelSettings.ModelType = LvqModelType.G2m && mr.ModelSettings.PrototypesPerClass = 1));
        ("GGM 1", (fun (mr:LvqRunAnalysis.ModelResults) -> mr.ModelSettings.ModelType = LvqModelType.Ggm && mr.ModelSettings.PrototypesPerClass = 1));
        ("GM 5", (fun mr -> mr.ModelSettings.ModelType = LvqModelType.Gm && mr.ModelSettings.PrototypesPerClass = 5));
        ("G2M 5", (fun mr -> mr.ModelSettings.ModelType = LvqModelType.G2m && mr.ModelSettings.PrototypesPerClass = 5));
        ("GGM,5", (fun mr -> mr.ModelSettings.ModelType = LvqModelType.Ggm && mr.ModelSettings.PrototypesPerClass = 5));
        ]
    let datasetFilters = 
        Seq.toList resultsByDatasetByModel.Keys
            |> List.filter (fun key -> resultsByDatasetByModel.[key].Count > 65)
            |> List.map (fun datasetKey ->(defaultArg (LvqRunAnalysis.friendlyDatasetName datasetKey) datasetKey, (fun (mr:LvqRunAnalysis.ModelResults)  ->mr.DatasetBaseShorthand = datasetKey)))
    
    let comb (nameA, filterA) (nameB, filterB) = 
        ((if nameB <> "" then nameA + " * " + nameB else nameA), (fun x -> filterA x && filterB x))

    let perModelHeurFilters =
        [
        for hFilt in [singleHeurFilter; normOnlyFilter; ] do
            for mFilt in modelFilters do
                yield comb mFilt hFilt
        ]
    let datasetAndNormedFilters =
        [
        for hFilt in [noFilter; singleHeurFilter; normOnlyFilter;] do
            for dFilt in datasetFilters do
                yield comb dFilt hFilt
        ]

    let perModelDatasetFilters = 
        [
        for dFilt in datasetFilters do
            for dnFilt in [comb dFilt singleHeurFilter; comb dFilt normOnlyFilter] do
                for mFilt in modelFilters do
                    yield comb dnFilt mFilt
                
        ]
    List.concat 
        [  
            singleOrAnythingFilters;
            perModelHeurFilters;
            datasetAndNormedFilters;
            heuristics |> List.map (fun heur ->  (@"\textsf{" + heur.Code + "}", getSettings >> isHeuristicApplied heur) );
            heuristics |> List.filter (fun heur ->heur.Code <> "normalize" && heur.Code <> "extend" && heur.Code <> "S") |>List.map onlyHeurFilter;
            perModelDatasetFilters;
        ]
    

let analysisPairsGiven (filter:LvqRunAnalysis.ModelResults -> bool) (heur:Heuristic) = 
    seq {
        for datasetRes in resultsByDatasetByModel.Values do
            for modelRes in datasetRes.Values do
                if filter modelRes then
                    match optCompare datasetRes modelRes heur with
                    | None -> ()
                    | Some(comparison) -> yield comparison
    }



let analysisGiven filter heur = analysisPairsGiven filter heur |> Seq.map compare

let getTrainingError (r:LvqRunAnalysis.SingleLvqRunOutcome) =  r.TrainingError
let curry f x y = f (x, y)
let uncurry f (x, y) = f x y
let uncurry3 f (x, y, z) = f x y z
