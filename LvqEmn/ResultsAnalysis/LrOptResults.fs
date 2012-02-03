module LrOptResults

open LvqGui
open LvqLibCli

//---------------------------------------------------PARSING

let loadDatasetLrOptResults datasetName =
        let filepattern = "*.txt"
        TestLr.resultsDir.GetDirectories(datasetName).[0].GetFiles(filepattern)
        |> Seq.map LvqGui.LrOptimizationResult.ProcFile
        |> Seq.filter (fun res -> res <> null)
        |> Seq.toList

let groupErrorsByLr (lrs:list<LrOptimizationResult.LrAndError>) = lrs |> Utils.groupList (fun lr -> lr.LR) (fun lr -> lr.Errors)

let groupErrorsByLrForSetting (results:LrOptimizationResult list) (exampleSettings:LvqModelSettingsCli) =
    results 
        |> List.filter (fun result -> exampleSettings.WithDefaultLr().WithDefaultSeeds().Canonicalize() = result.unoptimizedSettings.WithDefaultLr().WithDefaultSeeds().Canonicalize())
        |> List.collect (fun lrOptResult ->  lrOptResult.GetLrs() |> Seq.toList) 
        |> groupErrorsByLr

let extractTrainingError (errs:TestLr.ErrorRates) = (errs.training, errs.trainingStderr)
let extractTestError (errs:TestLr.ErrorRates) = (errs.test, errs.testStderr)
let extractNnError (errs:TestLr.ErrorRates) = (errs.nn, errs.nnStderr)

let unpackToListErrs (errs:TestLr.ErrorRates list) = [errs |> List.map (fun err-> err.training); errs |> List.map (fun err -> err.test); errs |> List.map (fun err -> err.nn)]

#nowarn "25"
let meanStderrOfErrs errs =
    let [trnD; tstD; nnD] = unpackToListErrs errs |> List.map Utils.sampleDistribution
    TestLr.ErrorRates(trnD.Mean, trnD.StdErr, tstD.Mean,tstD.StdErr,nnD.Mean,nnD.StdErr, 0.0)
