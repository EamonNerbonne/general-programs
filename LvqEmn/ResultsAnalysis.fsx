#if INTERACTIVE
#I @"..\EmnExtensions\bin\Release"
#I @"..\Support Tools\alglib"
#I @"LvqGui\bin\Release"
#r "EmnExtensions"
#r "LvqGui"
#r "LvqLibCli"
#r "alglibnet2"
#endif


open System.IO;
open System;
open LvqGui;
open LvqLibCli;
open EmnExtensions;


//---------------------------------------------------BASICS
let pass x f = f x 

let rec flipList lstOfLists = 
    let lstOfNonemptyLists = List.filter (List.isEmpty >> not) lstOfLists
    if List.isEmpty lstOfNonemptyLists then []
    else List.map List.head lstOfNonemptyLists :: (List.map List.tail lstOfNonemptyLists |> flipList)
    
let apply2nd f (v1,v2) = (v1,f v2)
let apply1st f (v1,v2) = (f v1, v2)
let groupList keyF valF = 
    List.toSeq >> Seq.groupBy keyF >> Seq.map (apply2nd (Seq.map valF >> Seq.toList)) >> Seq.toList
    
let countMeanVar xs = 
    let count = List.length xs
    let mean = List.sum xs / float count
    let var = List.sumBy (fun x -> (x-mean)**2.0) xs / (float count - 1.0)
    (count, mean, var)

let meanStderr xs = 
    let (count, mean, var) = countMeanVar xs
    (mean, Math.Sqrt( var / float count))

let latexstderr (mean,stderr) =  "$ "+EmnExtensions.MathHelpers.Statistics.GetFormatted(mean,stderr,-0.4).Replace("~",@"\pm")+"$"

let sampleCorrelation listA listB =
    let (countA, meanA, varA) = countMeanVar listA
    let (countB, meanB, varB) = countMeanVar listB
    assert (countA = countB)
    (List.zip listA listB
        |> List.sumBy (fun (a,b) -> (a - meanA) * (b - meanB))) / (float countA - 1.0) / Math.Sqrt(varA * varB)

let twoTailedPairedTtest xs ys = 
    let (count, mean, var) = List.zip xs ys |>List.map (fun (x,y) -> x - y) |> countMeanVar
    let t = mean / Math.Sqrt(var / float count)
    let p = alglib.studenttdistr.studenttdistribution(count - 1, -Math.Abs(t)) * 2.0
    (p, mean < 0.0)
        
//---------------------------------------------------PARSING


let loadAllResults datasetName =
    let filepattern = "*.txt"
    TestLr.resultsDir.GetDirectories(datasetName).[0].GetFiles(filepattern)
    |> Seq.map LvqGui.DatasetResults.ProcFile
    |> Seq.toList

let curDatasetName = "base"
let allResults = loadAllResults curDatasetName

let groupResultsByLr (results:list<DatasetResults>) = 
    results
    |> List.collect (fun res -> res.GetLrs() |> Seq.toList |> List.map (fun lr -> (lr.Lr, lr.Errors, res)))
    |> groupList (fun (lr, err, result) -> lr) (fun (lr, err, result) -> err )

let coreSettingsEq a b = DatasetResults.WithoutLrOrSeeds(a).ToShorthand() =  DatasetResults.WithoutLrOrSeeds(b).ToShorthand()
let filterResults exampleSettings results = 
    results |> List.filter (fun (result:DatasetResults) -> coreSettingsEq exampleSettings result.unoptimizedSettings && result.unoptimizedSettings.InstanceSeed < 20u)

//let loadResultsByLr datasetName (settings:LvqLibCli.LvqModelSettingsCli) = 
//    loadAllResults datasetName |> filterResults settings  |> groupResultsByLr

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

let latexifyConfusable (datasetName:string)  (results:list<DatasetResults>) settingsList =
    let latexifyConfusableRow (settings, label:string) = 
        let rawResults = results |> filterResults settings |> groupResultsByLr //list of LRs, each has a list of results in file order
        let results = 
            rawResults
            |> List.map (apply2nd meanStderrOfErrs) //list of LRs, each mean+stderrs for error rates
            |> List.sortBy (snd >>  (fun err-> err.ErrorMean))
//        printfn "%d" (rawResults |> List.head |> snd |> List.length)
        let confusableCount = results |> List.map snd |> certainlyConfusable  |> List.length
        let confusableRatio = float confusableCount / float(List.length results)
        let prErr(mean, stderr) =  sprintf @"$%.1f \pm %.2f $" (mean*100.) (stderr*100.)
        let (bestlr, besterr) = List.head results 
        let (lr75th, err75th ) = List.length results / 4 |> List.nth results
        
        (besterr.training, 
            label
            + " & " + prErr (besterr.training , besterr.trainingStderr )
            + " & " + prErr (err75th.training , err75th.trainingStderr )
            + " & " + prErr (besterr.test , besterr.testStderr )
            + " & " + prErr (err75th.test , err75th.testStderr )
            + " & " + (if F.IsFinite besterr.nn then prErr (besterr.nn, besterr.nnStderr ) else "")
            + " & " + (if F.IsFinite err75th.nn then prErr (err75th.nn, err75th.nnStderr ) else "")
            //+ sprintf " & $%2.0f" (100. - 100. * confusableRatio) + "\\%$ "
            // + " & " + sprintf " $ %1.3f $ & $ %1.3f $" bestlr.Lr0 bestlr.LrP + (if bestlr.LrB > 0.0 then sprintf  " & $ %1.3f $" bestlr.LrB else "&")
        )
            
    @"\noindent " + datasetName  + ":\\\\\n" + 
        @"\begin{tabular}{@{}lllllll@{}}"+"\n"
        + @"model &\multicolumn{2}{c}{training error rate} & \multicolumn{2}{c}{test error rate} &\multicolumn{2}{c}{NN error rate}  \\" + "\n" //& better than & $\lr_0$ & $\lr_P$ & $\lr_B$
        + @"        & best &\footnotesize upper quartile & best &\footnotesize upper quartile & best &\footnotesize upper quartile \\\hline" + "\n" //&  & &  &
        + String.Join("\\\\\n", settingsList |> List.map latexifyConfusableRow (*|> List.sort*) |> List.map snd)
        + "\n" + @"\end{tabular}"



let latexifyCompareMethods (datasetName:string)  (allresults:list<DatasetResults>) settingsList =
    let latexifyConfusableRow (settings:LvqModelSettingsCli, label:string) = 
        let basicSettings = (new LvqModelSettingsCli()).WithChanges(settings.ModelType, settings.PrototypesPerClass, settings.ParamsSeed, settings.InstanceSeed)
        let getResults s = 
            allresults |> filterResults s |> groupResultsByLr //list of LRs, each has a list of results in file order
            |> List.map snd //list of LRs without their values, each  has a list of results in file order
            |> List.map (fun errs -> (meanStderrOfErrs errs, errs))//list of LRs, each mean+stderrs for error rates
            |> List.sortBy (fst>> (fun err-> err.ErrorMean))
        let results = getResults settings
        let basicResults = getResults basicSettings
        let (besterr, besterrs) = List.head results 
        let (err75th, err75ths) = List.length results / 4 |> List.nth results
        let (besterrB ,besterrBs)= List.head basicResults 
        let (err75thB ,err75thBs)= List.length basicResults / 4 |> List.nth basicResults

        let significantDiff errsA errsB errSelector =
            twoTailedPairedTtest (errsA |>List.map errSelector) (errsB |> List.map errSelector)
        let prErr (mean, stderr) (errsA, errsB) errSelector =
            let (p, isBetter) = significantDiff errsA errsB errSelector
            let color = 
                if isBetter && p < 0.01 then
                    "0.4,0.8,1.0"   
                else if isBetter && p < 0.05 then
                    "0.8,0.9,1.0" 
                else if not isBetter && p <0.01 then
                    "1.0,0.55,0.4"
                else if not isBetter && p <0.05 then
                    "1.0,0.85,0.8"
                else
                    "1.0,1.0,1.0"
            sprintf @"\cellcolor[rgb]{%s} $%.1f \pm %.2f $" color (mean*100.) (stderr*100.)
        
        let trainingError (err:TestLr.ErrorRates) = err.training
        let testError (err:TestLr.ErrorRates) = err.test
        let nnError (err:TestLr.ErrorRates) = err.nn

        (besterr.training, 
            label
            + " & " + prErr (besterr.training , besterr.trainingStderr) (besterrs , besterrBs) trainingError
            + " & " + prErr (err75th.training , err75th.trainingStderr) (err75ths, err75thBs) trainingError
            + " & " + prErr (besterr.test , besterr.testStderr) (besterrs , besterrBs) testError
            + " & " + prErr (err75th.test , err75th.testStderr) (err75ths , err75thBs) testError
            + " & " + (if F.IsFinite besterr.nn then prErr (besterr.nn, besterr.nnStderr) (besterrs, besterrBs) nnError else "")
            + " & " + (if F.IsFinite err75th.nn then prErr (err75th.nn, err75th.nnStderr) (err75ths, err75thBs) nnError else "")
            //+ sprintf " & $%2.0f" (100. - 100. * confusableRatio) + "\\%$ "
            // + " & " + sprintf " $ %1.3f $ & $ %1.3f $" bestlr.Lr0 bestlr.LrP + (if bestlr.LrB > 0.0 then sprintf  " & $ %1.3f $" bestlr.LrB else "&")
        )
            
    @"\noindent " + datasetName  + ":\\\\\n" + 
        @"\begin{tabular}{@{}lllllll@{}}"+"\n"
        + @"model &\multicolumn{2}{c}{training error rate} & \multicolumn{2}{c}{test error rate} &\multicolumn{2}{c}{NN error rate}  \\" + "\n" //& better than & $\lr_0$ & $\lr_P$ & $\lr_B$
        + @"        & best &\footnotesize upper quartile & best &\footnotesize upper quartile & best &\footnotesize upper quartile \\\hline" + "\n" //&  & &  &
        + String.Join("\\\\\n", settingsList |> List.map latexifyConfusableRow (*|> List.sort*) |> List.map snd)
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

let errTypeCorrelation results  =
    groupResultsByLr results //list of LRs, each has a list of results in file order, each has a few error rate types
//    |> List.sortBy (snd >> flipList >> List.map meanStderr >> List.map fst >> List.sum) //list of lr's is sorted by quality
//    |> (fun list -> Seq.take 5 list |> Seq.toList) //only take accurate LR's
    |> List.map 
        (snd //discard LR data: each has a list of results in file order, each has a few error rate types
        >> unpackToListErrs //each has a few error rate types, each has a list of results in file order
        >> corrs) //each has a few error type correlations
    |> flipList //List of error types, each has the list of correlation for all LRs
    |> List.map meanStderr
    |> (fun list -> List.zip (corrNames ["train";"test";"NN"]) list)

let errTypeCorrelationLatex results (settings:LvqModelSettingsCli) =
    let corrInfo = errTypeCorrelation results
    settings.ToShorthand() + " & " + String.Join(" & ", List.map (fun (errname, x) -> if Double.IsNaN(fst x) then "" else latexstderr x) corrInfo)


    

//-------------------------------------------------------------------------------initialization correlations
let initCorrs results (settingslist:list< LvqModelSettingsCli * string >) = 
    let relevantTypes = 
        settingslist |> List.map fst |> List.filter (fun modeltype -> not (modeltype.ModelType = LvqModelType.Lgm))
    relevantTypes
        |> List.map (filterResults >> pass results >> groupResultsByLr >> List.map (snd >> List.map (fun err->err.training))) 
                //list of model types, each has a list of LRs, each has a list of results in file order
        |> flipList //list of lrs, each has a  list of model types, each has a list of results in file order
        |> List.map corrs //, list of lrs, each has a list of model type correlations
        |> flipList //, list of model type correlations, each has a list of corrs for all LRs
        |> List.map meanStderr //, list of model type correlation mean/stderrs
        |> List.zip (corrSettings relevantTypes)



//-------------------------------------------------------------------------------------------------main code:


let alternates:list< (LvqModelSettingsCli -> LvqModelSettingsCli) * string > =
    let setProtosAndType lvqType protos (settings:LvqModelSettingsCli) = settings.WithChanges(lvqType, protos, settings.ParamsSeed, settings.InstanceSeed)
    [
//        (setProtosAndType LvqModelType.Lgm 1 , "lgm 1ppc");
//        (setProtosAndType LvqModelType.Lgm 5 , "lgm 5ppc");
        (setProtosAndType LvqModelType.Gm 1 , "gm 1ppc");
        (setProtosAndType LvqModelType.G2m 1 , "g2m 1ppc");
        (setProtosAndType LvqModelType.Ggm 1 , "ggm 1ppc");
        (setProtosAndType LvqModelType.Gm 5 , "gm 5ppc");
        (setProtosAndType LvqModelType.G2m 5 , "g2m 5ppc");
        (setProtosAndType LvqModelType.Ggm 5 , "ggm 5ppc");
    ] 

let basicSettings = LvqModelSettingsCli()
let heuristics:list< LvqModelSettingsCli * string > =
    [
        (LvqModelSettingsCli(), "core");
        (LvqModelSettingsCli(UpdatePointsWithoutB = true), "NoB");
        (LvqModelSettingsCli(SlowStartLrBad = true ), "SlowStartLrBad");
        (LvqModelSettingsCli(NgUpdateProtos = true), "NgUpdate");
        (LvqModelSettingsCli(NgInitializeProtos = true), "NgInit");
        (LvqModelSettingsCli(NgInitializeProtos = true, ProjOptimalInit = true), "NgInit+Pi");
        (LvqModelSettingsCli(NgInitializeProtos = true, ProjOptimalInit=true, BLocalInit=true), "NgInit+Pi+Bi");
        (LvqModelSettingsCli(NgInitializeProtos = true, SlowStartLrBad = true ), "NgInit+SlowStartLrBad");
    ]



List.map (fun (settings, name) ->
        latexifyCompareMethods name allResults (
            alternates
            |> List.filter (fun (alternate, altName) -> 
                    settings = basicSettings 
                    || (alternate settings).ToShorthand() <> (alternate basicSettings).ToShorthand() 
                    && (not settings.NgInitializeProtos 
                        || (alternate settings).PrototypesPerClass <> 1
                        ) 
                    )
            |> List.map (fun (alternate, altName) -> (alternate settings, altName))
        )
    ) heuristics 
    |> String.concat "\n\n"
    |> printfn "%s"  //print results


(List.map (LvqModelSettingsCli(NgInitializeProtos = true, SlowStartLrBad = true ) |> pass |> apply1st) alternates)
    |> List.map (apply1st filterResults >> apply1st (pass allResults) )



let coversAllResults = 
    let alltypes = heuristics |> List.collect (fun (setting, name) -> alternates |> List.map (fun (alt, altname) -> (alt setting, name + ":" + altname))) |> List.map fst 
    TestLr.resultsDir.GetDirectories(curDatasetName).[0].GetFiles(filepattern)
    |> Seq.toList
    |> List.map LvqGui.DatasetResults.ProcFile
    |> List.map (fun result -> result.unoptimizedSettings)
    |> List.filter (fun settings -> not <| List.exists (coreSettingsEq settings) alltypes )
    |> List.filter (fun settings -> settings.ModelType = LvqModelType.Lgm |> not )
    |> List.map (fun settings -> settings.ToShorthand())
    


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
