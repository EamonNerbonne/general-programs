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
       // >> Seq.filter (isTested tempStore >> not) 
        >> Seq.map (improveAndTest tempStore)
        >> Seq.toList


[
    //"Ggm-1,scP,Ppca,SlowK,lr0.023856933148000251,lrP0.024547811783315155,lrB5.74323779391736,"
    //"Gm-1,scP,lr0.00031107939389401281,lrP14.02245453771569,"// GeoMean: 0.200119 ~ 0.001250; Training: 0.247116 ~ 0.005440; Test: 0.249612 ~ 0.005917; NN: 0.235402 ~ 0.004090

    //"Gpq-1,Ppca,scP,neiB,lr0.010220550736019737,lrP0.14329360239567931,lrB0.019884392119598537,"
    "G2m-1,Ppca,scP,neiB,lr0.0076233468565424936,lrP0.2345331487738074,lrB0.038908206439316292,"
    "G2m-5,scP,lr0.027493759451317022,lrP0.14237999639527971,lrB0.017299899679362487,"// GeoMean: 0.107888 ~ 0.000854; Training: 0.111457 ~ 0.002939; Test: 0.118621 ~ 0.003476; NN: 0.153824 ~ 0.003695
    //"Gpq-5,Ppca,scP,neiB,NGi,lr0.0045342843040029474,lrP0.064742861087417752,lrB0.053659607722999894,"
    //"G2m-5,Ppca,scP,neiB,NGi,lr0.0041163477547472025,lrP0.12514067956819713,lrB0.59856959218187411,"
    //"G2m-1,Ppca,scP,lr0.011832180266966016,lrP0.14806611543379794,lrB0.016147051104712921,"// GeoMean: 0.133304 ~ 0.000994; Training: 0.172748 ~ 0.006247; Test: 0.177301 ~ 0.006536; NN: 0.166293 ~ 0.002987
    //"G2m-1,scP,lr0.01462705218920733,lrP0.29149194963539737,lrB0.00958803473805164,"// GeoMean: 0.132896 ~ 0.001012; Training: 0.171655 ~ 0.004448; Test: 0.176456 ~ 0.004929; NN: 0.177466 ~ 0.006307
    //"Gpq-1,scP,lr0.023511667560001521,lrP0.18261865403223981,lrB0.030664100544367488,"// GeoMean: 0.131492 ~ 0.002668; Training: 0.178893 ~ 0.009648; Test: 0.184539 ~ 0.009738; NN: 0.178269 ~ 0.006930
    //"Gpq-5,Ppca,scP,NGi,lr0.038327226569367268,lrP0.032150919612653019,lrB0.0209235215135698,"// GeoMean: 0.103560 ~ 0.000740; Training: 0.101954 ~ 0.001164; Test: 0.112299 ~ 0.002284; NN: 0.149844 ~ 0.002432
    //"Gpq-1,Ppca,scP,lr0.012358494489286302,lrP0.11032533067623808,lrB0.02295090037869002,"// GeoMean: 0.136670 ~ 0.000897; Training: 0.171710 ~ 0.006362; Test: 0.176939 ~ 0.006621; NN: 0.166823 ~ 0.003912
    //"G2m-5,Ppca,scP,NGi,lr0.0037521078348990008,lrP0.87705208187116923,lrB0.423013349191042,"// GeoMean: 0.105566 ~ 0.000701; Training: 0.102163 ~ 0.001678; Test: 0.114249 ~ 0.002805; NN: 0.152617 ~ 0.003191
    ]
    |> optimizeSettingsList


let researchRes () =
    allUniformResults defaultStore
        |> List.sortBy (fun res->res.GeoMean) 
        |> Seq.distinctBy (fun res -> res.Settings.WithCanonicalizedDefaults()) |> Seq.toList
        |> List.filter(fun res->not res.Settings.scP)
        |> List.sortBy (fun res->res.Settings.ToShorthand())
        |> List.sortBy (fun res -> res.Settings.ActiveRefinementCount ())
//        |> List.rev
        |> List.map (fun res->res.Settings)
        |> Seq.filter (isTested tempStore >> not) //seq is lazy, so this last minute rechecks availability of results.
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
        |> Seq.groupBy (fun res-> res.Settings.WithCanonicalizedDefaults())
        |> Seq.collect snd |>Seq.toList
        //|> Seq.distinctBy (fun res-> res.Settings.WithCanonicalizedDefaults()) |> Seq.toList

        //|> List.filter (fun res->res.Settings.ModelType = LvqModelType.G2m)
        |> List.map printMeanResults
        |> String.concat "\n"
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
