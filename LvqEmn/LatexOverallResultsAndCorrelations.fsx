#I @"ResultsAnalysis\bin\ReleaseMingw2"
#r "ResultsAnalysis"
#r "LvqLibCli"
#r "LvqGui"

open LvqLibCli
open LvqGui    

let allResults = LrOptResults.loadDatasetLrOptResults "base"

LatexOverallErrorTables.lvqMethodsOptimalLrErrorsTable "base" allResults ModelSettings.allCoreModelSettings |> printfn "%s"

LatexOverallErrorTables.lvqMethodsNonOptimalLrErrorsTable "base" allResults ModelSettings.allCoreModelSettings |> printfn "%s"

ErrorCorrelations.initCorrs allResults ModelSettings.coreProjectingModelSettings

ErrorCorrelations.meanInitCorrs allResults ModelSettings.coreProjectingModelSettings

ErrorCorrelations.errTypeCorrTableLatex allResults false ModelSettings.allCoreModelSettings


