#I @"ResultsAnalysis\bin\ReleaseMingw"
#r "ResultsAnalysis"
#r "LvqLibCli"
#r "LvqGui"

#r "PresentationCore"
#r "System.Xaml"
#r "PresentationFramework"
#r "WindowsBase"
#r "EmnExtensionsWpf"
#r "EmnExtensions"
#time "on"

open LvqLibCli
open LvqGui
open System.Threading
open System.Threading.Tasks
open System.Windows.Threading
open System.Windows
open System
open System.Xaml
open System.Windows.Media
open EmnExtensions.Wpf
open EmnExtensions.Wpf.Plot
open EmnExtensions.Wpf.Plot.VizEngines


let dispatcher:Dispatcher = WpfTools.StartNewDispatcher(ThreadPriority.BelowNormal)
let scheduler:TaskScheduler = dispatcher.GetScheduler().Result

let schedule func = (Task.Factory.StartNew (func, CancellationToken.None, TaskCreationOptions.None, scheduler))
//let schedule (action:Action) = (Task.Factory.StartNew (action, CancellationToken.None, TaskCreationOptions.None, scheduler))


let datasets = 
    [
        for datasetFactory in CreateDataset.StandardDatasets() do
            datasetFactory.NormalizeDimensions <- true
            yield datasetFactory.CreateDataset()
    ]
           
let makeLvqSettings modelType prototypes lrB lrP lr0 = 
    let mutable tmp = new LvqModelSettingsCli()
    tmp.NgInitializeProtos <- true
    tmp.SlowStartLrBad <- true
    tmp.RandomInitialProjection <- true
    tmp.ModelType <- modelType
    tmp.PrototypesPerClass <- prototypes
    tmp.LR0 <- lr0
    tmp.LrScaleB <- lrB
    tmp.LrScaleP <- lrP
    tmp
    

let Ggm1 = makeLvqSettings LvqModelType.Ggm 1
let Ggm5 = makeLvqSettings LvqModelType.Ggm 5
let G2m1 = makeLvqSettings LvqModelType.G2m 1
let G2m5 = makeLvqSettings LvqModelType.G2m 5
let Gm1 = makeLvqSettings LvqModelType.Gm 1 0.
let Gm5 = makeLvqSettings LvqModelType.Gm 5 0.

type TestResults = { GeoMean:float; Mean:float;  Results:TestLr.ErrorRates list; Settings:LvqModelSettingsCli;}

let iterCount = 1e7

let testSettings settings =
    let results =
        [
            for dataset in datasets do
                let model = new LvqMultiModel(dataset,settings,false)
                model.TrainUptoIters(iterCount,dataset, CancellationToken.None)
                yield model.CurrentErrorRates(dataset)
        ]
    let averageErr= results|> List.averageBy (fun res->res.CanonicalError)
    let geomAverageErr= results|> List.averageBy  (fun res-> Math.Log res.CanonicalError) |> Math.Exp
    { GeoMean = geomAverageErr; Mean = averageErr;Settings = settings; Results = results}


let logscale steps (v0, v1) = 
    let lnScale = Math.Log(v1 / v0)
    [ for i in [0..steps-1] -> v0 * Math.Exp(lnScale * (float i / (float steps - 1.))) ]

    //[0.001 -> 0.1]

let lrsChecker lr0range settingsFactory = 
    [ for lr0 in lr0range -> async { return (lr0 |> settingsFactory |> testSettings) } ]
    |> Async.Parallel 
    |> Async.RunSynchronously
    |> Array.sortBy (fun res -> res.GeoMean)

let lrs = lrsChecker (logscale 100 (0.001,0.1)) (G2m5 0.1 0.01)

let lrs1 = lrsChecker (logscale 30 (0.0002,0.002)) (G2m5 0.1 0.01)
let lrs2 = lrsChecker (logscale 10 (0.001,0.002)) (G2m5 0.1 0.01)

let lrs3 = lrsChecker (logscale 10 (0.01,2.)) (fun lrB -> G2m5 lrB 0.01 0.001714487966)


let plotControl = 
    (Task.Factory.StartNew (fun () -> 
        let plotControl=  new PlotControl ()
        let window = new System.Windows.Window () 
        window.Content <- plotControl
        window.Show ()
        plotControl
    , CancellationToken.None, TaskCreationOptions.None, scheduler
    )).Result

let plot = 
    (
        schedule 
            (fun () ->
                let plotviz:IPlot = 
                    let plotmetadata = new PlotMetaData ()
                    let viz = new VizLineSegments ()
                    Plot.Create(plotmetadata, viz)
                plotControl.Graphs.Add(plotviz)
                plotviz
            )
    ).Result

Task.Factory.StartNew (fun () -> 
    let plotviz = 
        let plotmetadata = new PlotMetaData ()
        let viz = new VizLineSegments ()

    plotControl.Graphs.Add(plotviz)
    , CancellationToken.None, TaskCreationOptions.None, scheduler
    )
let plotControl.Graphs.[0]