#I @"ResultsAnalysis\bin\ReleaseMingw"
#r "ResultsAnalysis"
#r "LvqLibCli"
#r "LvqGui"

open LvqLibCli
open LvqGui    



let allResults = ResultParsing.loadAllResults "base"
let tenResults = ResultParsing.onlyFirst10results allResults


List.map (LatexifyCompareMethods.latexifyCompareHeuristic tenResults) GeneralSettings.heuristics |> String.concat "\n\n" |> printfn "%s"

LatexifyResults.latexifyConfusable "base" allResults GeneralSettings.allTypesWithName |> printfn "%s"
LatexifyResults.latexifyLrRelevanceConfusable "base" allResults GeneralSettings.allTypesWithName |> printfn "%s"

ErrorCorrelations.initCorrs allResults GeneralSettings.basicTypes

ErrorCorrelations.meanInitCorrs allResults GeneralSettings.basicTypes

ErrorCorrelations.errTypeCorrTableLatex allResults false GeneralSettings.basicTypes


