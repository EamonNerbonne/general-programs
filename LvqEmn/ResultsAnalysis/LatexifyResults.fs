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

let latexifyConfusable (title:string)  (allResults:list<DatasetResults>) settingsList =
    let latexifyConfusableRow errTypeSelector (settings, label:string) = 
        let resultsByLr = 
            ResultParsing.chooseResults allResults settings |> ResultParsing.groupResultsByLr //list of LRs, each has a list of results in file order
            |> List.map (fun (lr, errs) -> (errs |> List.map (errTypeSelector >> fst), ResultParsing.meanStderrOfErrs errs |> errTypeSelector, lr))
            |> List.sortBy (fun (errmean, _, _) -> List.average errmean)
            |> List.toArray
        let resultCount =  Array.length resultsByLr
        let rankedResults =
            [|0; resultCount / 4; resultCount * 2 / 4; resultCount * 3 / 4; resultCount - 1 |] 
            |> Array.map (Array.get resultsByLr)
        let (bestMeans,_,_) = rankedResults.[0]    

        let confusableCount =
            resultsByLr |> Seq.skip 1
            |> Seq.map (fun (errmean,_,_) -> Utils.twoTailedPairedTtest bestMeans errmean)
            |> Seq.filter (fun (firstIsBetter, p) -> not firstIsBetter || p > 0.05)
            |> Seq.length
        let confusableRatio = float confusableCount / float(resultCount - 1)
        let prErr(mean, stderr) =  sprintf @"$%.1f \pm %.2f $" (mean*100.) (stderr*100.)

        label
        + String.concat "" (Seq.map (fun (_,meanStderr,_) -> " & " + prErr meanStderr) rankedResults)
        + sprintf " & $%f" (100. - 100. * confusableRatio) + "\\%$ "
        // + " & " + sprintf " $ %1.3f $ & $ %1.3f $" bestlr.Lr0 bestlr.LrP + (if bestlr.LrB > 0.0 then sprintf  " & $ %1.3f $" bestlr.LrB else "&")
        
            
    @"\noindent " + title  + " (training error):\\\\\n" + 
        @"\begin{tabular}{@{}lllllll@{}}"+"\n"
        + @"     model   & best &\footnotesize upper quartile & median &\footnotesize lower quartile & worst & certainty \\\hline" + "\n" //&  & &  &
        + String.concat "\\\\\n" (settingsList |> List.map (latexifyConfusableRow (fun err-> (err.training, err.trainingStderr))))
        + "\n" + @"\end{tabular}"

