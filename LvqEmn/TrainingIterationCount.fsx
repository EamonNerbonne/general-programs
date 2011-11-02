#I @"ResultsAnalysis\bin\ReleaseMingw"
#r "ResultsAnalysis"
#r "LvqLibCli"
#r "LvqGui"

open LvqLibCli
open LvqGui    

let datasetResults =
        let filepattern = "*.txt"
        TestLr.resultsDir.GetDirectories()
        |> Seq.filter (fun dir -> dir.Name <> "base")
        |> Seq.collect (fun dir-> dir.GetFiles("*.txt"))
        |> Seq.map LvqGui.DatasetResults.ProcFile
        |> Seq.filter (fun res -> res <> null)
        |> Seq.toList

let baseDatasetResults =
        let filepattern = "*.txt"
        TestLr.resultsDir.GetDirectories("base").[0].GetFiles("*.txt")
        |> Seq.map LvqGui.DatasetResults.ProcFile
        |> Seq.filter (fun res -> res <> null)
        |> Seq.toList

let lrOptIters (results:list<DatasetResults>) = 
    results
    |> Seq.map (fun datasetResults ->  (datasetResults.GetLrs () |> Seq.length |> float) * datasetResults.trainedIterations)
    |> Seq.sum

let resAnalysisIters = 
    ResultAnalysis.analyzedModels ()
    |> Seq.map (fun res -> res.Results)
    |> Seq.concat
    |> Seq.sumBy (fun res -> res.Iterations)

let totalIters = 3. * lrOptIters datasetResults +  7. * lrOptIters baseDatasetResults + 10.* resAnalysisIters