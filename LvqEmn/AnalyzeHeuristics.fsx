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

let resultsByDatasetByModel =
    ResultAnalysis.analyzedModels |> toDict (fun modelRes -> modelRes.DatasetBaseShorthand) (toDict (fun modelRes -> (getSettings modelRes).Key) id)


