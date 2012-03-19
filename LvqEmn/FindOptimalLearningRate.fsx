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

let defaultStore = "uniform-results.txt"
let newStore = "uniform-results-new.txt"
let tempStore = "uniform-results-tmp.txt"
let decayStore = "uniform-results-withDecay.txt"
let temp2Store = "uniform-results-tmp2.txt"
let optimizeSettingsList = 
        List.map (CreateLvqModelValues.ParseShorthand >> withDefaultLr) 
        >> Seq.distinctBy (fun s-> s.WithCanonicalizedDefaults())  >> Seq.toList
        //>> List.rev
        >> Seq.filter (isTested tempStore >> not) 
        >> Seq.map (improveAndTestWithControllers 0 1.0 learningRateControllers tempStore)
        >> Seq.toList

(*
[
    "Ggm-1,scP,Ppca,SlowK,lr0.023856933148000251,lrP0.024547811783315155,lrB5.74323779391736,"
    "Gm-1,scP,lr0.00031107939389401281,lrP14.02245453771569,"// GeoMean: 0.200119 ~ 0.001250; Training: 0.247116 ~ 0.005440; Test: 0.249612 ~ 0.005917; NN: 0.235402 ~ 0.004090
    
    "Ggm-5,Ppca,scP,NGi,SlowK,lr0.010642145293080753,lrP0.039826223786304772,lrB53.420046186514462, "
    "Ggm-1,scP,Ppca,lr0.015907068045327617,lrP0.10751518999560114,lrB2.2281412699827672, "
    "Ggm-1,scP,Bcov,lr0.016864906604885335,lrP0.12489875104765052,lrB3.6457807781465017,"
    "Ggm-1,scP,lr0.0070894501970626515,lrP0.28634698318942875,lrB13.199124471880509,"
    "Ggm-1,scP,Ppca,SlowK,lr0.023856933148000251,lrP0.024547811783315155,lrB5.74323779391736, "
    "Ggm-1,scP,Ppca,Bcov,SlowK,lr0.032431021403821612,lrP0.024604412062745919,lrB4.9332431650390651,"
    "Ggm-5,scP,Ppca,NGu,NGi,SlowK,lr0.0070305007512141164,lrP0.056754615837412821,lrB68.622497141366978,"
    "Ggm-5,scP,Ppca,NGi,lr0.0041363963876378494,lrP0.13282169510056088,lrB53.9131929003227, "
    "Ggm-5,Ppca,SlowK,lr0.023002796709016129,lrP0.012961349492535364,lrB9.928512858671569, "

    "Gpq-1,Ppca,scP,lr0.012358494489286302,lrP0.11032533067623808,lrB0.02295090037869002,"// GeoMean: 0.136670 ~ 0.000897; Training: 0.171710 ~ 0.006362; Test: 0.176939 ~ 0.006621; NN: 0.166823 ~ 0.003912
    "G2m-1,Ppca,scP,lr0.011832180266966016,lrP0.14806611543379794,lrB0.016147051104712921,"// GeoMean: 0.133304 ~ 0.000994; Training: 0.172748 ~ 0.006247; Test: 0.177301 ~ 0.006536; NN: 0.166293 ~ 0.002987
    "Gpq-1,Ppca,scP,neiB,lr0.010220550736019737,lrP0.14329360239567931,lrB0.019884392119598537,"
    "G2m-1,Ppca,scP,neiB,lr0.0076233468565424936,lrP0.2345331487738074,lrB0.038908206439316292,"
    "Gpq-1,scP,lr0.023511667560001521,lrP0.18261865403223981,lrB0.030664100544367488,"// GeoMean: 0.131492 ~ 0.002668; Training: 0.178893 ~ 0.009648; Test: 0.184539 ~ 0.009738; NN: 0.178269 ~ 0.006930
    "G2m-1,scP,lr0.01462705218920733,lrP0.29149194963539737,lrB0.00958803473805164,"// GeoMean: 0.132896 ~ 0.001012; Training: 0.171655 ~ 0.004448; Test: 0.176456 ~ 0.004929; NN: 0.177466 ~ 0.006307

    "Gpq-5,Ppca,scP,NGi,lr0.038327226569367268,lrP0.032150919612653019,lrB0.0209235215135698,"// GeoMean: 0.103560 ~ 0.000740; Training: 0.101954 ~ 0.001164; Test: 0.112299 ~ 0.002284; NN: 0.149844 ~ 0.002432
    "G2m-5,Ppca,scP,NGi,lr0.0037521078348990008,lrP0.87705208187116923,lrB0.423013349191042,"// GeoMean: 0.105566 ~ 0.000701; Training: 0.102163 ~ 0.001678; Test: 0.114249 ~ 0.002805; NN: 0.152617 ~ 0.003191
    "Gpq-5,Ppca,scP,neiB,NGi,lr0.0045342843040029474,lrP0.064742861087417752,lrB0.053659607722999894,"
    "G2m-5,Ppca,scP,neiB,NGi,lr0.0041163477547472025,lrP0.12514067956819713,lrB0.59856959218187411,"
    "Gpq-5,Ppca,scP,lr0.092222534449745777,lrP0.021717632663959416,lrB0.0085847050289434221,"// GeoMean: 0.106323 ~ 0.000839; Training: 0.107291 ~ 0.001794; Test: 0.116176 ~ 0.002830; NN: 0.151171 ~ 0.002795
    "G2m-5,Ppca,scP,lr0.023603993984004434,lrP0.14795089865689767,lrB0.0054090355108190723,"// GeoMean: 0.108298 ~ 0.000924; Training: 0.110655 ~ 0.002151; Test: 0.117819 ~ 0.002976; NN: 0.153458 ~ 0.003406
    "Gpq-5,scP,NGi,lr0.047494016517064176,lrP0.041604983107655237,lrB0.018100546049847184,"// GeoMean: 0.113179 ~ 0.000829; Training: 0.114339 ~ 0.003825; Test: 0.125227 ~ 0.004332; NN: 0.162770 ~ 0.004756
    "G2m-5,scP,NGi,lr0.010538507687204799,lrP0.38796279359979308,lrB0.12897475832613819,"// GeoMean: 0.109049 ~ 0.000885; Training: 0.106623 ~ 0.002312; Test: 0.117771 ~ 0.002924; NN: 0.156530 ~ 0.003461
    ]
    |> optimizeSettingsList
    *)
let heuristics=
    [
        (fun (s: LvqModelSettingsCli) -> 
            let mutable copy = s
            copy.Bcov <- true
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
                        //]//
                        "Lgm-1,";"Lgm-3,";"Lgm-5,";"Lgm[6]-1,";"Lgm[6]-3,";"Lgm[6]-5,"]
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
        |> List.sortBy (fun s->s.LikelyRefinementRanking())
        |> Seq.filter (isTested defaultStore >> not) //seq is lazy, so this last minute rechecks availability of results.
        |> Seq.map withDefaultLr
        |> Seq.map (improveAndTestWithControllers 0 1.0 allControllers defaultStore)
        |> Seq.toList
        //|> List.map(fun s->s.ToShorthand())

let researchRes () =
    allUniformResults defaultStore
        |> List.filter(fun res->not res.Settings.scP )
        |> List.append (allUniformResults "uniform-results-scp-gm-ggm.txt" )
        |> List.append (allUniformResults "uniform-results-scp-g2m-gpq-normalizes-BPv.txt" )
        |> List.sortBy (fun res->res.GeoMean) 
        |> Seq.distinctBy (fun res -> res.Settings.WithCanonicalizedDefaults()) |> Seq.toList
        //|> List.filter(fun res->not res.Settings.scP && res.Settings.ModelType <> LvqModelType.Lgm)
        //|> List.sortBy (fun res->res.Settings.ToShorthand())
        |> List.sortBy (fun res -> res.Settings.LikelyRefinementRanking ())
        //|> List.rev
        |> List.map (fun res->res.Settings)
        //|> (fun ss -> ss.AsParallel().WithDegreeOfParallelism(2))
        |> Seq.filter (isTested decayStore >> not) //seq is lazy, so this last minute rechecks availability of results.
        |> Seq.map (improveAndTestWithControllers 7 0.5 decayControllers decayStore)
        |> Seq.toList

let recomputeRes filename =
    allUniformResults filename 
        |> List.rev
        |> Seq.distinctBy (fun res -> res.Settings.WithCanonicalizedDefaults()) |> Seq.toList
        |> List.sortBy (fun res->res.GeoMean) 
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
    allUniformResults defaultStore
        |> List.sortBy (fun res->res.GeoMean)
        |> List.map printMeanResults
        |> List.iter (fun line -> File.AppendAllText (LrOptimizer.resultsDir.FullName + tempStore,line + "\n"))

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
