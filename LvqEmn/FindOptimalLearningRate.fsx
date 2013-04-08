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

open LvqGui
open System.IO
open System.Linq
open LvqLibCli
open System
open Utils
open OptimalLrSearch

//Microsoft.FSharp.Collections.LazyList.ofList [1;2;3] |> Microsoft.FSharp.Collections.LazyList.toList


let defaultStore = "uniform-results.txt"
let newStore = "uniform-results-new.txt"
let tempStore = "uniform-results-tmp.txt"
let decayStore = "uniform-results-withDecay.txt"
let temp2Store = "uniform-results-tmp2.txt"
let optimizeSettingsList = 
        List.map (CreateLvqModelValues.ParseShorthand >> withDefaultLr) 
        >> Seq.distinctBy (fun s-> s.WithCanonicalizedDefaults())  >> Seq.toList
        >> List.rev
        >> Seq.filter (isTested defaultStore >> not) 
        >> Seq.filter (isTested newStore >> not) 
        >> Seq.map (improveAndTestWithControllers 0 1.0 allControllers newStore)
        >> Seq.toList
    
let heuristics=
    [
        (fun (s: LvqModelSettingsCli) -> 
            let mutable copy = s
            copy.Bcov <- true
            copy
            )
        (fun (s: LvqModelSettingsCli) -> 
            let mutable copy = s
            copy.LrPp <- true
            copy
            )
        (fun (s: LvqModelSettingsCli) -> 
            let mutable copy=s
            copy.LocallyNormalize <- true
            copy
            )
        (fun (s: LvqModelSettingsCli) -> 
            let mutable copy=s
            copy.NGi <- true
            copy
            )
        (fun (s: LvqModelSettingsCli) -> 
            let mutable copy=s
            copy.NGu <- true
            copy
            )
        (fun (s: LvqModelSettingsCli) -> 
            let mutable copy=s
            copy.Popt <- true
            copy
            )
        (fun (s: LvqModelSettingsCli) -> 
            let mutable copy=s
            copy.Ppca <- true
            copy
            )
        (fun (s: LvqModelSettingsCli) -> 
            let mutable copy=s
            copy.SlowK <- true
            copy
            )
        (fun (s: LvqModelSettingsCli) -> 
            let mutable copy=s
            copy.neiB <- true
            copy
            )
        (fun (s: LvqModelSettingsCli) -> 
            let mutable copy=s
            copy.neiP <- true
            copy
            )
        (fun (s: LvqModelSettingsCli) -> 
            let mutable copy=s
            copy.scP <- true
            copy
            )
        (fun (s: LvqModelSettingsCli) -> 
            let mutable copy=s
            copy.wGMu <- true
            copy
            )
        (fun (s: LvqModelSettingsCli) -> 
            let mutable copy=s
            if s.ModelType = LvqModelType.Gm then
                copy.noKP <- true
            copy
            )
        id
        ]
        |> List.map (fun f -> f>> (fun (s: LvqModelSettingsCli)->s.Canonicalize()))

//let basics = ["Ggm-1,";"Ggm-5,";"Gm-1,";"Gm-5,";"G2m-1,";"G2m-5,";"Gpq-1,";"Gpq-5,";"Lgm-1,";"Lgm-5,";"Lgm[6]-1,";"Lgm[6]-5,"] |> List.map CreateLvqModelValues.ParseShorthand
let basics = ["Ggm-1,";"Ggm-3,";"Ggm-5,";"Gm-1,";"Gm-3,";"Gm-5,";"G2m-1,";"G2m-3,";"G2m-5,";"Gpq-1,";"Gpq-3,";"Gpq-5,";
                        "Lgm-1,";"Lgm-3,";"Lgm-5,";"Lgm[6]-1,";"Lgm[6]-3,";"Lgm[6]-5,"
                        ]
                        |> List.map CreateLvqModelValues.ParseShorthand



let interestingSettings () = 
    basics 
        |> List.collect (fun s-> List.map (fun f-> f s) heuristics)
        |> Seq.distinct 
        |> Seq.collect (fun s-> List.map (fun f-> f s) heuristics)
        |> Seq.distinct 
        |> Seq.collect (fun s-> List.map (fun f-> f s) heuristics)
        |> Seq.distinct 
        |> Seq.collect (fun s-> List.map (fun f-> f s) heuristics)
        |> Seq.distinct |>Seq.toList
        |> List.filter (fun s->s.LikelyRefinementRanking() <4)
        |> List.filter (fun s-> not s.LrPp || s.ModelType = LvqModelType.Lgm)
        |> List.sortBy (fun s->s.LikelyRefinementRanking())
        |> Seq.filter (isTested defaultStore >> not) //seq is lazy, so this last minute rechecks availability of results.
        |> Seq.map withDefaultLr
        |> Seq.map (improveAndTestWithControllers 0 1.0 allControllers defaultStore)
        |> Seq.toList
        //|> List.map(fun s->s.ToShorthand())



let recomputeRes filename =
    allUniformResults filename 
        |> List.sortBy (fun res->res.GeoMean) 
       // |> List.rev
        |> Seq.distinctBy (fun res -> res.Settings.WithCanonicalizedDefaults()) |> Seq.toList
        |> List.map (fun res->res.Settings)
        |> List.filter (fun settings -> settings.ModelType = LvqModelType.G2m)
        |> List.map (OptimalLrSearch.finalTestSettings >> OptimalLrSearch.printResults >> (fun resline -> File.AppendAllText (LrOptimizer.resultsDir.FullName + "\\" + tempStore, resline + "\n"); resline ))





let showEffect filename removeRelevantSetting resultsFilter =
    let allRes = allUniformResults filename 
                        |> List.filter resultsFilter
                        |> List.rev |> Seq.distinctBy (fun res -> res.Settings.WithCanonicalizedDefaults()) |> Seq.toList
    let havingInterestingCompanions = 
        allRes |> List.map (fun res->res.Settings.WithCanonicalizedDefaults())
            |> List.filter (fun settings-> removeRelevantSetting settings <> settings)
            |> List.map removeRelevantSetting
            |> (fun list-> new System.Collections.Generic.HashSet<LvqModelSettingsCli>(list) )
    allRes 
        |> List.filter (fun res ->  (removeRelevantSetting res.Settings).WithCanonicalizedDefaults() |> havingInterestingCompanions.Contains)
        |> Seq.groupBy (fun res -> (removeRelevantSetting res.Settings).WithCanonicalizedDefaults())
        |> Seq.map (fun (group,members) -> members |> Seq.toList |> List.sortBy (fun res->res.GeoMean))
        |> Seq.toList
        |> List.sortBy (fun (best::_) -> best.GeoMean)
        |> List.map (List.map printMeanResults)

let removeEachIterStuffs settings = 
                                            let mutable newSettings:LvqModelSettingsCli = settings
//                                            newSettings.neiB <- false
//                                            newSettings.neiP <- false
                                            newSettings.Bcov <- false
//                                            newSettings.Popt <- false
//                                            newSettings.Ppca <- false
                                            newSettings
showEffect defaultStore removeEachIterStuffs (fun res->res.Settings.ModelType = LvqModelType.Ggm && res.Settings.PrototypesPerClass = 5)



let bestCurrentSettings () = 
    let newBestList = 
        [defaultStore; newStore; tempStore]
            |> List.collect allUniformResults
            |> List.append (allUniformResults tempStore)
            |> List.sortBy (fun res->res.GeoMean)
            |> Seq.distinctBy (fun res -> res.Settings.Canonicalize()) |> Seq.toList
            |> List.map printMeanResults
    File.Delete (LrOptimizer.resultsDir.FullName + tempStore)
    newBestList
        |> List.iter (fun line -> File.AppendAllText (LrOptimizer.resultsDir.FullName + defaultStore,line + "\n"))

let improveKnownCombos () = 
    LrOptimizer.resultsDir.GetFiles("*.txt", SearchOption.AllDirectories)
        |> Seq.map (fun fileInfo -> fileInfo.Name  |> LvqGui.LrOptimizationResult.ExtractItersAndSettings)
        |> Seq.filter (fun (ok,_,_) -> ok)
        |> Seq.map (fun (_,_,settings) -> settings.WithCanonicalizedDefaults())
        |> Seq.distinct
        |> Seq.filter (isTested defaultStore >>not)
        |> Seq.sortBy (fun s-> s.ToShorthand().Length)
        //|> Seq.take 20 |> Utils.shuffle
        |> Seq.map withDefaultLr
        |> Seq.filter (isTested defaultStore >> not) //seq is lazy, so this last minute rechecks availability of results.
        //|> Seq.map (fun s->s.ToShorthand()) 
        |> Seq.map (improveAndTest defaultStore)
        |> Seq.toList


//showNeiEffect defaultStore |> printfn "%A"


[
    "Ggm-1,"
    "Ggm-1,SlowK,"
    "Ggm-1,Ppca,"
    "Ggm-1,Bcov,"
    "Ggm-1,Bcov,Ppca,"
    "Ggm-1,SlowK,Ppca,"
    "Ggm-1,SlowK,Bcov,"
    "Ggm-1,SlowK,Ppca,Bcov,"
    ]
    |> optimizeSettingsList


[
    "Ggm-1,mu0.01,"
    "Ggm-1,mu0.03,"
    "Ggm-1,mu0.1,"
    "Ggm-1,SlowK,mu0.01,"
    "Ggm-1,SlowK,mu0.03,"
    "Ggm-1,SlowK,mu0.1,"
    "Ggm-1,Ppca,mu0.01,"
    "Ggm-1,Ppca,mu0.03,"
    "Ggm-1,Ppca,mu0.1,"
    "Ggm-1,Bcov,mu0.01,"
    "Ggm-1,Bcov,mu0.03,"
    "Ggm-1,Bcov,mu0.1,"
    "Ggm-1,Bcov,Ppca,mu0.01,"
    "Ggm-1,Bcov,Ppca,mu0.03,"
    "Ggm-1,Bcov,Ppca,mu0.1,"
    "Ggm-1,SlowK,Ppca,mu0.01,"
    "Ggm-1,SlowK,Ppca,mu0.03,"
    "Ggm-1,SlowK,Ppca,mu0.1,"
    "Ggm-1,SlowK,Bcov,mu0.01,"
    "Ggm-1,SlowK,Bcov,mu0.03,"
    "Ggm-1,SlowK,Bcov,mu0.1,"
    "Ggm-1,SlowK,Ppca,Bcov,mu0.01,"
    "Ggm-1,SlowK,Ppca,Bcov,mu0.03,"
    "Ggm-1,SlowK,Ppca,Bcov,mu0.1,"
    ]
    |> optimizeSettingsList

