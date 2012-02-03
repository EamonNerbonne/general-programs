module GeneralSettings

open LvqLibCli

let allTypes =
    let defaults = LvqModelSettingsCli.defaults
    [
        defaults.WithChanges(LvqModelType.Lgm, 1)
        defaults.WithChanges(LvqModelType.Gm, 1)
        defaults.WithChanges(LvqModelType.G2m, 1)
        defaults.WithChanges(LvqModelType.Ggm, 1)
        defaults.WithChanges(LvqModelType.Lgm, 5 )
        defaults.WithChanges(LvqModelType.Gm, 5)
        defaults.WithChanges(LvqModelType.G2m, 5)
        defaults.WithChanges(LvqModelType.Ggm, 5)
    ] 

let basicTypes = allTypes |> List.filter (fun settings -> settings.ModelType <> LvqModelType.Lgm)

