module GeneralSettings

open LvqLibCli
open LvqGui

let allvariants:list< (LvqModelSettingsCli -> LvqModelSettingsCli) * string > =
    let setProtosAndType lvqType protos (settings:LvqModelSettingsCli) = settings.WithChanges(lvqType, protos)
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



let basicTypesWithName = variants |> List.map (fun (variant, varName) -> (LvqModelSettingsCli() |> variant, varName))
let allTypesWithName =  allvariants |> List.map (fun (variant, varName) -> (LvqModelSettingsCli() |> variant, varName))
let basicTypes = basicTypesWithName |> List.map fst
