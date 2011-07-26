#I @"ResultsAnalysis\bin\Release"
#r "ResultsAnalysis"
#r "LvqLibCli"
#r "LvqGui"

open LvqLibCli


let allResults = ResultParsing.loadAllResults "base"
let tenResults = ResultParsing.onlyFirst10results allResults


List.map (LatexifyCompareMethods.latexifyCompareHeuristic tenResults) GeneralSettings.heuristics |> String.concat "\n\n"

LatexifyResults.latexifyConfusable "base" allResults GeneralSettings.basicTypesWithName

ErrorCorrelations.initCorrs allResults GeneralSettings.basicTypes

ErrorCorrelations.meanInitCorrs allResults GeneralSettings.basicTypes

ErrorCorrelations.errTypeCorrTableLatex allResults false GeneralSettings.basicTypes
