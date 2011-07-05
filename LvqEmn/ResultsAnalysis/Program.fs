open System.IO;
open System;
open LvqGui;
open LvqLibCli;

//---------------------------------------------------CONSTANTS

let filepattern = "*.txt"
let curDatasetName = "base"
let alltypes:list<LvqModelSettingsCli> =
    [ LvqModelSettingsCli(ModelType = LvqModelType.Lgm, PrototypesPerClass = 1) ] 

//---------------------------------------------------BASICS

let rec flipList lstOfLists = 
    let lstOfNonemptyLists = List.filter (List.isEmpty >> not) lstOfLists
    if List.isEmpty lstOfNonemptyLists then []
    else List.map List.head lstOfNonemptyLists :: (List.map List.tail lstOfNonemptyLists |> flipList)
    
let apply2nd f (v1,v2) = (v1,f v2)
let groupList keyF valF = 
    List.toSeq >> Seq.groupBy keyF >> Seq.map (apply2nd (Seq.map valF >> Seq.toList)) >> Seq.toList
    
let countMeanVar xs = 
    let count = float(List.length xs)
    let mean = List.sum xs / count
    let var = List.sumBy (fun x -> (x-mean)**2.0) xs / (count - 1.0)
    (count, mean, var)

let meanStderr xs = 
    let (count, mean, var) = countMeanVar xs
    (mean, Math.Sqrt( var / count))

let latexstderr (mean,stderr) = EmnExtensions.MathHelpers.Statistics.GetFormatted(mean,stderr).Replace("~",@"\pm")

let sampleCorrelation listA listB =
    let (countA, meanA, varA) = countMeanVar listA
    let (countB, meanB, varB) = countMeanVar listB
    assert (countA = countB)
    (List.zip listA listB
        |> List.sumBy (fun (a,b) -> (a - meanA) * (b - meanB))) / (float countA - 1.0) / Math.Sqrt(varA * varB)

//---------------------------------------------------PARSING

let loadResultsByLr datasetName (settings:LvqLibCli.LvqModelSettingsCli) = 
    TestLr.resultsDir.GetDirectories(datasetName).[0].GetFiles(filepattern)
    |> Seq.map LvqGui.DatasetResults.ProcFile
    |> Seq.filter (fun result -> DatasetResults.WithoutLrOrSeeds(result.unoptimizedSettings).ToShorthand() =  settings.ToShorthand())
    |> Seq.collect (fun res -> res.GetLrs() |> Seq.map (fun lr -> (lr.Lr, lr.Errors, res)))
    |> Seq.toList
    |> groupList (fun (lr, err, result) -> lr) (fun (lr, err, result) -> [err.training; err.test; err.nn] |> List.filter (Double.IsNaN >> not) )


//------------------------------------------------------------------------Best lr's

let bestResults cutoffscale useall datasetName settings = 
    let toCutoff (mean, stderr) = mean + cutoffscale*stderr
    let toCutoffMin (mean, stderr) = mean - cutoffscale*stderr
    let results = 
        loadResultsByLr datasetName settings //list of LRs, each has a list of results in file order
        |> List.map (apply2nd (flipList >> List.map meanStderr)) //list of LRs, each mean+stderrs for error rates
        |> List.sortBy (snd >> List.map fst >> List.sum)

    let cutoff = results |> List.head |> snd |> List.map toCutoff
    let filterF = if useall then List.forall2 else List.exists2
    results |> List.filter (snd >> List.map toCutoffMin >> filterF (fun cutoffval currval -> cutoffval > currval) cutoff)

let certainlyConfusable = bestResults 1.00 true 
let possiblyConfusable =  bestResults 1.96 true
let certainlyOneConfusable =  bestResults 1.00 false

let latexifyConfusable (datasetName:string) settings =
    let latexifyConfusableList = 
        let prErr(mean,stderr) = latexstderr (mean*100., stderr*100.)
        let prTuple (lr:DatasetResults.Lr, errs) = sprintf  "$ %f $ & $ %f $ & $ %f $" lr.Lr0 lr.LrP lr.LrB + " & $" + String.Join("$ & $", List.map prErr errs) + "$"
        List.map prTuple >> (fun list->String.Join("  \\\\\n",list))
    let confusables = certainlyConfusable datasetName settings
    let hasLrB =  (confusables |> List.head |> fst).LrB <> 0.0
    let hasNnError=  confusables |> List.head |> snd |> List.length = 3
    @"\noindent " + datasetName + ":\\\\\n" + 
        @"\begin{tabular}{llll"
        + (if hasLrB then "l" else "")
        + (if hasNnError then "l" else "")
        + "}\n" + @"$\lr_0$ & $\lr_P$ "
        + if hasLrB then @"& $\lr_B$ " else ""
        + @"& training error & test error "
        + if hasNnError then @"& NN error" else ""
        + " \\\\\n\\hline"
        + latexifyConfusableList confusables
        + "\n\\end{tabular}"

String.Join("\n\n", alltypes |> List.map (latexifyConfusable curDatasetName)) |> printfn "%s"  //print results

//-------------------------------------------------------------------------------err-type correlations

let tails xs = 
    let step x xss = (x::List.head xss)::xss
    List.foldBack step xs [[]]

let correlateUsing comb =
    tails >> List.collect (function
                                    | x::xsrest -> (List.map (comb x) xsrest)
                                    | [] -> []  
                                )

let corrs = correlateUsing sampleCorrelation

let corrNames = correlateUsing (fun (a:LvqModelSettingsCli) b -> a.ToShorthand() + "-" + b.ToShorthand())


let errTypeCorrelation datasetName settings =
    loadResultsByLr datasetName settings //list of LRs, each has a list of results in file order, each has a few error rate types
//    |> List.sortBy (snd >> flipList >> List.map meanStderr >> List.map fst >> List.sum) //list of lr's is sorted by quality
//    |> (fun list -> Seq.take 5 list |> Seq.toList) //only take accurate LR's
    |> List.map 
        (snd //discard LR data: each has a list of results in file order, each has a few error rate types
        >> flipList //each has a few error rate types, each has a list of results in file order
        >> corrs) //each has a few error type correlations
    |> flipList //List of error types, each has the list of correlation for all LRs
    |> List.map meanStderr
    |> (fun list -> List.zip (if List.length list > 1 then corrNames ["train";"test";"NN"] else corrNames ["train";"test"]) list)

let errTypeCorrelationLatex datasetName settings =
    let corrInfo = errTypeCorrelation datasetName settings
    settings.ToShorthand() + " & " + String.Join(" & ", List.map (fun (errname, x) -> (*errname + ": " +*) (latexstderr >> (fun (corr) -> "$" + corr + "$")) x) corrInfo)

errTypeCorrelation "base" (LvqModelSettingsCli(ModelType = LvqModelType.G2m)) |> ignore

String.Join("\\\\\n", alltypes |> List.map (errTypeCorrelationLatex curDatasetName)) |> printfn "%s"


//-------------------------------------------------------------------------------initialization correlations
let initCorrs = 
    let relevantTypes = 
        alltypes |> List.filter (fun modeltype -> not (modeltype.ModelType = LvqModelType.Lgm))
    relevantTypes
        |> List.map (loadResultsByLr curDatasetName >> List.map (snd >> List.map List.head)) 
                //list of model types, each has a list of LRs, each has a list of results in file order
        |> flipList //list of lrs, each has a  list of model types, each has a list of results in file order
        |> List.map corrs //, list of lrs, each has a list of model type correlations
        |> flipList //, list of model type correlations, each has a list of corrs for all LRs
        |> List.map meanStderr //, list of model type correlation mean/stderrs
        |> List.zip (corrNames relevantTypes)

initCorrs |> List.averageBy (snd >> fst)


alltypes |> List.filter (fun modeltype -> not (modeltype.Contains("lgm")))
    |> List.map (loadResultsByLrThenFileThenErrtype >> List.map (snd >> List.map List.head)) 
            //list of model types, each has a list of LRs, each has a list of results in file order
    |> List.concat
    |> corrs //, list of lrs, each has a list of model type correlations
    |> meanStderr //, list of model type correlation mean/stderrs
