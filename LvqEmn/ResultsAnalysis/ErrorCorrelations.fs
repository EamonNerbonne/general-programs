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

let errTypeCorrelation results onlyBestResults =
    LrOptResults.groupResultsByLr results //list of LRs, each has a list of results in file order, each has a few error rate types
    |> if onlyBestResults then    
            List.sortBy (snd >> LrOptResults.meanStderrOfErrs >> (fun errs->errs.CanonicalError)) //list of lr's is sorted by quality
            >> (fun list -> Seq.take 10 list |> Seq.toList) //only take accurate LR's
        else
            id
    |> List.map 
        (snd //discard LR data: each has a list of results in file order, each has a few error rate types
        >> LrOptResults.unpackToListErrs //each has a few error rate types, each has a list of results in file order
        >> corrs) //each has a few error type correlations
    |> Utils.flipList //List of error types, each has the list of correlation for all LRs
    |> List.map Utils.sampleDistribution
    |> (fun list -> List.zip (corrNames ["train";"test";"NN"]) list)


let errTypeCorrelationLatex title results onlyBestResults =
    let corrInfo = errTypeCorrelation results onlyBestResults
    title + " & " + String.Join(" & ", List.map (fun (errname, x:Utils.SampleDistribution) -> if Double.IsNaN(x.Mean) then "" else Utils.latexstderr x) corrInfo)

let errTypeCorrTableLatex results onlyBestResults settingsList = 
    @"\begin{tabular}{llll} model type & training $\leftrightarrow$ test & training $\leftrightarrow$ NN &  test $\leftrightarrow$ NN  \\\hline" + "\n" + 
    (settingsList 
    |> List.map (fun (settings:LvqModelSettingsCli) -> errTypeCorrelationLatex (settings.ToShorthand()) (LrOptResults.lrOptResultsForSettings results settings) onlyBestResults) 
    |> String.concat "\\\\\n") + "\n"
    + @"\end{tabular}" + "\n"
    

//-------------------------------------------------------------------------------initialization correlations
let initCorrs results (settingslist:list< LvqModelSettingsCli>) = 
    settingslist
        |> List.map (LrOptResults.lrOptResultsForSettings results)  //list settings, each has a list of results
        |> List.map  (LrOptResults.groupResultsByLr >> List.map (snd >> List.map (fun err->err.training))) 
                //list of settings, each has a list of LRs, each has a list of training error in file order
        |> Utils.flipList //list of lrs, each has a list of settings, each has a list of results in file order
        |> List.map corrs //, list of lrs, each has a list of model type correlations
        |> Utils.flipList //, list of model type correlations, each has a list of corrs for all LRs
        |> List.map Utils.sampleDistribution //, list of model type correlation mean/stderrs
        |> List.zip (corrSettings settingslist)

let meanInitCorrs results  (settingslist:list< LvqModelSettingsCli>) = 
    settingslist
        |> List.filter (fun settings -> settings.ModelType <> LvqModelType.Lgm)
        |> List.map (LrOptResults.lrOptResultsForSettings results >> LrOptResults.groupResultsByLr >> List.map (snd >> List.map (fun err->err.training))) 
                //list of model types, each has a list of LRs, each has a list of results in file order
        |> List.concat  //list of model types+lrs, each has a list of results in file order
        |> corrs //a list of correlations between differing types/lrs over changing initialization
        |> Utils.sampleDistribution //mean correlation between differing types/lrs over changing initialization
