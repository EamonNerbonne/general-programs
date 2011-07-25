module LatexifyCompareMethods
open LvqGui
open LvqLibCli
open EmnExtensions
open System

let latexifyCompareMethods (datasetName:string) (allresults:list<DatasetResults>) settingsList =
    let latexifyConfusableRow (baseSettings:LvqModelSettingsCli, settings:LvqModelSettingsCli, label:string) = 
        let getResults s = 
            allresults |> ResultParsing.filterResults s |> ResultParsing.groupResultsByLr //list of LRs, each has a list of results in file order
            |> List.map snd //list of LRs without their values, each  has a list of results in file order
            |> List.map (fun errs -> (ResultParsing.meanStderrOfErrs errs, errs))//list of LRs, each mean+stderrs for error rates
            |> List.sortBy (fst>> (fun err-> err.ErrorMean))
        let results = getResults settings
        let basicResults = getResults baseSettings
        let (besterr, besterrs) = List.head results 
        let (err75th, err75ths) = List.length results / 4 |> List.nth results
        let (besterrB ,besterrBs)= List.head basicResults 
        let (err75thB ,err75thBs)= List.length basicResults / 4 |> List.nth basicResults

        let significantDiff errsA errsB errSelector =
            Utils.twoTailedPairedTtest (errsA |>List.map errSelector) (errsB |> List.map errSelector)
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

