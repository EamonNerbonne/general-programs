#I @"ResultsAnalysis\bin\ReleaseMingw2"
#r "ResultsAnalysis"
#r "LvqLibCli"
#r "LvqGui"

open LvqLibCli
open LvqGui    



let allResults = LrOptResults.loadDatasetLrOptResults "base"
let tenResults = List.filter (fun (result:DatasetResults) ->result.unoptimizedSettings.InstanceSeed < 20u) allResults


List.map (LatexifyCompareMethods.latexifyCompareHeuristic tenResults) GeneralSettings.heuristics |> String.concat "\n\n" |> printfn "%s"

LatexifyResults.latexifyConfusable "base" allResults GeneralSettings.allTypesWithName |> printfn "%s"
LatexifyResults.latexifyLrRelevanceConfusable "base" allResults GeneralSettings.allTypesWithName |> printfn "%s"

ErrorCorrelations.initCorrs allResults GeneralSettings.basicTypes

ErrorCorrelations.meanInitCorrs allResults GeneralSettings.basicTypes

ErrorCorrelations.errTypeCorrTableLatex allResults false GeneralSettings.basicTypes


