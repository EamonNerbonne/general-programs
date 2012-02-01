#I @"ResultsAnalysis\bin\ReleaseMingw"
#r "ResultsAnalysis"
#r "LvqLibCli"
#r "LvqGui"
#r "EmnExtensions"
#time "on"

open LvqLibCli
open LvqGui
open System.Threading
open EmnExtensions.Text
open EmnExtensions
open System.IO
open System.Collections.Generic
open System.Linq
open System


type HeuristicsSettings = { DataSettings: string; ModelSettings: LvqModelSettingsCli }

let normalizeDatatweaks str = new System.String(str |> Seq.sortBy (fun c -> - int32 c) |> Seq.distinct |> Seq.toArray)

let getSettings (modelResults:ResultAnalysis.ModelResults) = { DataSettings = normalizeDatatweaks modelResults.DatasetTweaks; ModelSettings = modelResults.ModelSettings.WithDefaultLr().WithDefaultNnTracking().Canonicalize()  }

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
    let tweaksL = ResultAnalysis.latexLiteral (lvqSettings.DataSettings + " " + lvqSettings.ModelSettings.ToShorthand()) 
    ResultAnalysis.friendlyDatasetLatexName baseResults.DatasetBaseShorthand + @"\phantom{" + tweaksL + @"}&\llap{" + tweaksL + "}"


let compare (baseResults, heurResults) =
    let errs (model:ResultAnalysis.ModelResults) = model.Results |> Seq.map (fun res->res.CanonicalError*100.) |> Seq.toList 
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
    |> Option.map (fun (heuristicResults:ResultAnalysis.ModelResults) -> (modelResults, heuristicResults))


let resultsByDatasetByModel =
    ResultAnalysis.analyzedModels () 
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
    let normOnlyFilter = (@"only \textsf{normalize}", (fun (mr:ResultAnalysis.ModelResults) -> mr.DatasetTweaks.Contains('n') && countActiveHeuristics mr = 1 ))
    let normFilter = ("\textsf{normalize}", (fun (mr:ResultAnalysis.ModelResults) -> mr.DatasetTweaks.Contains('n') ))
    let noFilter = ("", (fun (mr:ResultAnalysis.ModelResults) -> true))

    let singleHeurFilter = ("no other heuristics", (fun (mr:ResultAnalysis.ModelResults)  -> countActiveHeuristics mr = 0))
    let onlyHeurFilter heur = 
        (@"only \textsf{" + heur.Code + "}", (fun (mr:ResultAnalysis.ModelResults) ->
                let settings = getSettings mr
                let (on,off) = heur.Activator settings
                countActiveHeuristicsB off = 0 && (off |> heur.Activator |> fst) = Some(settings)
            ) 
        )
    let singleOrAnythingFilters = [
        ("all results", (fun (mr:ResultAnalysis.ModelResults) -> true));
        singleHeurFilter;
        ("1ppc, no other heuristics", (fun (mr:ResultAnalysis.ModelResults)  -> countActiveHeuristics mr = 0 && mr.ModelSettings.PrototypesPerClass = 1))
        ("5ppc, no other heuristics", (fun (mr:ResultAnalysis.ModelResults)  -> countActiveHeuristics mr = 0 && mr.ModelSettings.PrototypesPerClass = 5))
        normOnlyFilter;
        (@"1ppc, only \textsf{normalize}", (fun (mr:ResultAnalysis.ModelResults)  -> mr.DatasetTweaks.Contains('n') && countActiveHeuristics mr = 1 && mr.ModelSettings.PrototypesPerClass = 1))
        (@"5ppc, only \textsf{normalize}", (fun (mr:ResultAnalysis.ModelResults)  -> mr.DatasetTweaks.Contains('n') && countActiveHeuristics mr = 1 && mr.ModelSettings.PrototypesPerClass = 5))
        ]
    let modelFilters = [
        ("GM 1", (fun (mr:ResultAnalysis.ModelResults) -> mr.ModelSettings.ModelType = LvqModelType.Gm && mr.ModelSettings.PrototypesPerClass = 1));
        ("G2M 1", (fun mr -> mr.ModelSettings.ModelType = LvqModelType.G2m && mr.ModelSettings.PrototypesPerClass = 1));
        ("GGM 1", (fun (mr:ResultAnalysis.ModelResults) -> mr.ModelSettings.ModelType = LvqModelType.Ggm && mr.ModelSettings.PrototypesPerClass = 1));
        ("GM 5", (fun mr -> mr.ModelSettings.ModelType = LvqModelType.Gm && mr.ModelSettings.PrototypesPerClass = 5));
        ("G2M 5", (fun mr -> mr.ModelSettings.ModelType = LvqModelType.G2m && mr.ModelSettings.PrototypesPerClass = 5));
        ("GGM,5", (fun mr -> mr.ModelSettings.ModelType = LvqModelType.Ggm && mr.ModelSettings.PrototypesPerClass = 5));
        ]
    let datasetFilters = 
        Seq.toList resultsByDatasetByModel.Keys
            |> List.filter (fun key -> resultsByDatasetByModel.[key].Count > 65)
            |> List.map (fun datasetKey ->(defaultArg (ResultAnalysis.friendlyDatasetName datasetKey) datasetKey, (fun (mr:ResultAnalysis.ModelResults)  ->mr.DatasetBaseShorthand = datasetKey)))
    
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
    

let analysisPairsGiven (filter:ResultAnalysis.ModelResults -> bool) (heur:Heuristic) = 
    seq {
        for datasetRes in resultsByDatasetByModel.Values do
            for modelRes in datasetRes.Values do
                if filter modelRes then
                    match optCompare datasetRes modelRes heur with
                    | None -> ()
                    | Some(comparison) -> yield comparison
    }



let analysisGiven filter heur = analysisPairsGiven filter heur |> Seq.map compare

let getTrainingError (r:ResultAnalysis.Result) =  r.TrainingError
let curry f x y = f (x, y)
let uncurry f (x, y) = f x y
let uncurry3 f (x, y, z) = f x y z

@"<!DOCTYPE html>
<html><head>
<style type=""text/css"">
  table { border-collapse:collapse; border-bottom: 2px solid #666;border-top: 2px solid #666;}
  tr { border-top: 1px solid #888;}
  tr.noborder {border-top:none;}
  th { background: #eee;  text-align:left; font-weight:normal; }
  th:first-child { min-width:11em; }
  td { text-align:right; padding:0 0.2em; white-space:nowrap;}
  td:first-child, td:first-child ~ td:nth-child(2n - 1), th:first-child ~ td:nth-child(2n) {  border-left:1px solid black;  }
  body { font-family: Calibri, Sans-serif; }
  .slightlybetter {background: rgba(96, 192, 255, 0.2);}
  .slightlyworse {background: rgba(255, 128, 128, 0.2);}
  .better {background: rgba(96, 192, 255, 0.5);}
  .muchbetter {background: rgba(96, 192, 255, 0.8);}
  .worse {background: rgba(255, 128, 128, 0.5);}
  .muchworse {background: rgba(255, 128, 128, 0.8);}
  div {padding:0 0.2em;}
</style>
</head><body>" +
"<table><thead><tr><th class=\"head\"><div style=\"text-align:right;\">Heuristic</div>Assumption</th><th colspan=\"2\">" + (heuristics |> List.map (fun heur->heur.Code) |> String.concat " </th><th colspan=\"2\"> ") + "</th></tr></thead><tbody>" +
    (allFilters |> List.map (fun (filtername, filter) -> 
            "<tr><th rowspan=\"2\">" + filtername + " </td> " + 
            (heuristics |> List.map 
                (fun heur ->
                    let rawAnalysis =analysisPairsGiven filter heur |> List.ofSeq
                    let analysis = rawAnalysis |> List.map (Utils.apply2 (fun mr -> mr.Results |> Array.map (fun  (r:ResultAnalysis.Result)->(r.TrainingError,r.TestError)) |> List.ofArray))
                    
                    if List.isEmpty analysis |> not then
                        let totalResCount =  analysis |> List.length
                        let pairsOfTrnTst_2_pairOfTrnsTsts x = x |> List.map (fun ((trnA,tstA),(trnB,tstB)) -> ((trnA,trnB),(tstA,tstB) )) |> List.unzip
                        let classifyP (better, p) = 
                            if p = 1. then
                                ""
                            else
                                if p > 0.05 then "slightly" else if p > 0.01 then "" else "much"
                                +
                                if better then "better" else "worse"
                        
                        let stringifyErrPatterns errs = 
                            let count = errs |> snd |> List.length
                            let (beforeTrnDistrib, beforeTstDistrib) = errs |> Utils.apply2 (List.map fst >> List.average >> (*) 100.)
                            let (trnDistrib, tstDistrib) = errs |> Utils.apply2 (List.map snd >> List.average >> (*) 100.)
                            let errsChange = errs |> Utils.apply2 (List.map comparisonErrChange)
                            let (trnErrsChange, tstErrsChange) = errsChange |> Utils.apply2 List.average
                            let (trnErrsChangeP,tstErrsChangeP) = errsChange |> Utils.apply2 Utils.twoTailedOneSampleTtest
                            sprintf @"<td class=""%s"">%.1f &rarr; %.1f<br/>[%i] %.1f</td><td class=""%s"">%.1f &rarr; %.1f<br/>[%i] %.1f</td>" (classifyP trnErrsChangeP)  beforeTrnDistrib trnDistrib count trnErrsChange (classifyP tstErrsChangeP) beforeTstDistrib tstDistrib count tstErrsChange
                        let allErrs = 
                            analysis 
                                |> List.map (Utils.apply2 List.unzip) 
                                |> pairsOfTrnTst_2_pairOfTrnsTsts
                                |> Utils.apply2 (List.map (uncurry List.zip) >> List.concat)
                        
                        let meanErrs = analysis |> List.map (Utils.apply2 (List.unzip >> Utils.apply2 List.average)) |> pairsOfTrnTst_2_pairOfTrnsTsts // Array.ofList>> EmnExtensions.Algorithms.SelectionAlgorithm.Median)) //List.zip allBaseErrs allHeurErrs

                        let bestErrs = analysis |> List.map (Utils.apply2 List.min) |> pairsOfTrnTst_2_pairOfTrnsTsts
                        
                        (stringifyErrPatterns allErrs, stringifyErrPatterns bestErrs)
                    else 
                        ("<td></td><td></td>", "<td></td><td></td>")
                ) |> List.unzip 
                |> Utils.apply2 (String.concat "")
                |> (fun (allErrRow,bestErrRow) -> allErrRow + "</tr><tr class=\"noborder\">" + bestErrRow)
            ) + "</tr>\n" 
        ) |> String.concat ""
    )
    +  "</tbody></table>"
+ "</body></html>"
|> (fun contents -> File.WriteAllText(EmnExtensions.Filesystem.FSUtil.FindDataDir(@"uni\Thesis\doc", System.Reflection.Assembly.GetAssembly(typeof<CreateDataset>)).FullName + @"\compare.html", contents))

let constF x _ = x
let classifyPtex (better, p) = 
    let slightlybetterC = "0.875,0.95,1"
    let betterC = "0.6875,0.875,1"
    let muchbetterC = "0.5,0.8,1"
    let slightlyworseC = "1,0.9,0.9"
    let worseC = "1, 0.75, 0.75"
    let muchworseC = "1, 0.6, 0.6"
    let blankC = "1,1,1"
    match (better, p) with
    | (_, 1.) -> blankC
    | (true, p) when p > 0.05 -> slightlybetterC
    | (true, p) when p > 0.01 -> betterC
    | (true, _) -> muchbetterC
    | (false, p) when p > 0.05 -> slightlyworseC
    | (false, p) when p > 0.01 -> worseC
    | (false, _) -> muchworseC


let latexCompareHeurs = 
    let subSetSelection (name:string) = not (name.Contains("+") || name = "S")
    let filterSelection = fst>>subSetSelection
    let heurSelection = (fun heur -> heur.Code) >> subSetSelection

    let heuristics = heuristics |> List.filter heurSelection
    let allFilters = allFilters |> List.filter filterSelection

    let strconcat xs = String.concat "" xs
    let coldef = heuristics |> List.map (constF "@{}>{\columncolor{white}[0mm][1mm]}r@{\hspace{1mm}}@{}>{\columncolor{white}[0mm][1mm]}r@{\hspace{1mm}}") |> String.concat "|"
    let headerrow = (heuristics |> List.map (fun heur-> sprintf @"& \multicolumn{2}{c}{\multirow{2}{*}{\textsf{%s}}}" heur.Code) |> strconcat)

    let mainbody = 
        let nicerFilters = allFilters |> List.map (Utils.apply1st (fun name -> name.Split([|" * "|], StringSplitOptions.None))) |> List.filter (fst >> (fun names->names.Length < 3))
        let rows = 
            [
            for (filternames, filter) in nicerFilters ->
                let (rowtitle1, rowtitle2) = 
                    match filternames with
                    | [|n1;n2|] -> (sprintf @"\multicolumn{2}{c}{%s}" n1,sprintf @"\multicolumn{2}{c}{%s}" n2)
                    | [|n1|] -> (sprintf @"\multicolumn{2}{c}{\multirow{2}{*}{%s}}" n1,"&")
                    | _ -> failwith "Illegal filter names!"
                let (allErrRow,bestErrRow) = 
                    [
                        for heur in heuristics ->
                            let rawAnalysis =analysisPairsGiven filter heur |> List.ofSeq
                            let analysis = rawAnalysis |> List.map (Utils.apply2 (fun mr -> mr.Results |> Array.map (fun  (r:ResultAnalysis.Result)->(r.TrainingError,r.TestError)) |> List.ofArray))
                    
                            if List.isEmpty analysis |> not then
                                let totalResCount =  analysis |> List.length
                                let pairsOfTrnTst_2_pairOfTrnsTsts x = x |> List.map (fun ((trnA,tstA),(trnB,tstB)) -> ((trnA,trnB),(tstA,tstB) )) |> List.unzip
                        
                                let stringifyErrPatterns errs = 
                                    let errsChange = errs |> Utils.apply2 (List.map comparisonErrChange)
                                    let (trnErrsChange, tstErrsChange) = errsChange |> Utils.apply2 List.average
                                    let (trnErrsChangeP,tstErrsChangeP) = errsChange |> Utils.apply2 Utils.twoTailedOneSampleTtest
                                    sprintf @"&\cellcolor[rgb]{%s}$%.1f$ &\cellcolor[rgb]{%s}$%.1f$" (classifyPtex trnErrsChangeP) trnErrsChange (classifyPtex tstErrsChangeP) tstErrsChange
                                let allErrs = 
                                    analysis 
                                        |> List.map (Utils.apply2 List.unzip) 
                                        |> pairsOfTrnTst_2_pairOfTrnsTsts
                                        |> Utils.apply2 (List.map (uncurry List.zip) >> List.concat)
                        
                                let meanErrs = analysis |> List.map (Utils.apply2 (List.unzip >> Utils.apply2 List.average)) |> pairsOfTrnTst_2_pairOfTrnsTsts // Array.ofList>> EmnExtensions.Algorithms.SelectionAlgorithm.Median)) //List.zip allBaseErrs allHeurErrs

                                let bestErrs = analysis |> List.map (Utils.apply2 List.min) |> pairsOfTrnTst_2_pairOfTrnsTsts
                        
                                (stringifyErrPatterns allErrs, stringifyErrPatterns bestErrs)
                            else 
                                (" & & ", " & & ")
                    ] |> List.unzip 
                    |> Utils.apply2 (String.concat "")
                rowtitle1 +  allErrRow + @"\\" + "\n" + rowtitle2 + bestErrRow  + @"\\" + "\n" 
            ]
        rows |> String.concat "\hline "
    @"\noindent\begin{longtable}{lr" + coldef + @"}\toprule 
     & Heuristic " + headerrow + @"\\
     Scenario\\\midrule
     " + mainbody + @"
     \bottomrule\end{longtable}"


File.WriteAllText(EmnExtensions.Filesystem.FSUtil.FindDataDir(@"uni\Thesis\doc", System.Reflection.Assembly.GetAssembly(typeof<CreateDataset>)).FullName + @"\compare.tex", latexCompareHeurs )

let analysisRawFor (scenarioSettings:HeuristicsSettings) (heur:Heuristic) = 
    let (heurSettings,shouldBescenarioSettings) = heur.Activator scenarioSettings
    [
        if  heur.Code = "none" || (shouldBescenarioSettings = scenarioSettings && heurSettings <> Some(scenarioSettings)) then
            for datasetRes in resultsByDatasetByModel.Values do
                let maybeBaseRes = Utils.getMaybe datasetRes scenarioSettings
                let maybeHeurRes = Option.bind (Utils.getMaybe datasetRes) heurSettings
                match (maybeBaseRes, maybeHeurRes) with
                | (Some baseRes, Some heurRes) ->
                    yield (baseRes, heurRes)
                | _ -> ()
    ]

let latexHeurRaws = 
    let scenarios =
        [
            for protos in [1;5] do
                for modeltype in [LvqModelType.Gm;LvqModelType.G2m;LvqModelType.Ggm] do
                    let mutable mutablesettings = new LvqModelSettingsCli()
                    mutablesettings.PrototypesPerClass <- protos
                    mutablesettings.ModelType <- modeltype
                    let modelsettings = mutablesettings
                    for datasetSettings in [""; "n"] do
                        let scenarioName = (modeltype.ToString ()).ToUpper () + " " + protos.ToString () + (if datasetSettings = "n" then @" \textsf{normalize}" else "")
                        let heursettings=
                            { 
                                DataSettings = datasetSettings
                                ModelSettings = modelsettings
                            }
                        yield (scenarioName, heursettings)
        ]
    let subSetSelection (name:string) = not (name.Contains("+") || name = "S" || name = "normalize")
    let heurSelection = (fun heur -> heur.Code) >> subSetSelection

    let relevantHeuristics = List.Cons ({ Name="base"; Code="none"; Activator = (fun s->(Some(s),s)); }, heuristics |> List.filter heurSelection)

    let strconcat xs = String.concat "" xs
    let coldef = relevantHeuristics |> List.map (constF ("@{}>{\columncolor{white}[0mm][1mm]}r@{\hspace{1mm}}" |> String.replicate 1)) |> String.concat "|"
    let headerrow = (relevantHeuristics |> List.map (fun heur-> sprintf @"& \multicolumn{1}{@{\hspace{1mm}}r@{\hspace{1mm}}}{\textsf{%s}}" heur.Code) |> strconcat)

    let mainbody = 
        let rows = 
            [
                for (scenarioName, scenarioSettings) in scenarios ->
                    let (trnAnalysis, testAnalysis, nnAnalysis) =
                        [
                            for heur in relevantHeuristics -> 
                                let rawAnalysis = 
                                    if heur.Code = "none" && scenarioSettings.DataSettings = "n" then
                                        analysisRawFor {DataSettings = ""; ModelSettings = scenarioSettings.ModelSettings} (List.head heuristics)
                                    else
                                        analysisRawFor scenarioSettings heur

                                if List.isEmpty rawAnalysis |> not then
                                    let ((trnA, tstA, nnA), (trnB, tstB, nnB)) = 
                                        rawAnalysis 
                                        |> List.map (Utils.apply2 (fun mr -> mr.Results |> Array.map (fun  (r:ResultAnalysis.Result)->(r.TrainingError,r.TestError, r.NnError)) |> List.ofArray))
                                        |> List.unzip
                                        |> Utils.apply2 (List.concat >>List.unzip3)

                                    let totalResCount =  rawAnalysis |> List.length

                                    let stringifyErrPatterns errsA errsB =
                                        let comparison = comparisonP (errsA, errsB)
                                        let meanErr = 100. * List.average errsB
                                        sprintf @"&\cellcolor[rgb]{%s}$%.1f$" (classifyPtex comparison) meanErr
                        
                                    (stringifyErrPatterns trnA trnB, stringifyErrPatterns tstA tstB, stringifyErrPatterns nnA nnB)
                                else 
                                    ("&","&","&")
                        ] |> List.unzip3 |> Utils.apply3 strconcat
                    sprintf @"\multirow{3}{*}{%s} &$\!\!\!\!\!$training: %s\\ & test: %s\\& NN: %s\\" scenarioName trnAnalysis testAnalysis nnAnalysis
            ] 
        rows |> String.concat @"\hline"
    
    @"\noindent\begin{longtable}{l@{}@{}r@{\hspace{1mm}}" + coldef + @"}\toprule 
     Scenario &" + headerrow + @"\\\midrule
     " + mainbody + @"
     \bottomrule\end{longtable}"


File.WriteAllText(EmnExtensions.Filesystem.FSUtil.FindDataDir(@"uni\Thesis\doc", System.Reflection.Assembly.GetAssembly(typeof<CreateDataset>)).FullName + @"\G2MLVQ_rawresults.tex", latexHeurRaws)



heuristics
    |> Seq.map (fun heur -> 
        analysisGiven (fun _ -> true) heur
        |> Utils.toDict (fst>>fst) (Seq.sortBy snd >> Seq.toArray)
        |> (fun dict -> (defaultArg (Utils.getMaybe dict Better) Array.empty,  defaultArg (Utils.getMaybe dict Worse) Array.empty, defaultArg (Utils.getMaybe dict Irrelevant) Array.empty)) 
        |> (fun (better, worse, irrelevant) ->
            (heur, (better.Length + worse.Length, irrelevant.Length, float better.Length / float (better.Length + worse.Length)), (better, worse,irrelevant))
        )
    )
    |> Seq.toList
    |> List.map (fun (heur, (count, ignoreCount,ratio), (better, worse,irrelevant)) ->
            sprintf @"\section{%s} \noindent %s was an improvement in $%1.1f\%%$ of %i cases and irrelevant in %i:" (ResultAnalysis.latexLiteral heur.Code) heur.Name (100.*ratio) count ignoreCount + "\n\n"
            + sprintf @"\noindent\begin{longtable}{lrccl@{}r}\toprule"  + "\n"
            + sprintf @"$p$-value & $\Delta\%%$ &\multicolumn{1}{c}{before}&\multicolumn{1}{c}{after}  & \multicolumn{2}{c}{Scenario} \\\midrule"  + "\n"
            + @"&&\multicolumn{2}{c}{Improved} \\ \cmidrule(r){3-4}" + "\n"
            + String.concat "\\\\\n" (Array.map (fun ( (difference,scenarioLatex),  ((p,  bothDistribs),((errChange, bestErrChange), (betterRatio, resCount))) ) -> sprintf @" %0.2g & %0.1f &%s&%s&%s" p errChange (bothDistribs|>fst|> Utils.latexstderr ) (bothDistribs|>snd|> Utils.latexstderr ) scenarioLatex) better)
            + @"\\\midrule" + "\n"
            + @"&&\multicolumn{2}{c}{Degraded} \\ \cmidrule(r){3-4}" + "\n"
            + String.concat "\\\\\n" (Array.map (fun ( (difference,scenarioLatex),  ((p,  bothDistribs),((errChange, bestErrChange), (betterRatio, resCount))) ) -> sprintf @" %0.2g & %0.1f &%s&%s&%s" p errChange (bothDistribs|>fst|> Utils.latexstderr ) (bothDistribs|>snd|> Utils.latexstderr ) scenarioLatex) worse)
            + "\n" + @"\\ \bottomrule\end{longtable}" + "\n\n" 
        )
    |> String.concat ""
    |> (fun contents -> File.WriteAllText(EmnExtensions.Filesystem.FSUtil.FindDataDir(@"uni\Thesis\doc", System.Reflection.Assembly.GetAssembly(typeof<CreateDataset>)).FullName + @"\AnalyzeHeuristics.tex", contents))

