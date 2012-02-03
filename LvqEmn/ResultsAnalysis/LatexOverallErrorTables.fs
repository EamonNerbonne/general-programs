module LatexOverallErrorTables
open LvqGui
open EmnExtensions

//For table in Results section with overall error rates of the basic methods.
let lvqMethodsOptimalLrErrorsTable (title:string)  (allResults:list<LrOptimizationResult>) settingsList =
    let errorExtractors = [LrOptResults.extractTrainingError; LrOptResults.extractTestError; LrOptResults.extractNnError]

    let lvqMethodErrorRateRow settings = 
        let bestErrs = 
            LrOptResults.groupErrorsByLrForSetting allResults settings //list of LRs, each has a list of results in file order
            |> List.map snd //ignore lr
            |> List.map LrOptResults.meanStderrOfErrs //get err distrib
            |> List.map (fun err -> errorExtractors |> List.map ((|>) err))
            |> List.sort //implicitly by first entry, the mean training error rate.
            |> List.head //select best error rates corresponding to crossvalidated runs with lr having lowest mean training error rate

        (settings.ToShorthand())
        + String.concat "" (List.map (fun (mean, stderr) -> if System.Double.IsNaN mean then " & " else sprintf @" & $%.1f \pm %.2f $" (mean*100.) (stderr*100.)) bestErrs)
            
    @"\noindent " + title  + " (best run):\\\\\n" + 
        @"\begin{tabular}{@{}lrrr@{}}\toprule"+"\n"
        + @"     \multicolumn{1}{c}{Model Type}   & \multicolumn{1}{c}{training error} & \multicolumn{1}{c}{test error} & \multicolumn{1}{c}{NN error} \\\midrule" + "\n" //&  & &  &
        + String.concat "\\\\\n" (settingsList |> List.map lvqMethodErrorRateRow)
        + "\n" + @"\bottomrule\end{tabular}"


//For table in Results section with overall error rates at various non-optimal lrs
let lvqMethodsNonOptimalLrErrorsTable (title:string)  (allResults:list<LrOptimizationResult>) settingsList =
    let latexifyConfusableRow settings = 
        let resultsByLr = 
            LrOptResults.groupErrorsByLrForSetting allResults settings //list of LRs, each has a list of results in file order
            |> List.map snd //ignore lr
            |> List.map (List.map (LrOptResults.extractTrainingError >> fst))//LrOptResults.meanStderrOfErrs errs |> errTypeSelector))
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

        (settings.ToShorthand())
        + String.concat "" (Seq.map (fun (trainErrs) -> " & " + prErr trainErrs) rankedResults)
        + sprintf " & $%.1f" (100. - 100. * confusableRatio) + "\\%$ "
        // + " & " + sprintf " $ %1.3f $ & $ %1.3f $" bestlr.Lr0 bestlr.LrP + (if bestlr.LrB > 0.0 then sprintf  " & $ %1.3f $" bestlr.LrB else "&")
        
    @"\noindent " + title  + " (training error):\\\\\n" + 
        @"\begin{tabular}{@{}lrrrrl@{}}\toprule"+"\n"
        + @"  \multicolumn{1}{c}{Model Type} & \multicolumn{1}{c}{best} &\multicolumn{1}{c}{\footnotesize upper quartile} & \multicolumn{1}{c}{median} &\multicolumn{1}{c}{\footnotesize lower quartile} & \multicolumn{1}{c}{lr-relevance} \\\midrule" + "\n" //&  & &  &
        + String.concat "\\\\\n" (settingsList |> List.map latexifyConfusableRow)
        + "\n" + @"\bottomrule\end{tabular}"


