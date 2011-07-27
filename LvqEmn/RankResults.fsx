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
    static member (+) (a, b) = { Iterations= a.Iterations + b.Iterations; TrainingError = a.TrainingError + b.TrainingError; TestError= a.TestError + b.TestError; NnError = a.NnError + b.NnError }
    static member DivideByInt (a, n) = { Iterations= a.Iterations / float n; TrainingError = a.TrainingError  / float n; TestError= a.TestError / float n; NnError = a.NnError / float n }
    static member (*) (a, x) = { Iterations= a.Iterations * x; TrainingError = a.TrainingError  * x; TestError= a.TestError * x; NnError = a.NnError * x }
    static member get_Zero () = { Iterations = 0.; TrainingError = 0.; TestError = 0.; NnError = 0. }

type ModelResults =
    { DatasetSettings: IDatasetCreator; ModelDir: DirectoryInfo; ModelSettings: LvqModelSettingsCli; Results: Result [] }
    member this.DatasetBaseShorthand = this.DatasetSettings.BaseShorthand()
    member this.MeanError = this.Results |> Array.averageBy (fun res -> res.CanonicalError)
    
let fixSegmentation (settings:IDatasetCreator) =
    match settings.Clone() with
    | :? LoadedDatasetSettings as ldSettings ->
        if ldSettings.Filename.StartsWith("segmentationNormed_") then
            ldSettings.Filename <- ldSettings.Filename.Replace("segmentationNormed_","segmentationX_")
            ldSettings.NormalizeDimensions <- true
            ldSettings.DimCount <- 19
            ldSettings :> IDatasetCreator
        elif ldSettings.Filename.StartsWith("segmentation_") then
            ldSettings.Filename <- ldSettings.Filename.Replace("segmentation_","segmentationX_")
            ldSettings :> IDatasetCreator
        else
            settings
    | _ -> settings


let datasetSettings = 
    LvqStatPlotsContainer.AutoPlotDir.GetDirectories()
    |> Seq.map (fun dir -> (dir, CreateDataset.CreateFactory(dir.Name) |> fixSegmentation))
    |> Seq.toArray
 
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
            let nnErrs = Utils.getMaybe lines "Projected NN Error Rate" |> function
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


let datasetAnnotation (settings:IDatasetCreator) =
    if settings.ExtendDataByCorrelation then "x" else ""
    + if settings.NormalizeDimensions then "n" else ""

for resStr in
            analyzedModels 
                |> Seq.groupBy (fun res-> res.DatasetBaseShorthand) 
                |> Seq.map 
                    (fun (key, group) ->
                        let sortedGroup = group |> Seq.sortBy (fun res -> res.MeanError)
                        let mappedGroup = sortedGroup |> Seq.map (fun res -> 
                            let avgResult = (res.Results |> Array.average) * 100.
                            "   " + datasetAnnotation res.DatasetSettings + sprintf " %s: %1.1f/%1.1f/%1.1f" (res.ModelSettings.ToShorthand()) avgResult.TrainingError avgResult.TestError avgResult.NnError
                            )
                        key + "\n" + String.concat "\n" mappedGroup + "\n\n"
                    )
        do
    printfn "%s" resStr
    

