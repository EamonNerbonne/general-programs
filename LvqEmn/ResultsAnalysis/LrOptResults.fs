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

let groupErrorsByLr (lrs:list<LrOptimizationResult.LrAndError>) = lrs |> Utils.groupList (fun lr -> lr.LR) (fun lr -> lr.Errors)

let groupErrorsByLrForSetting (results:LrOptimizationResult list) (exampleSettings:LvqModelSettingsCli) =
    results 
        |> List.filter (fun result -> exampleSettings.WithDefaultLr().WithDefaultSeeds().Canonicalize() = result.unoptimizedSettings.WithDefaultLr().WithDefaultSeeds().Canonicalize())
        |> List.collect (fun lrOptResult ->  lrOptResult.GetLrs() |> Seq.toList) 
        |> groupErrorsByLr

let extractTrainingError (errs:LrOptimizer.ErrorRates) = (errs.training, errs.trainingStderr)
let extractTestError (errs:LrOptimizer.ErrorRates) = (errs.test, errs.testStderr)
let extractNnError (errs:LrOptimizer.ErrorRates) = (errs.nn, errs.nnStderr)

let unpackToListErrs (errs:LrOptimizer.ErrorRates list) = [errs |> List.map (fun err-> err.training); errs |> List.map (fun err -> err.test); errs |> List.map (fun err -> err.nn)]

#nowarn "25"
let meanStderrOfErrs errs =
    let [trnD; tstD; nnD] = unpackToListErrs errs |> List.map Utils.sampleDistribution
    LrOptimizer.ErrorRates(trnD.Mean, trnD.StdErr, tstD.Mean,tstD.StdErr,nnD.Mean,nnD.StdErr, 0.0)
