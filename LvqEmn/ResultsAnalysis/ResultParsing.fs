module ResultParsing

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

let groupResultsByLr (results:list<DatasetResults>) = results |> Seq.collect (fun res -> res.GetLrs() )  |> Seq.toList   |> groupErrorsByLr

let coreSettingsEq (a:LvqModelSettingsCli) (b:LvqModelSettingsCli) = a.WithDefaultLr().WithDefaultSeeds().Canonicalize() =  b.WithDefaultLr().WithDefaultSeeds().Canonicalize()

let onlyFirst10results = List.filter (fun (result:DatasetResults) ->result.unoptimizedSettings.InstanceSeed < 20u)

let chooseResults (results:DatasetResults list) exampleSettings = 
    results |> List.filter (fun result -> coreSettingsEq exampleSettings result.unoptimizedSettings)

let unpackErrs (errs:TestLr.ErrorRates list) =  (errs |> List.map (fun err-> err.training), errs |> List.map (fun err -> err.test), errs |> List.map (fun err -> err.nn))

let unpackToListErrs = unpackErrs >> (fun (trnErrList, testErrList, nnErrList) -> [trnErrList; testErrList; nnErrList])

let meanStderrOfErrs errs =
    let (trnD, tstD, nnD) = unpackErrs errs |> Utils.apply3 Utils.sampleDistribution
    TestLr.ErrorRates(trnD.Mean, trnD.StdErr, tstD.Mean,tstD.StdErr,nnD.Mean,nnD.StdErr, 0.0)


let uncoveredSettings allResults alltypes = 
    allResults
    |> List.map (fun (result:DatasetResults) -> result.unoptimizedSettings)
    |> List.filter (fun settings -> not <| List.exists (coreSettingsEq settings) alltypes )
    |> List.filter (fun settings -> settings.ModelType = LvqModelType.Lgm |> not )
    |> List.map (fun settings -> settings.ToShorthand())

let isCanonical (settings:LvqModelSettingsCli) = settings.Canonicalize() = settings