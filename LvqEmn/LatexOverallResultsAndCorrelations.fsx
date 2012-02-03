#I @"ResultsAnalysis\bin\ReleaseMingw2"
#r "ResultsAnalysis"
#r "LvqLibCli"
#r "LvqGui"

open LvqLibCli
open LvqGui    

let allResults = LrOptResults.loadDatasetLrOptResults "base"

LatexOverallErrorTables.lvqMethodsOptimalLrErrorsTable "base" allResults ModelSettings.allCoreModelSettings |> printfn "%s"

LatexOverallErrorTables.lvqMethodsNonOptimalLrErrorsTable "base" allResults ModelSettings.allCoreModelSettings |> printfn "%s"

ErrorCorrelations.initCorrs allResults ModelSettings.coreProjectingModelSettings |> List.map (fun (corr,distr) -> sprintf "%s: %f ~ %f" corr distr.Mean distr.StdErr) |> String.concat "\n" |> printfn "%s"

ErrorCorrelations.meanInitCorrs allResults ModelSettings.coreProjectingModelSettings |> (fun distr -> sprintf "%f ~ %f" distr.Mean distr.StdErr) |> printfn "%s"

ErrorCorrelations.errTypeCorrTableLatex allResults false ModelSettings.allCoreModelSettings |> printfn "%s"

