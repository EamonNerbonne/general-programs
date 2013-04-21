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
        //>> List.rev
        //>> Seq.filter (isTested defaultStore >> not) 
        //>> Seq.filter (isTested newStore >> not) 
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
                        "Lgm[3]-1,";"Lgm[3]-3,";"Lgm[3]-5,";"Lgm-1,";"Lgm-3,";"Lgm-5,"
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
        |> List.sortBy (fun res->res.Mean2) 
       // |> List.rev
        |> Seq.distinctBy (fun res -> res.Settings.WithCanonicalizedDefaults()) |> Seq.toList
        |> List.map (fun res->res.Settings)
        |> List.filter (fun settings -> settings.ModelType = LvqModelType.G2m)
        |> List.map (OptimalLrSearch.finalTestSettings >> OptimalLrSearch.saveResults tempStore)




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
        |> Seq.map (fun (group,members) -> members |> Seq.toList |> List.sortBy (fun res->res.Mean2))
        |> Seq.toList
        |> List.sortBy (List.head >> (fun best-> best.Mean2))
        |> List.map (List.map printMeanResults)

let removeEachIterStuffs settings = 
                                            let mutable newSettings:LvqModelSettingsCli = settings
//                                            newSettings.neiB <- false
//                                            newSettings.neiP <- false
                                            newSettings.Bcov <- false
//                                            newSettings.Popt <- false
//                                            newSettings.Ppca <- false
                                            newSettings
showEffect defaultStore removeEachIterStuffs (fun res->res.Settings.ModelType = LvqModelType.Ggm && res.Settings.PrototypesPerClass = 3)



let sortFile file = 
    let newBestList = 
        allUniformResults file
        |> List.append (allUniformResults tempStore)
        |> List.sortBy (fun res->res.Mean2)
        |> Seq.distinctBy (fun res -> res.Settings.Canonicalize()) |> Seq.toList
//        |> List.map (fun x -> 
//            let mutable settings = x.Settings
//            if settings.ModelType = LvqModelType.Lgm || settings.ModelType = LvqModelType.Lpq || settings.ModelType = LvqModelType.Gm then
//                settings.Dimensionality <- (
//                     if settings.Dimensionality = 0 then
//                         if settings.ModelType = LvqModelType.Gm then 2 else 3
//                     else if settings.Dimensionality = 6 then
//                         0
//                     else 
//                         settings.Dimensionality
//                    )
//            { Mean2 = x.Mean2; NN = x.NN; Test = x.Test; Training = x.Training; Settings = settings }
//        )
        |> List.map printMeanResults
    File.Delete (LrGuesser.resultsDir.FullName + file)
    newBestList
        |> List.iter (fun line -> File.AppendAllText (LrGuesser.resultsDir.FullName + file, line + "\n"))


let bestCurrentSettings () = 
    let newBestList = 
        [defaultStore; newStore; tempStore]
            |> List.collect allUniformResults
            |> List.append (allUniformResults tempStore)
            |> List.sortBy (fun res->res.Mean2)
            |> Seq.distinctBy (fun res -> res.Settings.Canonicalize()) |> Seq.toList
            |> List.map printMeanResults
    File.Delete (LrGuesser.resultsDir.FullName + tempStore)
    newBestList
        |> List.iter (fun line -> File.AppendAllText (LrGuesser.resultsDir.FullName + defaultStore,line + "\n"))


//showNeiEffect defaultStore |> printfn "%A"


[
    "Fgm-1,mu0.1,"
    "Fgm-1,scP,mu0.1,"
    "Fgm-1,scP,Ppca,mu0.1,"
    "Fgm-1,scP,Popt,mu0.1,"
    "Fgm-1,scP,Ppca,Popt,mu0.1,"
    "Fgm-1,SlowK,mu0.1,"
    "Fgm-1,SlowK,scP,mu0.1,"
    "Fgm-1,SlowK,scP,Ppca,mu0.1,"
    "Fgm-1,SlowK,scP,Popt,mu0.1,"
    "Fgm-1,SlowK,scP,Ppca,Popt,mu0.1,"
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


        
[
    "Normal-1,mu0.01,"
    "Normal-1,mu0.01,SlowK,"
    "Normal-1,mu0.01,Bcov,"
    "Normal-1,mu0.01,Bcov,SlowK,"
    "Normal-1,"
    "Normal-1,SlowK,"
    "Normal-1,Bcov,"
    "Normal-1,Bcov,SlowK,"
    "Normal-2,mu0.01,"
    "Normal-2,NGi,mu0.01,"
    "Normal-2,mu0.01,SlowK,"
    "Normal-2,NGi,mu0.01,SlowK,"
    "Normal-2,mu0.01,Bcov,"
    "Normal-2,NGi,mu0.01,Bcov,"
    "Normal-2,mu0.01,SlowK,Bcov,"
    "Normal-2,NGi,mu0.01,SlowK,Bcov,"
    "Normal-2,"
    "Normal-2,NGi,"
    "Normal-2,SlowK,"
    "Normal-2,NGi,SlowK,"
    "Normal-2,Bcov,"
    "Normal-2,NGi,Bcov,"
    "Normal-2,SlowK,Bcov,"
    "Normal-2,NGi,SlowK,Bcov,"
    "Normal-3,mu0.01,"
    "Normal-3,NGi,mu0.01,"
    "Normal-3,mu0.01,SlowK,"
    "Normal-3,NGi,mu0.01,SlowK,"
    "Normal-3,mu0.01,Bcov,"
    "Normal-3,NGi,mu0.01,Bcov,"
    "Normal-3,mu0.01,SlowK,Bcov,"
    "Normal-3,NGi,mu0.01,SlowK,Bcov,"
    "Normal-3,"
    "Normal-3,NGi,"
    "Normal-3,SlowK,"
    "Normal-3,NGi,SlowK,"
    "Normal-3,Bcov,"
    "Normal-3,NGi,Bcov,"
    "Normal-3,SlowK,Bcov,"
    "Normal-3,NGi,SlowK,Bcov,"
    ]
    |> optimizeSettingsList

//recomputeRes defaultStore
