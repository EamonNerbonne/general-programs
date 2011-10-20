module GeneralSettings

open LvqLibCli
open LvqGui

let allvariants:list< (LvqModelSettingsCli -> LvqModelSettingsCli) * string > =
    let setProtosAndType lvqType protos (settings:LvqModelSettingsCli) = settings.WithChanges(lvqType, protos, settings.ParamsSeed, settings.InstanceSeed)
    [
        (setProtosAndType LvqModelType.Lgm 1 , "lgm 1ppc");
        (setProtosAndType LvqModelType.Gm 1 , "gm 1ppc");
        (setProtosAndType LvqModelType.G2m 1 , "g2m 1ppc");
        (setProtosAndType LvqModelType.Ggm 1 , "ggm 1ppc");
        (setProtosAndType LvqModelType.Lgm 5 , "lgm 5ppc");
        (setProtosAndType LvqModelType.Gm 5 , "gm 5ppc");
        (setProtosAndType LvqModelType.G2m 5 , "g2m 5ppc");
        (setProtosAndType LvqModelType.Ggm 5 , "ggm 5ppc");
    ] 


let variants = allvariants |> List.filter (snd >> (fun s -> not <| s.StartsWith("lgm")))

let heuristics =
    [
        (LvqModelSettingsCli(), "core");
        (LvqModelSettingsCli(UpdatePointsWithoutB = true), "NoB");
        (LvqModelSettingsCli(SlowStartLrBad = true ), "SlowStartLrBad");
        (LvqModelSettingsCli(NgUpdateProtos = true), "NgUpdate");
        (LvqModelSettingsCli(NgInitializeProtos = true), "NgInit");
        (LvqModelSettingsCli(NgInitializeProtos = true, ProjOptimalInit = true), "NgInit+Pi");
        (LvqModelSettingsCli(NgInitializeProtos = true, ProjOptimalInit = true, BLocalInit = true), "NgInit+Pi+Bi");
        (LvqModelSettingsCli(NgInitializeProtos = true, SlowStartLrBad = true ), "NgInit+SlowStartLrBad");
    ]

let relevantVariants baseHeuristicSettings =
    let basicSettings = LvqModelSettingsCli()
    variants
        |> List.map (fun (variant, variantName) -> (variant basicSettings, variant baseHeuristicSettings, variantName) )
        |> List.filter (fun (baseSettings, heurSettings, _) -> baseSettings = heurSettings || ResultParsing.isCanonical heurSettings )


let alltypes = heuristics |> List.collect (fun (setting, name) -> variants |> List.map (fun (alt, altname) -> (alt setting, name + ":" + altname))) |> List.map fst 

let basicTypesWithName = variants |> List.map (fun (variant, varName) -> (LvqModelSettingsCli() |> variant, varName))
let allTypesWithName =  allvariants |> List.map (fun (variant, varName) -> (LvqModelSettingsCli() |> variant, varName))
let basicTypes = basicTypesWithName |> List.map fst
