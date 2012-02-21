#I @"ResultsAnalysis\bin\ReleaseMingw"
#r "EmnExtensionsWpf"
#r "PresentationCore"
#r "WindowsBase"
#r "PresentationFramework"
#r "System.Xaml"

open System
open EmnExtensions.Wpf




let lr epochs pwr scale = Math.Pow ( 1. + scale * epochs, pwr)
let lrsum epochs pwr scale  =  ( Math.Pow(1. + scale * epochs, pwr + 1.) - 1.) / (scale * ( pwr + 1.) )

lr 1e7 -0.75 0.00002 
lrsum 1e7 -0.75 0.00002 

let lrA epochs decay scale =  Math.Pow ( 1. + scale * epochs, - (decay*2.+1.)/(decay*2.+2.))
let lrAsum epochs decay scale = (decay*2. + 2.) / scale* (Math.Pow(1. + scale * epochs, 1. / ( decay*2. + 2.)) - 1.)

lrA 1e7 2. 0.00002
lrAsum 1e7 2. 0.00002

let makeWindow initializer = 
    let dispatcher = WpfTools.StartNewDispatcher(Threading.ThreadPriority.BelowNormal)
    dispatcher.BeginInvoke (
        new Action(fun () ->
            let window = new System.Windows.Window()
            window.Closed.Add ( fun _ -> dispatcher.BeginInvokeShutdown (Windows.Threading.DispatcherPriority.ApplicationIdle))
            window.Topmost <- true
            window.Loaded.Add (fun _ -> 
                window.Activate () |> ignore
                window.Topmost <- false
                printfn "showed"
                )
            initializer window
            window.Show ()
            ), [| |])

let makePlots pointArrs =
    makeWindow (fun window ->
        let plot = new PlotControl ()
        plot.ShowGridLines <- true
        for pointsArr in pointArrs do
            let engine = Plot.CreateLine ()
            engine.ChangeData pointsArr
            plot.Graphs.Add engine
        window.Content <- plot
        )
    

let makePlot points = 
    let pointsArr = points |> Seq.map (fun (x,y) -> new Windows.Point (x, y)) |> Seq.toArray
    makeWindow (fun window ->
        let plot = new PlotControl ()
        plot.ShowGridLines <- true
        let engine = Plot.CreateLine ()
        engine.ChangeData pointsArr
        plot.Graphs.Add engine
        window.Content <- plot
        )

let norm =
    let decay = 1.0
    Math.Sqrt ((decay*2.0 + 3.5) / (decay*2.0 + 0.3)) * Math.Sqrt( Math.Sqrt ((decay*2.0 + 2.5) / (decay*2.0 + 0.15)))

Seq.init 300 (fun i -> 
        let rescale decay = (3./4.) * (decay*2. + 2.) / (decay*2. + 1.) // * Math.Sqrt ((pwr + 3.5) / (pwr + 0.3)) * Math.Sqrt( Math.Sqrt ((pwr + 2.5) / (pwr + 0.15)))

        let protos = 2
        let classes = 10
        let decay = 1.
        let scale = 0.0001

        let iter = 10000. * float i

        //let k = 0.00002 / (0.8 * Math.Log (pwr*4. + 1.2)) // Math.Sqrt (Math.Sqrt (pwr + 0.1 ) )
        let k =  scale / Math.Sqrt (protos * classes |> float) / rescale decay

        let y = 0.01 * lrAsum iter decay k
        (iter, y)
    ) 
    |> makePlot

Seq.init 5000 (fun i -> 
        let x = float i / 50000.0
        let y = Math.Exp x
        (x, y) 
    ) 
    |> makePlot
