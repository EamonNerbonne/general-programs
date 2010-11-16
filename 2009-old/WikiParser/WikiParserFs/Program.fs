// Learn more about F# at http://fsharp.net
 (*
 #r "System.Xml";;
 #r "System.Xml.Linq";;
 #r @"D:\EamonLargeDocs\VersionControlled\docs-trunk\programs\EmnExtensionsWpf\bin\x86\Release\EmnExtensionsWpf.dll";;
 #r @"D:\EamonLargeDocs\VersionControlled\docs-trunk\programs\EmnExtensionsWpf\bin\x86\Release\EmnExtensionsWpf.dll";;
 
 #r "WindowsBase";;
 #r "PresentationCore";;
 #r "PresentationFramework";;
 #r "System.Xaml";;
 *)

open System
open System.Xml.Linq
open System.Xml
open System.IO
open EmnExtensions.Wpf
open EmnExtensions.Wpf.Plot
open EmnExtensions.Wpf.Plot.VizEngines
open System.Windows

let streamTopLevelEls (fi :FileInfo) filter = 
    seq {
        use xmlReader:XmlReader = fi.OpenRead() |> XmlReader.Create
        while xmlReader.Read() do
            if xmlReader.NodeType = XmlNodeType.Element && filter xmlReader then
                yield  XNode.ReadFrom xmlReader :?> XElement
    }

let xn s = XName.Get(s)
let wikiNamespace = XNamespace.Get("http://www.mediawiki.org/xml/export-0.4/")
let wikiFile = new FileInfo(@"D:\EamonLargeDocs\wikipedia\enwiki-latest-pages-articles.xml")

let wikiPages = streamTopLevelEls wikiFile (fun reader -> reader.LocalName = "page")

wikiPages |>Seq.map (fun xml -> xml.ToString()) |> Seq.head |> printfn "%s"



let plot = 
    Plot.Create(
        new PlotMetaData(
            DataLabel = "wikiData",
            XUnitLabel = "Letter",
            YUnitLabel = "Freq",
            AxisBindings = (TickedAxisLocation.BelowGraph ||| TickedAxisLocation.LeftOfGraph)
        ), new VizEngines.VizPixelScatterSmart())

(plot.Visualisation :?> VizEngines.IVizPixelScatter).CoverageRatio <- 0.9
(plot.Visualisation :?> VizEngines.IVizPixelScatter).CoverageGradient <- 1.0


let plotControl = new PlotControl( GraphsEnumerable = Seq.singleton (plot :> IPlot) )

let plotWin = 
    new Window(
        Title = "WikiPlot",
        Content = plotControl
    )

plotWin.Show()

let maxInteresting = 128
let charCodes=
    wikiPages 
        |> Seq.collect (fun xml -> xml.Value) 
        |> Seq.map int
        |> Seq.filter (fun n->n<maxInteresting)
        |> Seq.take 1000000000

let countArr = Array.create maxInteresting 0
for code in charCodes do 
    countArr.[code]<-countArr.[code]+1

let charFreq = 
    let charToPoint i count = new Point(float i, float count)
    let nonZero (p:Point) = p.Y<>0.
    countArr |> Array.toSeq |>Seq.mapi  charToPoint |> Seq.filter nonZero |> Seq.toArray

charFreq |> plot.Visualisation.ChangeData 

