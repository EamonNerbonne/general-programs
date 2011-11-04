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
        (*heurM @"Optimizing $P$ and seting $B_i$ to the local covariance" "Pi+,Bi+" (fun s  -> 
            let mutable on = s
            let mutable off = s
            on.BLocalInit <- true
            off.BLocalInit <- false
            on.ProjOptimalInit <- true
            off.ProjOptimalInit <- false
            (on, off))
        heurM @"Optimizing $P$, setting $B_i$ to the local covariance, and initially using a lower learning rate for incorrect prototypes" "Pi+,Bi+,!" (fun s  -> 
            let mutable on = s
            let mutable off = s
            on.BLocalInit <- true
            off.BLocalInit <- false
            on.ProjOptimalInit <- true
            off.ProjOptimalInit <- false
            on.SlowStartLrBad <- true
            off.SlowStartLrBad <- false
            (on, off))
        heurM @"Neural gas prototype initialization followed by $P$ optimization" "NGi+,Pi+" (fun s  -> 
            let mutable on = s
            let mutable off = s
            on.NgInitializeProtos <- true
            off.NgInitializeProtos <- false
            on.ProjOptimalInit <- true
            off.ProjOptimalInit <- false
            (on, off))
        heurM @"$P$ optimization and neural gas-like updates" "NG+,Pi+" (fun s  -> 
            let mutable on = s
            let mutable off = s
            on.NgUpdateProtos <- true
            off.NgUpdateProtos <- false
            on.ProjOptimalInit <- true
            off.ProjOptimalInit <- false
            (on, off))
        heurM @"Neural gas prototype initialization and initially using lower learning rates for incorrect prototypes" "NGi+,!" (fun s  -> 
            let mutable on = s
            let mutable off = s
            on.NgInitializeProtos <- true
            off.NgInitializeProtos <- false
            on.SlowStartLrBad <- true
            off.SlowStartLrBad <- false
            (on, off))
        heurM @"Neural gas-like updates and initially using lower learning rates for incorrect prototypes" "NG+,!" (fun s  -> 
            let mutable on = s
            let mutable off = s
            on.NgUpdateProtos <- true
            off.NgUpdateProtos <- false
            on.SlowStartLrBad <- true
            off.SlowStartLrBad <- false
            (on, off))
        heurM @"Neural gas prototype initialization and neural gas-like updates" "NGi+,NG+" (fun s  -> 
            let mutable on = s
            let mutable off = s
            on.NgInitializeProtos <- true
            off.NgInitializeProtos <- false
            on.NgUpdateProtos <- true
            off.NgUpdateProtos <- false
            (on, off))
        heurM @"Neural gas prototype initialization, neural gas-like updates and $P$ initialization by PCA" "NGi+,NG+,rP" (fun s  -> 
            let mutable on = s
            let mutable off = s
            on.NgInitializeProtos <- true
            off.NgInitializeProtos <- false
            on.RandomInitialProjection <- false
            off.RandomInitialProjection <- true
            on.NgUpdateProtos <- true
            off.NgUpdateProtos <- false
            (on, off))
        heurM @"Neural gas prototype initialization, neural gas-like updates, $P$ initialization by PCA and  initially using lower learning rates for incorrect prototypes" "NGi+,NG+,rP,!" (fun s  -> 
            let mutable on = s
            let mutable off = s
            on.NgInitializeProtos <- true
            off.NgInitializeProtos <- false
            on.RandomInitialProjection <- false
            off.RandomInitialProjection <- true
            on.NgUpdateProtos <- true
            off.NgUpdateProtos <- false
            on.SlowStartLrBad <- true
            off.SlowStartLrBad <- false
            (on, off))*)
        heurD "Extend dataset by correlations (x)" "x"
        heurD "Normalize each dimension (n)" "n"
        heurD "pre-normalized segmentation dataset (N)" "N"
    ]

type Difference = 
    | Better
    | Worse
    | Irrelevant



let comparisonP  (baseErrs, heurErrs) = Utils.twoTailedPairedTtest heurErrs baseErrs
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
    |> Option.map (fun settingsWithHeuristic -> settingsWithHeuristic.Key)
    |> Option.bind (Utils.getMaybe datasetResults)
    |> Option.map (fun (heuristicResults:ResultAnalysis.ModelResults) -> (modelResults, heuristicResults))


let resultsByDatasetByModel =
    ResultAnalysis.analyzedModels () 
        |> Utils.toDict (fun modelRes -> modelRes.DatasetBaseShorthand) 
                (Utils.toDict 
                    (fun modelRes -> (getSettings modelRes).Key) 
                    (fun modelRess -> 
                        match Seq.toArray modelRess with
                        | [| modelRes |] -> modelRes
                        | modelResArr -> failwith (sprintf "whoops: %A" modelResArr)
                    )
                )


let countActiveHeuristics (mr:ResultAnalysis.ModelResults) =
    let ms = mr.ModelSettings
    let modelHeurs = [ms.BLocalInit; ms.NgInitializeProtos;  ms.NgUpdateProtos; ms.ProjOptimalInit; not ms.RandomInitialProjection; ms.SlowStartLrBad;  ms.UpdatePointsWithoutB] |> List.filter id |> List.length
    modelHeurs + mr.DatasetTweaks.Length

let allFilters = 
    let simplifyName (name:string) = if name.Contains("-") then name.Substring(0, name.IndexOf("-")) else name
    let singleOrAnythingFilters = [
        ("No other heuristics", (fun mr -> countActiveHeuristics mr < 2));
        ("Incl. other heuristics", (fun (mr:ResultAnalysis.ModelResults) -> true));
        ]
    let modelFilters = [
        ("Ggm,1", (fun (mr:ResultAnalysis.ModelResults) -> mr.ModelSettings.ModelType = LvqModelType.Ggm && mr.ModelSettings.PrototypesPerClass = 1));
        ("G2m,1", (fun mr -> mr.ModelSettings.ModelType = LvqModelType.G2m && mr.ModelSettings.PrototypesPerClass = 1));
        ("Gm,1", (fun mr -> mr.ModelSettings.ModelType = LvqModelType.Gm && mr.ModelSettings.PrototypesPerClass = 1));
        ("Ggm,5", (fun mr -> mr.ModelSettings.ModelType = LvqModelType.Ggm && mr.ModelSettings.PrototypesPerClass = 5));
        ("G2m,5", (fun mr -> mr.ModelSettings.ModelType = LvqModelType.G2m && mr.ModelSettings.PrototypesPerClass = 5));
        ("Gm,5", (fun mr -> mr.ModelSettings.ModelType = LvqModelType.Gm && mr.ModelSettings.PrototypesPerClass = 5));
        ]
    let singleOrEverythingModelFilters =
        [for (name, filter) in singleOrAnythingFilters do
            yield (name, filter)
            for (name2, filter2) in modelFilters do
                yield ((if name = "" then name2 else name + ": " + name2), fun x -> filter x && filter2 x)
        ]        
    List.append singleOrEverythingModelFilters (
        heuristics |> List.map (fun heur ->  (heur.Code, getSettings >> isHeuristicApplied heur) )
        |> List.append (
            Seq.toList resultsByDatasetByModel.Keys
            |> List.filter (fun key -> resultsByDatasetByModel.[key].Count > 65)
            |> List.map (fun datasetKey ->(defaultArg (ResultAnalysis.friendlyDatasetName datasetKey) datasetKey, (fun modelRes->modelRes.DatasetBaseShorthand = datasetKey)))
        )
    )

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

@"<!DOCTYPE html>
<html><head>
<style type=""text/css"">
  table { border-collapse:collapse; border-bottom: 2px solid #666;border-top: 2px solid #666;}
  td { white-space: nowrap; border-bottom:1px solid #888;}
  td:nth-child(2n+1) { background: #ddd; }
  body { font-family: Calibri, Sans-serif; }
  .better {background: rgba(96, 192, 255, 0.3);}
  .muchbetter {background: rgba(96, 192, 255, 0.8);}
  .worse {background: rgba(255, 128, 128, 0.3);}
  .muchworse {background: rgba(255, 128, 128, 0.8);}
</style>
</head><body>" +
"<table><thead><tr><td>within</td><td>" + (heuristics |> List.map (fun heur->heur.Code) |> String.concat " </td><td> ") + "</td></tr></thead><tbody>" +
    (allFilters |> List.map (fun (filtername, filter) -> 
            "<tr><td>" + filtername + " </td><td> " + 
            (heuristics |> List.map 
                (fun heur ->
                    let analysis = analysisPairsGiven filter heur |> Seq.map (Utils.apply2 (fun mr -> mr.Results |> Array.map getTrainingError |> List.ofArray)) |> Seq.toList
                    if List.isEmpty analysis |> not then
//                        let allBaseErrs = List.collect fst analysis
//                        let allHeurErrs = List.collect snd analysis
//                        let bothErrs = (allBaseErrs, allHeurErrs)
//                        let changeRatio = comparisonBetterRatio bothErrs
                        let totalResCount =  analysis |> List.length

                        let medianErrs = analysis |> List.map (Utils.apply2 (Array.ofList>> EmnExtensions.Algorithms.SelectionAlgorithm.Median)) //List.zip allBaseErrs allHeurErrs
                        let medianErrsChange = medianErrs |> List.averageBy comparisonErrChange
                        let medianErrsChangeRatio = (medianErrs |> List.unzip |> comparisonBetterRatio) * 100.
                        let medianErrsChangeP = medianErrs |> List.unzip |> comparisonP

                        let bestErrs = analysis |> List.map (Utils.apply2 List.min)
                        let bestErrsChange =  bestErrs |> List.averageBy comparisonErrChange
                        let bestErrsChangeRatio = (bestErrs |> List.unzip |> comparisonBetterRatio )*100.
                        let bestErrsChangeP = bestErrs |> List.unzip |> comparisonP

                        let classifyP (better, p) = 
                            if p > 0.05 then ""
                            else 
                                if p > 0.01 then "" else "much"
                                +
                                if better then "better" else "worse"

                        sprintf "<div class=\"%s\">%.1f%%; %.2f</div> <div class=\"%s\">%.1f%%; %.2f</div>" (classifyP medianErrsChangeP) medianErrsChangeRatio medianErrsChange (classifyP bestErrsChangeP) bestErrsChangeRatio bestErrsChange
                    else 
                        ""
                ) |> String.concat " </td><td> "
            ) + "</td></tr>\n" 
        ) |> String.concat ""
    )
    +  "</tbody></table>"
+ "</body></html>"
|> (fun contents -> File.WriteAllText(EmnExtensions.Filesystem.FSUtil.FindDataDir(@"uni\Thesis\doc", System.Reflection.Assembly.GetAssembly(typeof<CreateDataset>)).FullName + @"\compare.html", contents))

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
//            + @"\\\midrule" + "\n"
            //+ @"&&\multicolumn{2}{c}{Irrelevant} \\ \cmidrule(r){3-4}" + "\n"
            //+ String.concat "\\\\\n" (Array.map (fun (p, bestErrChange,errChange,before,after, scenario, betterRatio, resCount) -> sprintf @" %0.2g & %0.1f &%s&%s&%s" p errChange (Utils.latexstderr before) (Utils.latexstderr after) scenario) irrelevant)
            + "\n" + @"\\ \bottomrule\end{longtable}" + "\n\n" 
        )
    |> String.concat ""
    |> (fun contents -> File.WriteAllText(EmnExtensions.Filesystem.FSUtil.FindDataDir(@"uni\Thesis\doc", System.Reflection.Assembly.GetAssembly(typeof<CreateDataset>)).FullName + @"\AnalyzeHeuristics.tex", contents))

(*
heuristics |> List.map (fun heur ->
        

    )

    *)

    