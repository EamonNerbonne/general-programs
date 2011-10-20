module LatexifyResults
open LvqGui
open EmnExtensions

let bestResults cutoffscale useall (results:list<TestLr.ErrorRates>) = 
    let best = results |> List.head
    let cutoff = best.training + cutoffscale*best.trainingStderr
    results |> List.filter (fun err-> err.training - err.trainingStderr*cutoffscale < cutoff)

//    let cutoff = results |> List.head |> snd |> List.map toCutoff
//    let filterF = if useall then List.forall2 else List.exists2
//    results |> List.filter (snd >> List.map toCutoffMin >> filterF (fun cutoffval currval -> cutoffval > currval) cutoff)

let certainlyConfusable = bestResults 1.00 true
let possiblyConfusable =  bestResults 1.96 true
let certainlyOneConfusable =  bestResults 1.00 false

let latexifyLrRelevanceConfusable (title:string)  (allResults:list<DatasetResults>) settingsList =
    let trainingError (errs:TestLr.ErrorRates) = (errs.training, errs.trainingStderr)
    let testError (errs:TestLr.ErrorRates) = (errs.test, errs.testStderr)
    let nnError (errs:TestLr.ErrorRates) = (errs.nn, errs.nnStderr)
    let latexifyConfusableRow (settings, label:string) = 
        let resultsByLr = 
            ResultParsing.chooseResults allResults settings 
            |> ResultParsing.groupResultsByLr //list of LRs, each has a list of results in file order
            |> List.map snd //ignore lr
            |> List.map (fun  errs -> List.map (trainingError >> fst) errs)//ResultParsing.meanStderrOfErrs errs |> errTypeSelector))
            |> List.sortBy List.average 
            |> List.toArray
        let resultCount =  Array.length resultsByLr
        let rankedResults =
            [|0; resultCount / 4; resultCount * 2 / 4; resultCount * 3 / 4 |] 
            |> Array.map (Array.get resultsByLr)
        
        let bestTrainErrs = rankedResults.[0]    

        let confusableCount =
            resultsByLr |> Seq.skip 1
            |> Seq.map (fun trainErrs -> Utils.twoTailedPairedTtest bestTrainErrs trainErrs)
            |> Seq.filter (fun (firstIsBetter, p) -> not firstIsBetter || p > 0.05)
            |> Seq.length
        let confusableRatio = float confusableCount / float(resultCount - 1)
        let prErr errs = 
            let distrib = Utils.sampleDistribution errs
            sprintf @"$%.1f \pm %.2f $" (distrib.Mean*100.) (distrib.StdErr*100.)

        label
        + String.concat "" (Seq.map (fun (trainErrs) -> " & " + prErr trainErrs) rankedResults)
        + sprintf " & $%.1f" (100. - 100. * confusableRatio) + "\\%$ "
        // + " & " + sprintf " $ %1.3f $ & $ %1.3f $" bestlr.Lr0 bestlr.LrP + (if bestlr.LrB > 0.0 then sprintf  " & $ %1.3f $" bestlr.LrB else "&")
        
            
    @"\noindent " + title  + " (training error):\\\\\n" + 
        @"\begin{tabular}{@{}lrrrrl@{}}\toprule"+"\n"
        + @"  \multicolumn{1}{c}{Model Type} & \multicolumn{1}{c}{best} &\multicolumn{1}{c}{\footnotesize upper quartile} & \multicolumn{1}{c}{median} &\multicolumn{1}{c}{\footnotesize lower quartile} & \multicolumn{1}{c}{lr-relevance} \\\midrule" + "\n" //&  & &  &
        + String.concat "\\\\\n" (settingsList |> List.map latexifyConfusableRow)
        + "\n" + @"\bottomrule\end{tabular}"

let latexifyConfusable (title:string)  (allResults:list<DatasetResults>) settingsList =
    let trainingError (errs:TestLr.ErrorRates) = (errs.training, errs.trainingStderr)
    let testError (errs:TestLr.ErrorRates) = (errs.test, errs.testStderr)
    let nnError (errs:TestLr.ErrorRates) = (errs.nn, errs.nnStderr)
    let errTypes = [trainingError; testError; nnError]

    let latexifyConfusableRow (settings, label:string) = 
        let bestErrs = 
            ResultParsing.chooseResults allResults settings 
            |> ResultParsing.groupResultsByLr //list of LRs, each has a list of results in file order
            |> List.map snd //ignore lr
            |> List.map ResultParsing.meanStderrOfErrs //get err distrib
            |> List.map (fun  err -> 
                    errTypes |> List.map ((|>) err)
                )//ResultParsing.meanStderrOfErrs errs |> errTypeSelector))
            |> List.sort
            |> List.head

        label
        + String.concat "" (List.map (fun (mean, stderr) -> if System.Double.IsNaN mean then " & " else sprintf @" & $%.1f \pm %.2f $" (mean*100.) (stderr*100.)) bestErrs)
            
    @"\noindent " + title  + " (best run):\\\\\n" + 
        @"\begin{tabular}{@{}lrrr@{}}\toprule"+"\n"
        + @"     \multicolumn{1}{c}{Model Type}   & \multicolumn{1}{c}{training error} & \multicolumn{1}{c}{test error} & \multicolumn{1}{c}{NN error} \\\midrule" + "\n" //&  & &  &
        + String.concat "\\\\\n" (settingsList |> List.map latexifyConfusableRow)
        + "\n" + @"\bottomrule\end{tabular}"

