#I @"ResultsAnalysis\bin\ReleaseMingw"
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


let researchRes () =
    allUniformResults defaultStore
        |> List.rev
        |> Seq.distinctBy (fun res -> res.Settings.WithDefaultLr()) |> Seq.toList
        |> List.sortBy (fun res->res.GeoMean) 
        |> List.map (fun res->res.Settings)
        |> Seq.filter (isTested newStore >> not) //seq is lazy, so this last minute rechecks availability of results.
        |> Seq.map (improveAndTest newStore 0.4)
        |> Seq.toList

let recomputeRes filename =
    allUniformResults filename 
        |> List.rev
        |> Seq.distinctBy (fun res -> res.Settings.WithDefaultLr()) |> Seq.toList
        |> List.sortBy (fun res->res.GeoMean) 
        |> List.map (fun res->res.Settings)
        |> List.filter (fun settings -> settings.ModelType = LvqModelType.G2m)
        |> List.map (OptimalLrSearch.finalTestSettings >> OptimalLrSearch.printResults >> (fun resline -> File.AppendAllText (LrOptimizer.resultsDir.FullName + "\\" + tempStore, resline + "\n"); resline ))



let showNeiEffect filename =
    let allRes = allUniformResults filename |> List.rev |> Seq.distinctBy (fun res -> res.Settings.WithDefaultLr()) |> Seq.toList
    let withoutSpecial settings = 
                                                let mutable newSettings:LvqModelSettingsCli = settings
                                                newSettings.neiB <- false
                                                newSettings.neiP <- false
                                                newSettings.scP <- false
                                                newSettings
    let withSpecial = 
        allRes |> List.map (fun res->res.Settings.WithDefaultLr())
            |> List.filter (fun settings-> withoutSpecial settings <> settings)
            |> List.map withoutSpecial
            |> (fun list-> new System.Collections.Generic.HashSet<LvqModelSettingsCli>(list) )
    allRes 
        |> List.filter (fun res ->  (withoutSpecial res.Settings).WithDefaultLr() |> withSpecial.Contains)
        |> Seq.groupBy (fun res -> (withoutSpecial res.Settings).WithDefaultLr())
        |> Seq.map (fun (group,members) -> members |> Seq.toList |> List.sortBy (fun res->res.GeoMean))
        |> Seq.toList
        |> List.sortBy (fun (best::_) -> best.GeoMean)
        |> List.map (List.map printMeanResults)
    

let optimizeSettingsList = 
        List.map (CreateLvqModelValues.ParseShorthand >> withDefaultLr) 
        >> List.filter (isTested defaultStore >>not)
        >> Seq.distinct >> Seq.toList
        //|> List.map (fun s->s.ToShorthand())
        >> List.map (improveAndTest defaultStore 1.)

let bestCurrentSettings () = 
    allUniformResults defaultStore
        |> List.sortBy (fun res->res.GeoMean)
        |> Seq.distinctBy (fun res-> res.Settings.WithDefaultLr()) |> Seq.toList
        //|> List.filter (fun res->res.Settings.ModelType = LvqModelType.G2m)
        |> List.map printMeanResults
       // |> List.iter (fun line -> File.AppendAllText (LrOptimizer.resultsDir.FullName + "\\uniform-results-orig.txt",line + "\n"))

let improveKnownCombos () = 
    LrOptimizer.resultsDir.GetFiles("*.txt", SearchOption.AllDirectories)
        |> Seq.map (fun fileInfo -> fileInfo.Name  |> LvqGui.LrOptimizationResult.ExtractItersAndSettings)
        |> Seq.filter (fun (ok,_,_) -> ok)
        |> Seq.map (fun (_,_,settings) -> settings.WithDefaultSeeds().WithDefaultLr())
        |> Seq.distinct
        |> Seq.filter (isTested defaultStore >>not)
        |> Seq.sortBy (fun s-> s.ToShorthand().Length)
        //|> Seq.take 20 |> Utils.shuffle
        |> Seq.map withDefaultLr
        |> Seq.filter (isTested defaultStore >> not) //seq is lazy, so this last minute rechecks availability of results.
        //|> Seq.map (fun s->s.ToShorthand()) 
        |> Seq.map (improveAndTest defaultStore 1.)
        |> Seq.toList


//showNeiEffect defaultStore |> printfn "%A"
