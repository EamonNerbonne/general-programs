module ResultParsing

open LvqGui
open LvqLibCli

//---------------------------------------------------PARSING

let loadAllResults datasetName =
        let filepattern = "*.txt"
        TestLr.resultsDir.GetDirectories(datasetName).[0].GetFiles(filepattern)
        |> Seq.map LvqGui.DatasetResults.ProcFile
        |> Seq.filter (fun res -> res <> null)
        |> Seq.toList

let groupResultsByLr (results:list<DatasetResults>) = 
    results
    |> List.collect (fun res -> res.GetLrs() |> Seq.toList |> List.map (fun lr -> (lr.LR, lr.Errors, res)))
    |> Utils.groupList (fun (lr, err, result) -> lr) (fun (lr, err, result) -> err )

let coreSettingsEq a b = DatasetResults.WithoutLrOrSeeds(a).ToShorthand() =  DatasetResults.WithoutLrOrSeeds(b).ToShorthand()

let onlyFirst10results = List.filter (fun (result:DatasetResults) ->result.unoptimizedSettings.InstanceSeed < 20u)

let chooseResults results exampleSettings = 
    results |> List.filter (fun (result:DatasetResults) -> coreSettingsEq exampleSettings result.unoptimizedSettings)

//let loadResultsByLr datasetName (settings:LvqLibCli.LvqModelSettingsCli) = 
//    loadAllResults datasetName |> filterResults settings  |> groupResultsByLr

let unpackErrs errs = 
    (List.map (fun (err:TestLr.ErrorRates) -> err.training) errs,
        List.map (fun (err:TestLr.ErrorRates) -> err.test) errs,
        List.map (fun (err:TestLr.ErrorRates) -> err.nn) errs)

let unpackToListErrs errs = 
    [List.map (fun (err:TestLr.ErrorRates) -> err.training) errs;
        List.map (fun (err:TestLr.ErrorRates) -> err.test) errs;
        List.map (fun (err:TestLr.ErrorRates) -> err.nn) errs]


let meanStderrOfErrs errs =
    let (trnD, tstD, nnD) = unpackErrs errs |> Utils.apply3 Utils.sampleDistribution
    TestLr.ErrorRates(trnD.Mean, trnD.StdErr, tstD.Mean,tstD.StdErr,nnD.Mean,nnD.StdErr, 0.0)


let uncoveredSettings allResults alltypes = 
    allResults
    |> List.map (fun (result:DatasetResults) -> result.unoptimizedSettings)
    |> List.filter (fun settings -> not <| List.exists (coreSettingsEq settings) alltypes )
    |> List.filter (fun settings -> settings.ModelType = LvqModelType.Lgm |> not )
    |> List.map (fun settings -> settings.ToShorthand())

let isCanonical (settings:LvqModelSettingsCli) = CreateLvqModelValues.SettingsFromShorthand(settings.ToShorthand()) = settings