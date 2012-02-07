#I @"ResultsAnalysis\bin\ReleaseMingw2"
#r "ResultsAnalysis"
#r "LvqLibCli"
#r "LvqGui"
#r "EmnExtensions"
#r "PresentationCore"
#r "WindowsBase"
#r "EmnExtensionsWpf"
#time "on"

open LvqLibCli
open LvqGui
open System.Threading
open System
open System.IO
open System.Windows.Media
open EmnExtensions.Wpf
open System.Threading.Tasks

open Utils
open OptimalLrSearch


allUniformResults () |> List.sortBy (fun res->res.GeoMean) |> Seq.distinctBy (fun res-> res.Settings.WithDefaultLr()) |> Seq.toList
    //|> List.filter (fun res->res.Settings.ModelType = LvqModelType.Gm && res.Settings.PrototypesPerClass = 1)
    |> List.map printMeanResults


//(*
[
    "Gpq-1,Ppca,";"Gm-5,noKP,NGi,SlowK,";
    "G2m-1,neiP,";"G2m-1,neiB,";"Gm-1,neiP,";"G2m-1,neiB,neiP,";
    "G2m-1,neiP,Ppca,";"G2m-1,neiB,Ppca,";"Gm-1,neiP,Ppca,";"G2m-1,neiB,neiP,Ppca,";
    "G2m-5,neiP,Ppca,";"G2m-5,neiB,Ppca,";"Gm-5,neiP,Ppca,";"G2m-5,neiB,neiP,Ppca,";
    "G2m-5,neiP,Ppca,NGi,";"G2m-5,neiB,Ppca,NGi,";"Gm-5,neiP,Ppca,NGi,";"G2m-5,neiB,neiP,Ppca,NGi,";
    "Gm-1,neiP,SlowK,";"Gm-1,neiP,Ppca,SlowK,";"Gm-5,neiP,Ppca,SlowK,";"Gm-5,neiP,Ppca,NGi,SlowK,";
    "Gpq-1,neiP,";"Gpq-1,neiB,";"Gpq-1,neiB,neiP,";"Gpq-1,neiP,Ppca,";"Gpq-1,neiB,Ppca,";"Gpq-1,neiB,neiP,Ppca,";
    "Gpq-5,neiP,NGi,";"Gpq-5,neiB,NGi,";"Gpq-5,neiB,neiP,NGi,";"Gpq-5,neiP,Ppca,NGi,";"Gpq-5,neiB,Ppca,NGi,";"Gpq-5,neiB,neiP,Ppca,NGi,";
    ]
//*)
(*
[
    "G2m-1,scP,";"Gpq-1,scP,";"Gm-1,scP,";
    "G2m-5,scP,NGi,";"Gpq-5,scP,NGi,";"Gm-5,scP,NGi,";
    "G2m-1,scP,Ppca,";"Gpq-1,scP,Ppca,";"Gm-1,scP,Ppca,";
    "G2m-5,scP,Ppca,NGi,";"Gpq-5,scP,Ppca,NGi,";"Gm-5,scP,Ppca,NGi,";
    "G2m-5,scP,Ppca,";"Gpq-5,scP,Ppca,";"Gm-5,scP,Ppca,";
    "G2m-5,scP,";"Gpq-5,scP,";"Gm-5,scP,";
    "Lgm-1,neiP,";"Lgm-5,NGi,neiP,";
    ]
   // *)
    |> List.map (CreateLvqModelValues.ParseShorthand >> withDefaultLr) 
    |> List.filter (isTested>>not)
    |> Seq.distinct |>Seq.toList
    //|> List.map (fun s->s.ToShorthand())
    |> List.map improveAndTest

LrOptimizer.resultsDir.GetFiles("*.txt", SearchOption.AllDirectories)
    |> Seq.map (fun fileInfo -> fileInfo.Name  |> LvqGui.LrOptimizationResult.ExtractItersAndSettings)
    |> Seq.filter (fun (ok,_,_) -> ok)
    |> Seq.map (fun (_,_,settings) -> settings.WithDefaultSeeds().WithDefaultLr())
    |> Seq.distinct
    |> Seq.filter (isTested>>not)
    |> Seq.sortBy (fun s-> s.ToShorthand().Length)
    //|> Seq.take 20 |> Utils.shuffle
    |> Seq.map withDefaultLr
    |> Seq.filter (isTested>>not) //seq is lazy, so this last minute rechecks availability of results.
    //|> Seq.map (fun s->s.ToShorthand()) 
    |> Seq.map improveAndTest
    |> Seq.toList

let recomputeRes () =
    File.ReadAllLines ( LrOptimizer.resultsDir.FullName + "\\uniform-results.txt") 
        |> List.ofArray 
        |> List.map (fun s-> if s.Contains " " then  s.Substring(0, s.IndexOf " ") else s)
        |> List.filter (String.IsNullOrEmpty >> not)
        |> List.map CreateLvqModelValues.TryParseShorthand 
        |> List.filter (fun settings -> settings.HasValue)
        |> List.map (fun settings -> settings.Value)
        //|> List.map (fun settings -> settings.ToShorthand())
        |> List.map (OptimalLrSearch.testSettings 100 1u 1e7 >> OptimalLrSearch.printResults >> (fun resline -> File.AppendAllText (LrOptimizer.resultsDir.FullName + "\\uniform-results2.txt", resline + "\n"); resline ))



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
