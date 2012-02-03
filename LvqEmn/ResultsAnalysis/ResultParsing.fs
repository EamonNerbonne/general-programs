module LrOptResults

open LvqGui
open LvqLibCli

//---------------------------------------------------PARSING

let loadDatasetLrOptResults datasetName =
        let filepattern = "*.txt"
        TestLr.resultsDir.GetDirectories(datasetName).[0].GetFiles(filepattern)
        |> Seq.map LvqGui.DatasetResults.ProcFile
        |> Seq.filter (fun res -> res <> null)
        |> Seq.toList

let groupErrorsByLr (lrs:list<DatasetResults.LrAndError>) = lrs |> Utils.groupList (fun lr -> lr.LR) (fun lr -> lr.Errors)

let getLrAndErrors (lrOptResult:DatasetResults) = lrOptResult.GetLrs()

let groupResultsByLr lrOptResult = Seq.collect getLrAndErrors  >> Seq.toList  >> groupErrorsByLr <| lrOptResult

let unpackErrs (errs:TestLr.ErrorRates list) =  (errs |> List.map (fun err-> err.training), errs |> List.map (fun err -> err.test), errs |> List.map (fun err -> err.nn))

let unpackToListErrs = unpackErrs >> (fun (trnErrList, testErrList, nnErrList) -> [trnErrList; testErrList; nnErrList])

let meanStderrOfErrs errs =
    let (trnD, tstD, nnD) = unpackErrs errs |> Utils.apply3 Utils.sampleDistribution
    TestLr.ErrorRates(trnD.Mean, trnD.StdErr, tstD.Mean,tstD.StdErr,nnD.Mean,nnD.StdErr, 0.0)

let lrOptResultsForSettings (results:DatasetResults list) (exampleSettings:LvqModelSettingsCli) = 
    results |> 
        List.filter (fun result -> exampleSettings.WithDefaultLr().WithDefaultSeeds().Canonicalize() = result.unoptimizedSettings.WithDefaultLr().WithDefaultSeeds().Canonicalize())
