module LatexifyResults
open LvqGui
open EmnExtensions
open System

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

let latexifyConfusable (title:string)  (results:list<DatasetResults>) settingsList =
    let latexifyConfusableRow (settings, label:string) = 
        let rawResults = results |> ResultParsing.filterResults settings |> ResultParsing.groupResultsByLr //list of LRs, each has a list of results in file order
        let results = 
            rawResults
            |> List.map (Utils.apply2nd ResultParsing.meanStderrOfErrs) //list of LRs, each mean+stderrs for error rates
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
            
    @"\noindent " + title  + ":\\\\\n" + 
        @"\begin{tabular}{@{}lllllll@{}}"+"\n"
        + @"model &\multicolumn{2}{c}{training error rate} & \multicolumn{2}{c}{test error rate} &\multicolumn{2}{c}{NN error rate}  \\" + "\n" //& better than & $\lr_0$ & $\lr_P$ & $\lr_B$
        + @"        & best &\footnotesize upper quartile & best &\footnotesize upper quartile & best &\footnotesize upper quartile \\\hline" + "\n" //&  & &  &
        + String.Join("\\\\\n", settingsList |> List.map latexifyConfusableRow (*|> List.sort*) |> List.map snd)
        + "\n" + @"\end{tabular}"

