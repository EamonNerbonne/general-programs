﻿#I @"ResultsAnalysis\bin\ReleaseMingw2"
#r "ResultsAnalysis"
#r "LvqLibCli"
#r "LvqGui"

open LvqLibCli
open LvqGui    

let allResults = LrOptResults.loadDatasetLrOptResults "base"

LatexOverallErrorTables.lvqMethodsOptimalLrErrorsTable "base" allResults GeneralSettings.allTypes |> printfn "%s"

LatexOverallErrorTables.lvqMethodsNonOptimalLrErrorsTable "base" allResults GeneralSettings.allTypes |> printfn "%s"

ErrorCorrelations.initCorrs allResults GeneralSettings.basicTypes

ErrorCorrelations.meanInitCorrs allResults GeneralSettings.basicTypes

ErrorCorrelations.errTypeCorrTableLatex allResults false GeneralSettings.allTypes


