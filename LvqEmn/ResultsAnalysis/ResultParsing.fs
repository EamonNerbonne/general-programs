module ResultParsing

open LvqGui
open LvqLibCli

//---------------------------------------------------PARSING

let loadAllResults datasetName =
    let filepattern = "*.txt"
    TestLr.resultsDir.GetDirectories(datasetName).[0].GetFiles(filepattern)
    |> Seq.map LvqGui.DatasetResults.ProcFile
    |> Seq.toList

let groupResultsByLr (results:list<DatasetResults>) = 
    results
    |> List.collect (fun res -> res.GetLrs() |> Seq.toList |> List.map (fun lr -> (lr.Lr, lr.Errors, res)))
    |> Utils.groupList (fun (lr, err, result) -> lr) (fun (lr, err, result) -> err )

let coreSettingsEq a b = DatasetResults.WithoutLrOrSeeds(a).ToShorthand() =  DatasetResults.WithoutLrOrSeeds(b).ToShorthand()
let filterResults exampleSettings results = 
    results |> List.filter (fun (result:DatasetResults) -> coreSettingsEq exampleSettings result.unoptimizedSettings && result.unoptimizedSettings.InstanceSeed < 20u)

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
    let (trns, tsts, nns) = unpackErrs errs
    let (trnM, trnE) = Utils.meanStderr trns
    let (tstM, tstE) = Utils.meanStderr tsts
    let (nnM, nnE) = Utils.meanStderr nns
    TestLr.ErrorRates(trnM,trnE,tstM,tstE,nnM,nnE, 0.0)


let uncoveredSettings allResults alltypes = 
    allResults
    |> List.map (fun (result:DatasetResults) -> result.unoptimizedSettings)
    |> List.filter (fun settings -> not <| List.exists (coreSettingsEq settings) alltypes )
    |> List.filter (fun settings -> settings.ModelType = LvqModelType.Lgm |> not )
    |> List.map (fun settings -> settings.ToShorthand())
