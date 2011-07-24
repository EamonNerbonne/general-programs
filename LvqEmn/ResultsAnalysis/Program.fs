module Program

open LvqLibCli
open LvqGui
open LatexifyCompareMethods



let curDatasetName = "base"
let allResults = ResultParsing.loadAllResults curDatasetName


let alternates:list< (LvqModelSettingsCli -> LvqModelSettingsCli) * string > =
    let setProtosAndType lvqType protos (settings:LvqModelSettingsCli) = settings.WithChanges(lvqType, protos, settings.ParamsSeed, settings.InstanceSeed)
    [
//        (setProtosAndType LvqModelType.Lgm 1 , "lgm 1ppc");
//        (setProtosAndType LvqModelType.Lgm 5 , "lgm 5ppc");
        (setProtosAndType LvqModelType.Gm 1 , "gm 1ppc");
        (setProtosAndType LvqModelType.G2m 1 , "g2m 1ppc");
        (setProtosAndType LvqModelType.Ggm 1 , "ggm 1ppc");
        (setProtosAndType LvqModelType.Gm 5 , "gm 5ppc");
        (setProtosAndType LvqModelType.G2m 5 , "g2m 5ppc");
        (setProtosAndType LvqModelType.Ggm 5 , "ggm 5ppc");
    ] 

let basicSettings = LvqModelSettingsCli()
let heuristics:list< LvqModelSettingsCli * string > =
    [
        (LvqModelSettingsCli(), "core");
        (LvqModelSettingsCli(UpdatePointsWithoutB = true), "NoB");
        (LvqModelSettingsCli(SlowStartLrBad = true ), "SlowStartLrBad");
        (LvqModelSettingsCli(NgUpdateProtos = true), "NgUpdate");
        (LvqModelSettingsCli(NgInitializeProtos = true), "NgInit");
        (LvqModelSettingsCli(NgInitializeProtos = true, ProjOptimalInit = true), "NgInit+Pi");
        (LvqModelSettingsCli(NgInitializeProtos = true, ProjOptimalInit=true, BLocalInit=true), "NgInit+Pi+Bi");
        (LvqModelSettingsCli(NgInitializeProtos = true, SlowStartLrBad = true ), "NgInit+SlowStartLrBad");
    ]


let alltypes = heuristics |> List.collect (fun (setting, name) -> alternates |> List.map (fun (alt, altname) -> (alt setting, name + ":" + altname))) |> List.map fst 

List.map (fun (settings, name) ->
        latexifyCompareMethods name allResults (
            alternates
            |> List.filter (fun (alternate, altName) -> 
                    settings = basicSettings 
                    || (alternate settings).ToShorthand() <> (alternate basicSettings).ToShorthand() 
                    && (not settings.NgInitializeProtos || (alternate settings).PrototypesPerClass <> 1) 
                    )
            |> List.map (fun (alternate, altName) -> (alternate settings, altName))
        )
    ) heuristics 
    |> String.concat "\n\n"
    |> printfn "%s"  //print results


(List.map (LvqModelSettingsCli(NgInitializeProtos = true, SlowStartLrBad = true ) |> Utils.pass |> Utils.apply1st) alternates)
    |> List.map (Utils.apply1st ResultParsing.filterResults >> Utils.apply1st (Utils.pass allResults) )
    |> printfn "%A"


    
(*

String.Join("\\\\\n", alltypes|>List.map fst |> List.map (errTypeCorrelationLatex curDatasetName)) |> printfn "%s"


initCorrs curDatasetName alltypes|>printfn "%A"

initCorrs curDatasetName alltypes |> List.averageBy (snd >> fst) |> printfn "%A"


alltypes  |> List.map fst |> List.filter (fun settings -> settings.ModelType <> LvqModelType.Lgm)
    |> List.map (loadResultsByLr curDatasetName >> List.map (snd >> List.map (fun err->err.training))) 
            //list of model types, each has a list of LRs, each has a list of results in file order
    |> List.concat  //list of model types+lrs, each has a list of results in file order
    |> corrs //a list of correlations between differing types/lrs over changing initialization
    |> meanStderr //mean correlation between differing types/lrs over changing initialization
    |> printfn "%A"

*)