#I @"ResultsAnalysis\bin\ReleaseMingw2"
#r "ResultsAnalysis"
#r "LvqLibCli"
#r "LvqGui"
#r "EmnExtensions"
#time "on"

open LvqLibCli
open LvqGui
open System.Threading
open EmnExtensions.Text
open EmnExtensions
open System.IO
open System.Collections.Generic

type Result = 
    { Iterations: float; TrainingError: float; TestError:float; NnError: float } 
    member this.CanonicalError = this.TrainingError * 0.9 + if this.NnError.IsFinite() then 0.05 * this.TestError + 0.05 * this.NnError else 0.1 * this.TestError

type ModelResults =
    { DatasetSettings: IDatasetCreator; ModelDir: DirectoryInfo; ModelSettings: LvqModelSettingsCli; Results: Result [] }
    member this.DatasetBaseShorthand = this.DatasetSettings.BaseShorthand()
    member this.MeanError = this.Results |> Array.averageBy (fun res -> res.CanonicalError)

let datasetSettings = 
    LvqStatPlotsContainer.AutoPlotDir.GetDirectories()
    |> Seq.map (fun dir -> (dir, CreateDataset.CreateFactory(dir.Name)))
    |> Seq.toArray

let inline getMaybe (dict:Dictionary<'a,'b>) key =
    let value = ref Unchecked.defaultof<'b>
    if dict.TryGetValue(key, value) then
        Some(value.Value)
    else 
        None
 
let analyzedModels = 
    let decodeLine (line:string) =
        let name = line.SubstringBefore(":")
        let nums = line.Substring(name.Length+1).Split(',') |> Array.map float
        (name, nums)
    let getLines (file:FileInfo) = 
        System.Linq.Enumerable.ToDictionary(
            System.IO.File.ReadAllLines(file.FullName) 
            |> Array.filter (fun line -> (not <| line.StartsWith("Best idx:")) && line.Length <> 0)
            |> Array.map decodeLine
            , (fun (name, nums) -> name), (fun (name, nums) -> nums))
    let getResults (file:FileInfo) =
        let lines = getLines file
        if lines.Count < 4 then 
            None
        else
            let itersArr = lines.["Training Iterations"]
            let nnErrs = getMaybe lines "Projected NN Error Rate" |> function
                | Some(arr) -> arr
                | None -> Array.create itersArr.Length Operators.nan
            let zippedResults = Array.zip itersArr (Array.zip3 lines.["Training Error"] lines.["Test Error"] nnErrs)
            Some(Array.map (fun (i,(trn,tst,nn)) -> { Iterations = i; TrainingError = trn; TestError = tst; NnError = nn }) zippedResults)
    let getStats (modeldir:DirectoryInfo) = 
        modeldir.GetFiles("fullstats-*.txt")
        |> Seq.map getResults
        |> Seq.append (Seq.singleton None)
        |> Seq.maxBy (Option.map (Array.sumBy (fun res-> res.Iterations)))
    let loadModelResults dataset modeldir = 
        match getStats modeldir with
        | None -> None
        | Some(results) -> 
            Some({
                        ModelDir = modeldir; DatasetSettings = dataset; ModelSettings = CreateLvqModelValues.SettingsFromShorthand(modeldir.Name);
                        Results = results
                    })

    datasetSettings 
    |> Seq.collect (fun (datasetdir, dataset) -> datasetdir.GetDirectories() |> Array.map (loadModelResults dataset))
    |> Seq.filter Option.isSome
    |> Seq.map Option.get
    |> Seq.toArray


let shuffle seq =
    let arr = Seq.toArray seq
    EmnExtensions.Algorithms.ShuffleAlgorithm.Shuffle arr
    arr

let iters = 100000000L

