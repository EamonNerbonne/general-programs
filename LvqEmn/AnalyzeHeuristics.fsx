#I @"ResultsAnalysis\bin\ReleaseMingw"
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
open System.Linq


type HeuristicsSettings = 
    { DataSettings: string; ModelSettings: LvqModelSettingsCli }
    member this.Equiv other = this.DataSettings = other.DataSettings && this.ModelSettings.ToShorthand() = other.ModelSettings.ToShorthand()
    member this.Key = this.DataSettings + "|" + (this.ModelSettings.ToShorthand())

let normalizeDatatweaks str = new System.String(str |> Seq.sortBy (fun c -> - int32 c) |> Seq.distinct |> Seq.toArray)

let getSettings (modelResults:ResultAnalysis.ModelResults) = { DataSettings = normalizeDatatweaks modelResults.DatasetTweaks; ModelSettings = modelResults.ModelSettings.WithDefaultLr().WithDefaultNnTracking()  }


type Heuristic = 
    { Name:string; Activator: HeuristicsSettings -> (HeuristicsSettings * HeuristicsSettings); }
    //member this.IsActive settings =  (this.Activator settings |> fst).Equiv settings

let applyHeuristic (heuristic:Heuristic) settings = 
    let (on, off) = heuristic.Activator settings
    if off.Equiv settings && on.Equiv settings |> not && on.ModelSettings = CreateLvqModelValues.SettingsFromShorthand(on.ModelSettings.ToShorthand()) then Some(on) else None


let toDict keyf valf xs = (Seq.groupBy keyf xs).ToDictionary(fst, snd >> valf)
let orDefault defaultValue = 
    function
    | None -> defaultValue
    | Some(value) -> value

let resultsByDatasetByModel () =
    ResultAnalysis.analyzedModels () |> toDict (fun modelRes -> modelRes.DatasetBaseShorthand) (toDict (fun modelRes -> (getSettings modelRes).Key) System.Linq.Enumerable.Single )


let heuristics = 
    let heur name activator = { Name=name; Activator = activator; }
    let heurD name letter = { Name = name; Activator = (fun s -> 
        let on = normalizeDatatweaks (s.DataSettings + letter)
        let off = s.DataSettings.Replace(letter,"")

        ({ DataSettings = on; ModelSettings = s.ModelSettings}, { DataSettings = off; ModelSettings = s.ModelSettings}))}
    let heurM name activator = 
        {
            Name = name;
            Activator = (fun s ->
                let (on, off) = activator s.ModelSettings
                ({ DataSettings = s.DataSettings; ModelSettings = on }, { DataSettings = s.DataSettings; ModelSettings = off }))
        }
    [
        heurM "NGi" (fun s  -> 
            let mutable on = s
            let mutable off = s
            on.NgInitializeProtos <- true
            off.NgInitializeProtos <- false
            (on,off))
        heurM "NG" (fun s  -> 
            let mutable on = s
            let mutable off = s
            on.NgUpdateProtos <- true
            off.NgUpdateProtos <- false
            (on, off))
        heurM "pca" (fun s  -> 
            let mutable on = s
            let mutable off = s
            on.RandomInitialProjection <- false
            off.RandomInitialProjection <- true
            (on, off))
        heurM "SlowBad" (fun s  -> 
            let mutable on = s
            let mutable off = s
            on.SlowStartLrBad <- true
            off.SlowStartLrBad <- false
            (on, off))
        heurM "NoB" (fun s  -> 
            let mutable on = s
            let mutable off = s
            on.UpdatePointsWithoutB <- true
            off.UpdatePointsWithoutB <- false
            (on, off))
        heurM "Pi" (fun s  -> 
            let mutable on = s
            let mutable off = s
            on.ProjOptimalInit <- true
            off.ProjOptimalInit <- false
            (on, off))
        heurM "Bi" (fun s  -> 
            let mutable on = s
            let mutable off = s
            on.BLocalInit <- true
            off.BLocalInit <- false
            (on, off))
        heurM "Pi+Bi" (fun s  -> 
            let mutable on = s
            let mutable off = s
            on.BLocalInit <- true
            off.BLocalInit <- false
            on.ProjOptimalInit <- true
            off.ProjOptimalInit <- false
            (on, off))
        heurM "Pi+Bi+SlowBad" (fun s  -> 
            let mutable on = s
            let mutable off = s
            on.BLocalInit <- true
            off.BLocalInit <- false
            on.ProjOptimalInit <- true
            off.ProjOptimalInit <- false
            on.SlowStartLrBad <- true
            off.SlowStartLrBad <- false
            (on, off))
        heurM "NGi+Pi" (fun s  -> 
            let mutable on = s
            let mutable off = s
            on.NgInitializeProtos <- true
            off.NgInitializeProtos <- false
            on.ProjOptimalInit <- true
            off.ProjOptimalInit <- false
            (on, off))
        heurM "NG+Pi" (fun s  -> 
            let mutable on = s
            let mutable off = s
            on.NgUpdateProtos <- true
            off.NgUpdateProtos <- false
            on.ProjOptimalInit <- true
            off.ProjOptimalInit <- false
            (on, off))
        heurM "NGi+NG" (fun s  -> 
            let mutable on = s
            let mutable off = s
            on.NgInitializeProtos <- true
            off.NgInitializeProtos <- false
            on.NgUpdateProtos <- true
            off.NgUpdateProtos <- false
            (on, off))
        heurM "NGi+SlowBad" (fun s  -> 
            let mutable on = s
            let mutable off = s
            on.NgInitializeProtos <- true
            off.NgInitializeProtos <- false
            on.SlowStartLrBad <- true
            off.SlowStartLrBad <- false
            (on, off))
        heurM "NG+SlowBad" (fun s  -> 
            let mutable on = s
            let mutable off = s
            on.NgUpdateProtos <- true
            off.NgUpdateProtos <- false
            on.SlowStartLrBad <- true
            off.SlowStartLrBad <- false
            (on, off))
        heurD "Extend" "x"
        heurD "Normalize" "n"
        heurD "SegNorm" "N"
    ]

heuristics 
    |> Seq.map (fun heur -> 
        seq {
            for datasetRes in (resultsByDatasetByModel ()).Values do
                for modelRes in datasetRes.Values do
                    let lvqSettings = getSettings modelRes 
                    let heurResMaybe = 
                        lvqSettings
                        |> applyHeuristic heur 
                        |> Option.map (fun lvqS -> lvqS.Key)
                        |> Option.bind (Utils.getMaybe datasetRes)

                    match heurResMaybe with
                    | None -> ()
                    | Some(heurRes) ->
                        let errs (model:ResultAnalysis.ModelResults) = model.Results |> Seq.map (fun res->res.CanonicalError) |> Seq.toList 
                        let hErr = errs heurRes
                        let bErr = errs modelRes
                        let (isBetter, p) = Utils.twoTailedPairedTtest hErr bErr
                        yield 
                            (
                                isBetter, 
                                (
                                    (p, (heurRes.MeanError - modelRes.MeanError) / System.Math.Max(modelRes.MeanError,heurRes.MeanError) * 100. ),
                                    lvqSettings.ModelSettings.ToShorthand() + " " + (ResultAnalysis.applyDatasetTweaks lvqSettings.DataSettings (CreateDataset.CreateFactory modelRes.DatasetBaseShorthand)).Shorthand
                                )
                            )
        }
        |> toDict fst ((Seq.map snd) >> Seq.sort >> Seq.toArray)
        |> (fun dict -> (Utils.getMaybe dict true |> orDefault (Array.empty),  Utils.getMaybe dict false |> orDefault (Array.empty)) )
        |> (fun (better, worse) ->
            (heur.Name, better.Length + worse.Length, float better.Length / float (better.Length + worse.Length), better, worse)
        )
    ) 
    |> Seq.toList
    |> List.map (fun (name, count,ratio, better, worse) ->
            sprintf @"Heuristic %s was an improvement in $%1.1f\%%$ of %i cases:\\" name (100.*ratio) count + "\n"
            + sprintf @"\noindent\begin{tabular}{|l|l|l|}\hline"  + "\n"
            + sprintf @"$p$-value & errors & scenario\\\hline"  + "\n"
            + String.concat "\\\\\n" (Array.map (fun ((p, errChange), scenario) -> sprintf @" %0.3g & %0.1f\%% & \verb/%s/ " p errChange scenario) better)
            + @"\\\hline" + "\n"
            + String.concat "\\\\\n" (Array.map (fun ((p, errChange), scenario) -> sprintf @" %0.3g & %0.1f\%% & \verb/%s/ " p errChange scenario) worse)
            + "\n\\hline\\end{tabular}\n\n"
        )
    |> String.concat ""
    |> (fun contents -> File.WriteAllText(EmnExtensions.Filesystem.FSUtil.FindDataDir(@"uni\Thesis\doc", System.Reflection.Assembly.GetAssembly(typeof<CreateDataset>)).FullName + @"\AnalyzeHeuristics.tex", contents))

