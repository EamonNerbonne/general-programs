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
open LvqLibCli
open System
open Utils
open OptimalLrSearch

let defaultStore = "uniform-results.txt"
let newStore = "uniform-results-new.txt"
let tempStore = "uniform-results-tmp.txt"
let optimizeSettingsList = 
        List.map (CreateLvqModelValues.ParseShorthand >> withDefaultLr) 
        >> Seq.distinctBy (fun s-> s.WithCanonicalizedDefaults())  >> Seq.toList
        //>> List.map (fun s->s.ToShorthand())
        >> Seq.filter (isTested newStore >> not) 
        >> Seq.map (improveAndTest newStore)
        >> Seq.toList


//[@"G2m-1,scP,lr0.019456673102934145,lrP0.2311637035171763,lrB0.011238703940835445,"]
//    |> optimizeSettingsList


let researchRes () =
    allUniformResults defaultStore
        |> List.sortBy (fun res->res.GeoMean) 
        |> Seq.distinctBy (fun res -> res.Settings.WithCanonicalizedDefaults()) |> Seq.toList
        |> List.filter(fun res-> res.Settings.scP)
        |> List.sortBy (fun res->res.Settings.ToShorthand())
        |> List.sortBy (fun res -> res.Settings.ActiveRefinementCount ())
//        |> List.rev
        |> List.map (fun res->res.Settings)
        |> Seq.filter (isTested tempStore >> not) //seq is lazy, so this last minute rechecks availability of results.
//        |> Seq.append (allUniformResults newStore |> Seq.map  (fun res->res.Settings))
        |> Seq.map (improveAndTestWithControllers 1.0 learningRateControllers tempStore)
        |> Seq.toList

let recomputeRes filename =
    allUniformResults filename 
        |> List.rev
        |> Seq.distinctBy (fun res -> res.Settings.WithCanonicalizedDefaults()) |> Seq.toList
        |> List.sortBy (fun res->res.GeoMean) 
        |> List.map (fun res->res.Settings)
        |> List.filter (fun settings -> settings.ModelType = LvqModelType.G2m)
        |> List.map (OptimalLrSearch.finalTestSettings >> OptimalLrSearch.printResults >> (fun resline -> File.AppendAllText (LrOptimizer.resultsDir.FullName + "\\" + tempStore, resline + "\n"); resline ))



let removeEachIterStuffs settings = 
                                            let mutable newSettings:LvqModelSettingsCli = settings
                                            newSettings.neiB <- false
                                            newSettings.neiP <- false
                                            newSettings.scP <- false
                                            newSettings


let showEffect filename removeRelevantSetting =
    let allRes = allUniformResults filename |> List.rev |> Seq.distinctBy (fun res -> res.Settings.WithCanonicalizedDefaults()) |> Seq.toList
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

showEffect    defaultStore removeEachIterStuffs



let bestCurrentSettings () = 
    allUniformResults defaultStore
        |> List.sortBy (fun res->res.GeoMean)
        |> Seq.distinctBy (fun res-> res.Settings.WithCanonicalizedDefaults()) |> Seq.toList
        //|> List.filter (fun res->res.Settings.ModelType = LvqModelType.G2m)
        |> List.map printMeanResults
       // |> List.iter (fun line -> File.AppendAllText (LrOptimizer.resultsDir.FullName + "\\uniform-results-orig.txt",line + "\n"))

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
