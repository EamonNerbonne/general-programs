module LrOptResults

open LvqGui
open LvqLibCli

//---------------------------------------------------PARSING

let loadDatasetLrOptResults datasetName =
        let filepattern = "*.txt"
        LrOptimizer.resultsDir.GetDirectories(datasetName).[0].GetFiles(filepattern)
        |> Seq.map LvqGui.LrOptimizationResult.ProcFile
        |> Seq.filter (fun res -> res <> null)
        |> Seq.toList

let groupErrorsByLr (lrs:list<LrAndError>) = lrs |> Utils.groupList (fun lr -> lr.LR) (fun lr -> lr.Errors)

let groupErrorsByLrForSetting (results:LrOptimizationResult list) (exampleSettings:LvqModelSettingsCli) =
    results 
        |> List.filter (fun result -> exampleSettings.WithCanonicalizedDefaults() = result.unoptimizedSettings.WithCanonicalizedDefaults())
        |> List.collect (fun lrOptResult ->  lrOptResult.GetLrs() |> Seq.toList) 
        |> groupErrorsByLr

let extractTrainingError (errs:ErrorRates) = (errs.training, errs.trainingStderr)
let extractTestError (errs:ErrorRates) = (errs.test, errs.testStderr)
let extractNnError (errs:ErrorRates) = (errs.nn, errs.nnStderr)

let unpackToListErrs (errs:ErrorRates list) = [errs |> List.map (fun err-> err.training); errs |> List.map (fun err -> err.test); errs |> List.map (fun err -> err.nn)]

#nowarn "25"
let meanStderrOfErrs errs =
    let [trnD; tstD; nnD] = unpackToListErrs errs |> List.map Utils.sampleDistribution
    ErrorRates(trnD.Mean, trnD.StdErr, tstD.Mean,tstD.StdErr,nnD.Mean,nnD.StdErr)
