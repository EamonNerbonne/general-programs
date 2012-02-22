#I @"ResultsAnalysis\bin\ReleaseMingw2"
#r "ResultsAnalysis"
#r "LvqLibCli"
#r "LvqGui"
#r "EmnExtensions"
#time "on"

open LvqLibCli
open HeuristicAnalysis
open LvqGui
//open System.Threading
//open EmnExtensions.Text
//open EmnExtensions
open System.IO
//open System.Collections.Generic
//open System.Linq
open System

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
                    let analysis = rawAnalysis |> List.map (Utils.apply2 (fun mr -> mr.Results |> Array.map (fun  (r:LvqRunAnalysis.SingleLvqRunOutcome)->(r.TrainingError,r.TestError)) |> List.ofArray))
                    
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
                            let analysis = rawAnalysis |> List.map (Utils.apply2 (fun mr -> mr.Results |> Array.map (fun  (r:LvqRunAnalysis.SingleLvqRunOutcome)->(r.TrainingError,r.TestError)) |> List.ofArray))
                    
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
                                        |> List.map (Utils.apply2 (fun mr -> mr.Results |> Array.map (fun  (r:LvqRunAnalysis.SingleLvqRunOutcome)->(r.TrainingError,r.TestError, r.NnError)) |> List.ofArray))
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
            sprintf @"\section{%s} \noindent %s was an improvement in $%1.1f\%%$ of %i cases and irrelevant in %i:" (LvqRunAnalysis.latexLiteral heur.Code) heur.Name (100.*ratio) count ignoreCount + "\n\n"
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

