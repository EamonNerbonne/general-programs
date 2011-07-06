#if INTERACTIVE
#I @"..\..\EmnExtensions\bin\Release"
#I @"..\LvqGui\bin\Release"
#r "EmnExtensions"
#r "LvqGui"
#r "LvqLibCli"
#endif

open System.IO;
open System;
open LvqGui;
open LvqLibCli;
open EmnExtensions;

//---------------------------------------------------CONSTANTS

let filepattern = "*.txt"

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

let latexstderr (mean,stderr) = "$"+EmnExtensions.MathHelpers.Statistics.GetFormatted(mean,stderr).Replace("~",@"\pm")+"$"

let sampleCorrelation listA listB =
    let (countA, meanA, varA) = countMeanVar listA
    let (countB, meanB, varB) = countMeanVar listB
    assert (countA = countB)
    (List.zip listA listB
        |> List.sumBy (fun (a,b) -> (a - meanA) * (b - meanB))) / (float countA - 1.0) / Math.Sqrt(varA * varB)

//---------------------------------------------------PARSING

let coreSettingsEq a b = DatasetResults.WithoutLrOrSeeds(a).ToShorthand() =  DatasetResults.WithoutLrOrSeeds(b).ToShorthand()

let loadResultsByLr datasetName (settings:LvqLibCli.LvqModelSettingsCli) = 
    TestLr.resultsDir.GetDirectories(datasetName).[0].GetFiles(filepattern)
    |> Seq.map LvqGui.DatasetResults.ProcFile
    |> Seq.filter (fun result -> result.unoptimizedSettings.InstanceSeed < 20u )
    |> Seq.filter (fun result -> coreSettingsEq settings result.unoptimizedSettings)
    |> Seq.collect (fun res -> res.GetLrs() |> Seq.map (fun lr -> (lr.Lr, lr.Errors, res)))
    |> Seq.toList
    |> groupList (fun (lr, err, result) -> lr) (fun (lr, err, result) -> err )


let unpackErrs errs = 
    (List.map (fun (err:TestLr.ErrorRates) -> err.training) errs,
        List.map (fun (err:TestLr.ErrorRates) -> err.test) errs,
        List.map (fun (err:TestLr.ErrorRates) -> err.nn) errs)

let unpackToListErrs errs = 
    [List.map (fun (err:TestLr.ErrorRates) -> err.training) errs;
        List.map (fun (err:TestLr.ErrorRates) -> err.test) errs;
        List.map (fun (err:TestLr.ErrorRates) -> err.nn) errs]


let meanStderrOfErrs errs =
    let (trns, tsts, nns) = unpackErrs errs
    let (trnM, trnE) = meanStderr trns
    let (tstM, tstE) = meanStderr tsts
    let (nnM, nnE) = meanStderr nns
    TestLr.ErrorRates(trnM,trnE,tstM,tstE,nnM,nnE, 0.0)
//------------------------------------------------------------------------Best lr's

let bestResults cutoffscale useall (results:list<TestLr.ErrorRates>) = 
    let best = results |> List.head
    let cutoff = best.training + cutoffscale*best.trainingStderr
    results |> List.filter (fun err-> err.training - err.trainingStderr*cutoffscale < cutoff)

//    let cutoff = results |> List.head |> snd |> List.map toCutoff
//    let filterF = if useall then List.forall2 else List.exists2
//    results |> List.filter (snd >> List.map toCutoffMin >> filterF (fun cutoffval currval -> cutoffval > currval) cutoff)

let certainlyConfusable x = bestResults 1.00 true x
let possiblyConfusable x =  bestResults 1.96 true x
let certainlyOneConfusable x =  bestResults 1.00 false x

let latexifyConfusable (datasetName:string) settingsList =
    let latexifyConfusableRow (settings, label:string) = 
        let rawResults = loadResultsByLr datasetName settings //list of LRs, each has a list of results in file order
        let results = 
            rawResults
            |> List.map (apply2nd meanStderrOfErrs) //list of LRs, each mean+stderrs for error rates
            |> List.sortBy (snd >>  (fun err-> err.training))
//        printfn "%d" (rawResults |> List.head |> snd |> List.length)
        let confusableCount = results |> List.map snd |> certainlyConfusable  |> List.length
        let confusableRatio = float confusableCount / float(List.length results)
        let prErr(mean, stderr) = latexstderr (mean*100., stderr*100.)
        let (bestlr, besterr) = results |> List.head
        
        (besterr.training, 
            label + " & " + prErr (besterr.training , besterr.trainingStderr ) +
            " & " + prErr (besterr.test , besterr.testStderr ) +
            " & " + (if F.IsFinite besterr.nn then prErr (besterr.nn, besterr.nnStderr ) else "") +
            sprintf " & $%2.0f" (100. - 100. * confusableRatio) + "\\%$ " + 
            " & " + sprintf " $ %1.3f $ & $ %1.3f $" bestlr.Lr0 bestlr.LrP +
            (if bestlr.LrB > 0.0 then sprintf  " & $ %1.3f $" bestlr.LrB else "&"))
            
    @"\noindent " + datasetName  + ":\\\\\n" + 
        @"\begin{tabular}{llllllll}"+"\n"
        + @"model & training & test & NN & better than & $\lr_0$ & $\lr_P$ & $\lr_B$  \\" + "\n"
        + @"        & error rate & error rate & error rate &  & &  & \\\hline" + "\n"
        + String.Join("\\\\\n", settingsList |>List.map latexifyConfusableRow |> List.sort |> List.map snd)
        + "\n" + @"\end{tabular}"


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

let corrNames = correlateUsing (fun (a:string) b -> a + "-" + b)

let corrSettings = List.map (fun (setting:LvqModelSettingsCli) -> setting.ToShorthand()) >> corrNames

let errTypeCorrelation datasetName settings =
    loadResultsByLr datasetName settings //list of LRs, each has a list of results in file order, each has a few error rate types
//    |> List.sortBy (snd >> flipList >> List.map meanStderr >> List.map fst >> List.sum) //list of lr's is sorted by quality
//    |> (fun list -> Seq.take 5 list |> Seq.toList) //only take accurate LR's
    |> List.map 
        (snd //discard LR data: each has a list of results in file order, each has a few error rate types
        >> unpackToListErrs //each has a few error rate types, each has a list of results in file order
        >> corrs) //each has a few error type correlations
    |> flipList //List of error types, each has the list of correlation for all LRs
    |> List.map meanStderr
    |> (fun list -> List.zip (corrNames ["train";"test";"NN"]) list)

let errTypeCorrelationLatex datasetName settings =
    let corrInfo = errTypeCorrelation datasetName settings
    settings.ToShorthand() + " & " + String.Join(" & ", List.map (fun (errname, x) -> if Double.IsNaN(fst x) then "" else latexstderr x) corrInfo)




//-------------------------------------------------------------------------------initialization correlations
let initCorrs datasetName (settingslist:list< LvqModelSettingsCli * string >) = 
    let relevantTypes = 
        settingslist |> List.map fst |> List.filter (fun modeltype -> not (modeltype.ModelType = LvqModelType.Lgm))
    relevantTypes
        |> List.map (loadResultsByLr datasetName >> List.map (snd >> List.map (fun err->err.training))) 
                //list of model types, each has a list of LRs, each has a list of results in file order
        |> flipList //list of lrs, each has a  list of model types, each has a list of results in file order
        |> List.map corrs //, list of lrs, each has a list of model type correlations
        |> flipList //, list of model type correlations, each has a list of corrs for all LRs
        |> List.map meanStderr //, list of model type correlation mean/stderrs
        |> List.zip (corrSettings relevantTypes)





//-------------------------------------------------------------------------------------------------main code:

let curDatasetName = "base"
let alltypes:list< LvqModelSettingsCli * string > =
    [
        (LvqModelSettingsCli(ModelType = LvqModelType.Lgm, PrototypesPerClass = 1), "lgm 1ppc");
        (LvqModelSettingsCli(ModelType = LvqModelType.Lgm, PrototypesPerClass = 5), "lgm 5ppc");
        (LvqModelSettingsCli(ModelType = LvqModelType.Gm, PrototypesPerClass = 1), "gm 1ppc");
        (LvqModelSettingsCli(ModelType = LvqModelType.Gm, PrototypesPerClass = 5), "gm 5ppc");
        (LvqModelSettingsCli(ModelType = LvqModelType.G2m, PrototypesPerClass = 1), "g2m 1ppc");
        (LvqModelSettingsCli(ModelType = LvqModelType.G2m, PrototypesPerClass = 5), "g2m 5ppc");
        (LvqModelSettingsCli(ModelType = LvqModelType.Ggm, PrototypesPerClass = 1), "ggm 1ppc");
        (LvqModelSettingsCli(ModelType = LvqModelType.Ggm, PrototypesPerClass = 5), "ggm 5ppc");
    ] 


latexifyConfusable curDatasetName alltypes |> printfn "%s"  //print results

String.Join("\\\\\n", alltypes|>List.map fst |> List.map (errTypeCorrelationLatex curDatasetName)) |> printfn "%s"


initCorrs curDatasetName alltypes|>printfn "%A"

initCorrs curDatasetName alltypes |> List.averageBy (snd >> fst) |> printfn "%A"


alltypes  |> List.map fst |> List.filter (fun settings -> settings.ModelType <> LvqModelType.Lgm)
    |> List.map (loadResultsByLr curDatasetName >> List.map (snd >> List.map (fun err->err.training))) 
            //list of model types, each has a list of LRs, each has a list of results in file order
    |> List.concat  //list of model types+lrs, each has a list of results in file order
    |> corrs //a list of correlations between differing types/lrs over changing initialization
    |> meanStderr //mean correlation between differing types/lrs over changing initialization
    |> printfn "%A"
