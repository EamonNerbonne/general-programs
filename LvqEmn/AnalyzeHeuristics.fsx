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



type LvqSettings = 
    { DataSettings: string; ModelSettings: LvqModelSettingsCli }
    member this.Equiv other = this.DataSettings = other.DataSettings && this.ModelSettings.ToShorthand() = other.ModelSettings.ToShorthand()
    member this.Key = this.DataSettings + "|" + (this.ModelSettings.ToShorthand())

let normalizeDatatweaks str = new System.String(str |> Seq.sortBy (fun c -> - int32 c) |> Seq.distinct |> Seq.toArray)

let getSettings (modelResults:ResultAnalysis.ModelResults) = { DataSettings = normalizeDatatweaks modelResults.DatasetTweaks; ModelSettings = modelResults.ModelSettings }


type Heuristic = 
    { Name:string; Activator: LvqSettings -> LvqSettings }
    member this.IsActive settings =  (this.Activator settings).Equiv settings

let applyHeuristic (heuristic:Heuristic) settings = 
    let activated = heuristic.Activator settings
    if activated.Equiv settings then None else Some(activated)

let heuristics = 
    let heur name activator = { Name=name; Activator= activator; }
    let heurD name letter = { Name = name; Activator = (fun s -> 
        {
            DataSettings = normalizeDatatweaks (s.DataSettings + letter)
            ModelSettings = s.ModelSettings
        })}
    let heurM name activator = 
        {
            Name = name;
            Activator = (fun s ->
                {
                    DataSettings = s.DataSettings; 
                    ModelSettings = activator s.ModelSettings
                })
        }
    [
        heurM "NGi" (fun s  -> 
            let mutable ns = s
            ns.NgInitializeProtos <- true
            ns)
        heurM "NG" (fun s  -> 
            let mutable ns = s
            ns.NgUpdateProtos <- true
            ns)
        heurM "pca" (fun s  -> 
            let mutable ns = s
            ns.RandomInitialProjection <- false
            ns)
        heurM "SlowBad" (fun s  -> 
            let mutable ns = s
            ns.SlowStartLrBad <- true
            ns)
        heurM "NoB" (fun s  -> 
            let mutable ns = s
            ns.UpdatePointsWithoutB <- true
            ns)
        heurM "Pi" (fun s  -> 
            let mutable ns = s
            ns.ProjOptimalInit <- true
            ns)
        heurM "Bi" (fun s  -> 
            let mutable ns = s
            ns.BLocalInit <- true
            ns)
        heurD "Extend" "x"
        heurD "Normalize" "n"
        heurD "SegNorm" "N"
    ]

let toDict keyf valf xs = (Seq.groupBy keyf xs).ToDictionary(fst, snd >> valf)
let orDefault defaultValue = 
    function
    | None -> defaultValue
    | Some(value) -> value

let resultsByDatasetByModel =
    ResultAnalysis.analyzedModels |> toDict (fun modelRes -> modelRes.DatasetBaseShorthand) (toDict (fun modelRes -> (getSettings modelRes).Key) System.Linq.Enumerable.Single )


heuristics |> Seq.map (fun heur -> 
    seq {
        for datasetRes in  resultsByDatasetByModel.Values do
            for modelRes in datasetRes.Values do
                let heurResMaybe = 
                    getSettings modelRes 
                    |> applyHeuristic heur 
                    |> Option.map (fun lvqS -> lvqS.Key)
                    |> Option.bind (Utils.getMaybe datasetRes)

                match heurResMaybe with
                | None -> ()
                | Some(heurRes) ->
                    let errs (model:ResultAnalysis.ModelResults) = model.Results |> Seq.map (fun res->res.CanonicalError) |> Seq.toList 
                    let hErr = errs heurRes
                    let bErr = errs modelRes
                    yield Utils.twoTailedPairedTtest hErr bErr
    }
    |> toDict fst (Seq.map snd >> Seq.sort >> Seq.toArray)
    |> (fun dict -> (Utils.getMaybe dict true |> orDefault (Array.empty),  Utils.getMaybe dict false |> orDefault (Array.empty)) )
    |> (fun (better, worse) ->
        (heur.Name, better.Length + worse.Length, float better.Length / float (better.Length + worse.Length), better, worse)
    )
) |> Seq.toList
