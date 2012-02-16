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

let optimizeSettingsList = 
        List.map (CreateLvqModelValues.ParseShorthand >> withDefaultLr) 
        >> List.filter (isTested>>not)
        >> Seq.distinct >> Seq.toList
        //|> List.map (fun s->s.ToShorthand())
        >> List.map (improveAndTest 0)


allUniformResults ()
    |> List.sortBy (fun res->res.GeoMean)
    |> Seq.distinctBy (fun res-> res.Settings.WithDefaultLr()) |> Seq.toList
    //|> List.filter (fun res->res.Settings.ModelType = LvqModelType.G2m)
    |> List.map printMeanResults
   // |> List.iter (fun line -> File.AppendAllText (LrOptimizer.resultsDir.FullName + "\\uniform-results-orig.txt",line + "\n"))


LrOptimizer.resultsDir.GetFiles("*.txt", SearchOption.AllDirectories)
    |> Seq.map (fun fileInfo -> fileInfo.Name  |> LvqGui.LrOptimizationResult.ExtractItersAndSettings)
    |> Seq.filter (fun (ok,_,_) -> ok)
    |> Seq.map (fun (_,_,settings) -> settings.WithDefaultSeeds().WithDefaultLr())
    |> Seq.distinct
    |> Seq.filter (isTested>>not)
    |> Seq.sortBy (fun s-> -s.ToShorthand().Length)
    //|> Seq.take 20 |> Utils.shuffle
    |> Seq.map withDefaultLr
    |> Seq.filter (isTested>>not) //seq is lazy, so this last minute rechecks availability of results.
    //|> Seq.map (fun s->s.ToShorthand()) 
    |> Seq.map (improveAndTest 0)
    |> Seq.toList

let recomputeRes () =
    allUniformResults () 
        |> List.sortBy (fun res->res.GeoMean) 
//        |> Seq.distinctBy (fun res-> res.Settings.WithDefaultLr()) 
        |> Seq.toList
        |> List.map (fun res->res.Settings)
        |> List.filter (fun settings -> settings.ModelType = LvqModelType.G2m)
        |> List.map (OptimalLrSearch.finalTestSettings >> OptimalLrSearch.printResults >> (fun resline -> File.AppendAllText (LrOptimizer.resultsDir.FullName + "\\uniform-results2.txt", resline + "\n"); resline ))



let showNeiEffect () =
    let allRes = allUniformResults () |> List.rev |> Seq.distinctBy (fun res -> res.Settings.WithDefaultLr()) |> Seq.toList
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
    


showNeiEffect () |> printfn "%A"
