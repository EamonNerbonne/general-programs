module ErrorCorrelations

open LvqLibCli
open System


//-------------------------------------------------------------------------------err-type correlations

let tails xs = 
    let step y yss = (y::List.head yss)::yss
    List.foldBack step xs [[]]

let correlateUsing comb =
    tails >> List.collect (function
                                    | x::xsrest -> (List.map (comb x) xsrest)
                                    | [] -> []  
                                )

let corrs = correlateUsing Utils.sampleCorrelation

let corrNames = correlateUsing (fun (a:string) b -> a + "-" + b)

let corrSettings = List.map (fun (setting:LvqModelSettingsCli) -> setting.ToShorthand()) >> corrNames

let errTypeCorrelation results  =
    ResultParsing.groupResultsByLr results //list of LRs, each has a list of results in file order, each has a few error rate types
//    |> List.sortBy (snd >> flipList >> List.map meanStderr >> List.map fst >> List.sum) //list of lr's is sorted by quality
//    |> (fun list -> Seq.take 5 list |> Seq.toList) //only take accurate LR's
    |> List.map 
        (snd //discard LR data: each has a list of results in file order, each has a few error rate types
        >> ResultParsing.unpackToListErrs //each has a few error rate types, each has a list of results in file order
        >> corrs) //each has a few error type correlations
    |> Utils.flipList //List of error types, each has the list of correlation for all LRs
    |> List.map Utils.sampleDistribution
    |> (fun list -> List.zip (corrNames ["train";"test";"NN"]) list)


let errTypeCorrelationLatex results (settings:LvqModelSettingsCli) =
    let corrInfo = errTypeCorrelation results
    settings.ToShorthand() + " & " + String.Join(" & ", List.map (fun (errname, x:Utils.SampleDistribution) -> if Double.IsNaN(x.Mean) then "" else Utils.latexstderr x) corrInfo)


    

//-------------------------------------------------------------------------------initialization correlations
let initCorrs results (settingslist:list< LvqModelSettingsCli>) = 
    settingslist
        |> List.map (ResultParsing.filterResults >> Utils.pass results >> ResultParsing.groupResultsByLr >> List.map (snd >> List.map (fun err->err.training))) 
                //list of model types, each has a list of LRs, each has a list of results in file order
        |> Utils.flipList //list of lrs, each has a  list of model types, each has a list of results in file order
        |> List.map corrs //, list of lrs, each has a list of model type correlations
        |> Utils.flipList //, list of model type correlations, each has a list of corrs for all LRs
        |> List.map Utils.sampleDistribution //, list of model type correlation mean/stderrs
        |> List.zip (corrSettings settingslist)

