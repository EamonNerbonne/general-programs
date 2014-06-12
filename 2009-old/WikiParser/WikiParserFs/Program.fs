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

module CharFreqHistogram
open System
open System.Xml.Linq
open System.Xml
open System.IO
open EmnExtensions.Wpf
open EmnExtensions.Wpf
open EmnExtensions.Wpf.VizEngines
open System.Windows
open System.Collections.Generic

let streamTopLevelEls (fi :FileInfo) filter = 
    seq {
        use xmlReader:XmlReader = fi.OpenRead() |> XmlReader.Create
        while xmlReader.Read() do
            if xmlReader.NodeType = XmlNodeType.Element && filter xmlReader then
                yield  XNode.ReadFrom xmlReader :?> XElement
    }

//wikiPages |>Seq.map (fun xml -> xml.ToString()) |> Seq.head |> printfn "%s"



let asyncHisto syncContext (wikiPages:seq<XElement>) sink =
    let maxInterestingCharCode = 128
    let initarr() = Array.zeroCreate<int> maxInterestingCharCode

    let charFreqArr (arr : array<int>) charCodes  = 
        for code in charCodes do 
            if code < arr.Length then arr.[code]<-arr.[code]+1

    let charFreqPoints (arr : array<int>) pageText = 
        pageText 
            |> Seq.map int
            |> charFreqArr arr
        
    let charToPoint sum i count = new Point(float i, float count / sum)

    async {
        let arr = initarr()
        for page in wikiPages do
            let points = charFreqPoints arr page.Value
            let sum = arr |> Array.map float|> Array.sum
            do! Async.SwitchToContext(syncContext)
            arr |> Array.mapi (charToPoint sum) |> sink
            do! Async.SwitchToThreadPool()
    }


let main() =

    //let xn s = XName.Get(s)
//    let wikiNamespace = XNamespace.Get("http://www.mediawiki.org/xml/export-0.4/")
    let metadata = new PlotMetaData( DataLabel = "wikiData", XUnitLabel = "Letter", YUnitLabel = "Freq")
    let scatterViz = Plot.CreateLine(metadata, CoverageRatioY=1.0)
    let plotWin = 
        
        let plotControl = new PlotControl( ShowGridLines=true, GraphsEnumerable = (scatterViz |> Seq.singleton |> Seq.cast) ) 
        let win = new Window(Title = "WikiPlot", Content = plotControl)
        win.Show()
        win

    
    let wikiFile = new FileInfo(@"F:\wikipedia\enwiki-latest-pages-articles.xml")
    let wikiPages = streamTopLevelEls wikiFile (fun reader -> reader.LocalName = "page")
    let syncContext = System.Threading.SynchronizationContext.Current

    asyncHisto syncContext wikiPages scatterViz.ChangeData |> Async.Start


#if COMPILED
[<STAThread()>]
do 
    let app =  new Application()
    app.Startup.Add (fun _ -> main())
    app.Run() |> ignore
#else 
do main()

#endif
