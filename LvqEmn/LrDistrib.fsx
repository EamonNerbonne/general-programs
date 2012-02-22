#I @"ResultsAnalysis\bin\ReleaseMingw2"
#r "ResultsAnalysis"
#r "LvqLibCli"
#r "LvqGui"

open LvqLibCli
open LvqGui    
//open HeuristicAnalysis

let allLrOptResults =
        LrOptimizer.resultsDir.GetDirectories()
        |> Seq.filter (fun dir -> dir.Name <> "base")
        |> Seq.collect (fun dir-> dir.GetFiles("*.txt"))
        |> Seq.map LvqGui.LrOptimizationResult.ProcFile
        |> Seq.filter (fun res -> res <> null && res.trainedIterations > 2.e7 && res.trainedIterations < 4.e7)
        |> Seq.toList

let lrTestingResults = 
    [
        for lrOptResult in allLrOptResults do
            let dFactory = CreateDataset.CreateFactory lrOptResult.resultsFile.Directory.Name
            let (dataKey, dataHeur) =  LvqRunAnalysis.decodeDatasetSettingsAndName dFactory
            yield (dataKey, dataHeur, lrOptResult.unoptimizedSettings,  lrOptResult.GetLrs() |> Seq.toArray)
    ]

let hasNoHeuristics (settings:LvqModelSettingsCli) = settings.WithCanonicalizedDefaults() = LvqModelSettingsCli.defaults


let plainLrTestingResults =
    let isBasicType = (new System.Collections.Generic.HashSet<LvqModelSettingsCli>(ModelSettings.coreProjectingModelSettings)).Contains
    lrTestingResults |> List.filter (fun (_,dHeur,settings,_) -> (dHeur = "" || dHeur="n") && isBasicType settings)   


let plainCompleteLrTestingResults = 
    plainLrTestingResults 
    |> Utils.groupList (fun (dKey,_,_,_) -> dKey) id
    |> List.filter (fun rs -> List.length (snd rs) = 12) // only include datasets with all basic combos
    |> List.collect snd

let relevantDatasets = List.map (fun (x, _,_,_) ->x) plainCompleteLrTestingResults |> Set.ofList |> Set.toList |> List.map (fun x-> defaultArg (LvqRunAnalysis.friendlyDatasetName x) x)

let trainingErr (errs:ErrorRates) = errs.training

let meanBestLrLookup = 
    plainCompleteLrTestingResults
        |> Utils.groupList (fun (dKey,dHeur,mSettings,lrs) -> (dHeur, mSettings.ToShorthand()))  (fun (dKey,dHeur,mSettings,lrs) -> lrs)
        |> List.map 
            (fun (key, lrs) ->
                let (lr, trainErr) = 
                    Seq.collect id lrs 
                        |> List.ofSeq
                        |> LrOptResults.groupErrorsByLr
                        |> List.map (Utils.apply2nd (LrOptResults.meanStderrOfErrs >> (fun err-> (err.training, err.test))))
                        |> List.sortBy snd
                        |> List.head
                let trainErrOfBestLr = 
                    lrs |> List.map 
                        (fun oneDatasetResultArr -> 
                            let (bestTrainErr, testErrForBestTrainErr) =
                                oneDatasetResultArr 
                                |> Array.map (fun lrAndErr -> (lrAndErr.Errors.training,lrAndErr.Errors.test) )
                                |> Array.min
                            let errForStdLr = 
                                oneDatasetResultArr
                                |> Seq.filter (fun xyz -> xyz.LR = lr)
                                |> Seq.head

                            ((bestTrainErr,100. - 100.*bestTrainErr/errForStdLr.Errors.training), (testErrForBestTrainErr,100. - 100.*testErrForBestTrainErr/errForStdLr.Errors.test))
                        )
                    |> List.unzip
                    |> Utils.apply2 (List.unzip >> (Utils.apply2 List.average))
                    
                (key,(lr, trainErr,trainErrOfBestLr))
            )

meanBestLrLookup
    |> Utils.groupList (fst>>snd) (Utils.apply1st fst)
    |> List.map
        (fun (mKey, sublist) ->
            let (rLr, rMedErr, rBestErr) = sublist |> List.filter (fst>> (fun x -> x = "")) |> List.head |> snd
            let (nLr, nMedErr, nBestErr) = sublist |> List.filter (fst>> (fun x -> x = "n")) |> List.head |> snd
            let toPerc x= x*100.
            sprintf @"%s & $%.1f \%%$ & $%.1f \%%$ & $%.1f \%%$ & $%.1f \%%$ \\"
                mKey //model
                (rBestErr |> fst |>fst |>toPerc) //non-normalized mean training error
                (rBestErr |> snd |>fst |>toPerc) //non-normalized mean test error
                (nBestErr |> fst |>fst |>toPerc)//normalized mean training error
                (nBestErr |> snd |>fst |>toPerc)//normalized mean test error
        )
    |> String.concat "\n"
    |> printfn "%s"

meanBestLrLookup
    |> Utils.groupList (fst>>snd) (Utils.apply1st fst)
    |> List.map
        (fun (mKey, sublist) ->
            let (rLr, rMedErr, rBestErr) = sublist |> List.filter (fst>> (fun x -> x = "")) |> List.head |> snd
            let (nLr, nMedErr, nBestErr) = sublist |> List.filter (fst>> (fun x -> x = "n")) |> List.head |> snd
            let toPerc x= x*100.
            sprintf @"%s & $%.1f \%%$ & $%.1f \%%$ & $%.1f \%%$ & $%.1f \%%$ \\" //disabling lr-optimization accuracy changes:
                mKey //model
                (rBestErr |> fst|>snd) //...on non-normalized training error
                (rBestErr |> snd |>snd) //...on non-normalized test error
                (nBestErr |> fst|>snd) //...on normalized training error
                (nBestErr |> snd |>snd) //...on normalized test error
        )
    |> String.concat "\n"
    |> printfn "%s"

meanBestLrLookup
    |> Utils.groupList (fst>>snd) (Utils.apply1st fst)
    |> List.map
        (fun (mKey, sublist) ->
            let (rLr, rMedErr, rBestErr) = sublist |> List.filter (fst>> (fun x -> x = "")) |> List.head |> snd
            let (nLr, nMedErr, nBestErr) = sublist |> List.filter (fst>> (fun x -> x = "n")) |> List.head |> snd
            let toPerc x= x*100.
            
            sprintf @"%s & $%.3f $ & $%.3f $ & $%.3f $ & $%.3f $ & $%.3f $ & $%.3f $ \\"
                mKey rLr.Lr0 rLr.LrP rLr.LrB nLr.Lr0  nLr.LrP nLr.LrB //optimal general LR on non-normalized, then normalized datasets. blue:max, green:min search range.
        )
    |> String.concat "\n"
    |> printfn "%s"
