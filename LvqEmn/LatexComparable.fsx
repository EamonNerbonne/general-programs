#I @"ResultsAnalysis\bin\Release"
#r "ResultsAnalysis"
#r "LvqLibCli"
#r "LvqGui"

open GeneralHelpers
open LatexifyCompareMethods
open LvqLibCli


let allResults = ResultParsing.loadAllResults "base"

let heuristicsCompared=
    heuristics
    |> List.map (fun (baseHeuristicSettings, name) -> latexifyCompareMethods name allResults (relevantVariants baseHeuristicSettings))  
    |> String.concat "\n\n"


ErrorCorrelations.initCorrs allResults (variants |> List.map (fun (variant, varName) -> (LvqModelSettingsCli() |> variant, varName )) ) |>printfn "%A"


alltypes |> List.map (ErrorCorrelations.errTypeCorrelationLatex allResults) |> String.concat "\\\\\n" 


(*


String.Join("\\\\\n", alltypes|>List.map fst |> List.map (errTypeCorrelationLatex curDatasetName)) |> printfn "%s"

initCorrs curDatasetName alltypes|>printfn "%A"

initCorrs curDatasetName alltypes |> List.averageBy (snd >> fst) |> printfn "%A"

alltypes  |> List.map fst |> List.filter (fun settings -> settings.ModelType <> LvqModelType.Lgm)
    |> List.map (loadResultsByLr curDatasetName >> List.map (snd >> List.map (fun err->err.training))) 
            //list of model types, each has a list of LRs, each has a list of results in file order
    |> List.concat  //list of model types+lrs, each has a list of results in file order
    |> corrs //a list of correlations between differing types/lrs over changing initialization
    |> meanStderr //mean correlation between differing types/lrs over changing initialization
    |> printfn "%A"

*)