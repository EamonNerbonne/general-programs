﻿#I @"ResultsAnalysis\bin\ReleaseMingw"
#r "EmnExtensionsWpf"
#r "EmnExtensions"
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


let toPoint (x,y) = new Windows.Point (x, y)
let toPoints x =  x |> Seq.map toPoint |> Seq.toArray

let makePlots pointArrs =
    makeWindow (fun window ->
        let plot = new PlotControl ()
        plot.ShowGridLines <- true
        for (name, pointsArr) in pointArrs do
            let engine = Plot.CreateLine ( new PlotMetaData ( DataLabel = name ) )
            engine.ChangeData pointsArr
            plot.Graphs.Add engine
        plot.AutoPickColors(EmnExtensions.MathHelpers.RndHelper.ThreadLocalRandom)
        window.Content <- plot
        )
    

let makePlot points = 
    let pointsArr = toPoints points
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

[0.0; 0.1; 0.5; 1.0; 2.0; 10.0; 1000.0;]
    |> List.map (fun decay ->
            let graph =
                Seq.init 1000 (fun i -> 
                        let rescale decay = (3./4.) * (decay*2. + 2.) / (decay*2. + 1.) //( Math.Sqrt ((decay*2.  + 3.5) / (decay*2.  + 0.3)) * Math.Sqrt( Math.Sqrt ((decay*2.  + 2.5) / (decay*2.  + 0.15))))

                        let protos = 2
                        let classes = 10
                        //let decay = 0.1
                        let scale = 0.0001

                        let iter = 100000. * float i

                        //let k = 0.00002 / (0.8 * Math.Log (pwr*4. + 1.2)) // Math.Sqrt (Math.Sqrt (pwr + 0.1 ) )
                        let k =  scale / Math.Sqrt (protos * classes |> float) * rescale decay

                        let y =  lrAsum iter decay k
                        (iter, y)
                    ) |> toPoints 
            ("decay: " + decay.ToString(), graph)
            )
                |> makePlots

Seq.init 5000 (fun i -> 
        let x = float i / 50000.0
        let y = Math.Exp x
        (x, y) 
    ) 
    |> makePlot
