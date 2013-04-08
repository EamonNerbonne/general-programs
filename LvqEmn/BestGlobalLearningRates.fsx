#I @"ResultsAnalysis\bin\ReleaseMingw2"
#r "ResultsAnalysis"
#r "LvqLibCli"
#r "LvqGui"
#r "EmnExtensions"
#r "PresentationCore"
#r "WindowsBase"
#r "EmnExtensionsWpf"
#r "FSharp.PowerPack" 
#time "on"

open OptimalLrSearch

let defaultStore = "uniform-results.txt"
allUniformResults defaultStore
    |> List.sortBy (fun res->res.GeoMean)
    |> Seq.distinctBy (fun res-> res.Settings.WithCanonicalizedDefaults()) |> Seq.toList
    //|> List.filter (fun res->res.Settings.ModelType = LvqModelType.G2m)
    |> List.map printMeanResults
    |> List.iter (printfn "%s")
    
    // |> List.iter (fun line -> File.AppendAllText (LrOptimizer.resultsDir.FullName + "\\uniform-results-orig.txt",line + "\n"))



