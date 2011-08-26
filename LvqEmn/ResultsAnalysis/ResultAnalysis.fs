module ResultAnalysis

open System.IO
open EmnExtensions
open EmnExtensions.Text
open LvqGui
open LvqLibCli

type Result = 
    { Iterations: float; TrainingError: float; TestError:float; NnError: float } 
    member this.CanonicalError = this.TrainingError * 0.9 + if this.NnError.IsFinite() then 0.05 * this.TestError + 0.05 * this.NnError else 0.1 * this.TestError
    static member (+) (a, b) = { Iterations= a.Iterations + b.Iterations; TrainingError = a.TrainingError + b.TrainingError; TestError= a.TestError + b.TestError; NnError = a.NnError + b.NnError }
    static member DivideByInt (a, n) = { Iterations= a.Iterations / float n; TrainingError = a.TrainingError  / float n; TestError= a.TestError / float n; NnError = a.NnError / float n }
    static member (*) (a, x) = { Iterations= a.Iterations * x; TrainingError = a.TrainingError  * x; TestError= a.TestError * x; NnError = a.NnError * x }
    static member get_Zero () = { Iterations = 0.; TrainingError = 0.; TestError = 0.; NnError = 0. }

type ModelResults =
    {  DatasetBaseShorthand:string; DatasetTweaks:string; ModelDir: DirectoryInfo; ModelSettings: LvqModelSettingsCli; Results: Result [] }
    member this.MeanError = this.Results |> Array.averageBy (fun res -> res.CanonicalError)

let applyDatasetTweaks (tweaks:string) (settings:IDatasetCreator) = 
    let copy = settings.Clone()
    copy.ExtendDataByCorrelation <- tweaks.Contains("x")
    copy.NormalizeDimensions <- tweaks.Contains("n")
    if copy :? LoadedDatasetSettings then
        let ldSettings = copy :?> LoadedDatasetSettings
        let segmentVersion = if tweaks.Contains("N") then "segmentationNormed_" else "segmentation_"
        ldSettings.Filename <- ldSettings.Filename.Replace("segmentationX_", segmentVersion)
        if ldSettings.TestFilename <> null then
            ldSettings.TestFilename <- ldSettings.TestFilename.Replace("segmentationX_", segmentVersion)
    copy

let datasetSettings () = 
    let datasetAnnotation (settings:IDatasetCreator) segmentNorm =
        if settings.ExtendDataByCorrelation then "x" else ""
        + if settings.NormalizeDimensions then "n" else ""
        + if segmentNorm then "N" else ""
    let decodeDataset (settings:IDatasetCreator) =
        match settings.Clone() with
        | :? LoadedDatasetSettings as ldSettings ->
            if ldSettings.Filename.StartsWith("segmentationNormed_") then
                ldSettings.Filename <- ldSettings.Filename.Replace("segmentationNormed_","segmentationX_")
                if ldSettings.TestFilename <> null then
                    ldSettings.TestFilename <- ldSettings.TestFilename.Replace("segmentationNormed_","segmentationX_")
                ldSettings.NormalizeDimensions <- true
                ldSettings.DimCount <- 19
                (ldSettings.BaseShorthand(), datasetAnnotation settings true)
            elif ldSettings.Filename.StartsWith("segmentation_") then
                ldSettings.Filename <- ldSettings.Filename.Replace("segmentation_","segmentationX_")
                if ldSettings.TestFilename <> null then
                    ldSettings.TestFilename <- ldSettings.TestFilename.Replace("segmentation_","segmentationX_")
                (ldSettings.BaseShorthand(), datasetAnnotation settings false)
            else
                (ldSettings.BaseShorthand(), datasetAnnotation settings false)
        | _ -> (settings.BaseShorthand(), datasetAnnotation settings false)
    LvqStatPlotsContainer.AutoPlotDir.GetDirectories()
    |> Seq.map (fun dir -> (dir, CreateDataset.CreateFactory(dir.Name) |> decodeDataset))
    |> Seq.toArray
 
let analyzedModels () = 
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
                        ModelDir = modeldir; DatasetBaseShorthand = fst dataset; DatasetTweaks = snd dataset; ModelSettings = CreateLvqModelValues.ParseShorthand(modeldir.Name);
                        Results = results
                    })

    datasetSettings ()
    |> Seq.collect (fun (datasetdir, dataset) -> datasetdir.GetDirectories() |> Array.map (loadModelResults dataset))
    |> Seq.filter Option.isSome
    |> Seq.map Option.get
    |> Seq.toArray

let latexLiteral = sprintf @"{\footnotesize\textsf{%s}}"
let niceDatasetName = 
    let dict = new System.Collections.Generic.Dictionary<string,string>()
    dict.Add("pendigits.combined.data-16D-10,10992","pendigits")
    dict.Add("pendigits.combined.data-16Dn-10,10992","pendigits: normalized")
    dict.Add("pendigits.combined.data-16Dxn-10,10992","pendigits: extended, normalized")
    dict.Add("colorado.data-6D-14,28000","colorado")
    dict.Add("colorado.data-6Dn-14,28000","colorado: normalized")
    dict.Add("colorado.data-6Dxn-14,28000","colorado:extended, normalized")

    dict.Add("star-8D-9x10000,3(5Dr)x10i0.8n7g5[a9cd2154,]", "generated star")
    dict.Add("star-8Dxn-9x10000,3(5Dr)x10i0.8n7g5[a9cd2154,]", "generated star: extended, normalized")

    dict.Add("segmentation_test.data-19D-7,2100","segmentation")
    dict.Add("segmentationX_test.data-19D-7,2100", "segmentation")
    dict.Add("segmentationX_combined.data-19D-7,2310", "segment.\ (both)")
    dict.Add("segmentationX_train.data,segmentationX_test.data-19D-7,210^0", "segment.\ (pre-split)")
    
    
    dict.Add("segmentation_test.data-19Dn-7,2100","segmentation: normalized")
    dict.Add("segmentation_test.data-19Dxn-7,2100","segmentation: extended, normalized")
    dict.Add("segmentationNormed_test.data-19D-7,2100", "segmentation: pre-normalized")
    dict.Add("page-blocks.data-10D-5,5473", "page-blocks")
    dict.Add("page-blocks.data-10Dn-5,5473", "page-blocks: normalized")
    dict.Add("page-blocks.data-10Dxn-5,5473", "page-blocks: extended, normalized")
    dict.Add("letter-recognition.data-16D-26,20000", "letter-recognition")
    dict.Add("letter-recognition.data-16Dxn-26,20000", "letter-recognition: extended, normalized")

    dict.Add("nrm-24D-3x30000,1[5122ea19,]","gaussian cross")
    dict.Add("nrm-24Dxn-3x30000,1[5122ea19,]","gaussian cross: extended, normalized")

    dict.Add("optdigits.combined.data-64D-10,5620", "optdigits")
    dict.Add("optdigits.combined.data-64Dn-10,5620", "optdigits: normalized")
    fun datasetshorthand -> Utils.getMaybe dict datasetshorthand |> Utils.orDefault (latexLiteral datasetshorthand)
