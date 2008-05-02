#light

module FsWpfSample
#I @"C:\Program Files\Reference Assemblies\Microsoft\Framework\v3.0"
#r @"WindowsBase.dll"
#r @"PresentationCore.dll"
#r @"PresentationFramework.dll"

open System
open System.Windows
open System.Windows.Input

type SampleApplication =
  inherit Application as base
  new() as this = {}
  
  override x.OnStartup args =
    base.OnStartup(args)
    let win = new Window()
    win.Title <- "Sample Application"
    win.Show()
    win.add_Closing (fun e cancelEventArgs ->
                        let result = 
                          MessageBox.Show("Do you really want to quit?", x.MainWindow.Title, MessageBoxButton.YesNo,
                                          MessageBoxImage.Question, MessageBoxResult.No)
                        cancelEventArgs.Cancel <- (result=MessageBoxResult.No)
                        )

  
  override x.OnSessionEnding cancelArgs =
    base.OnSessionEnding(cancelArgs)
    let result = MessageBox.Show("Do you really want to quit?", base.MainWindow.Title, 
                                 MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No)
    cancelArgs.Cancel <- (result=MessageBoxResult.No)
      
  
#if COMPILED
[<STAThread()>]
do 
    let app =  new SampleApplication() in
    app.Run() |> ignore
#endif

