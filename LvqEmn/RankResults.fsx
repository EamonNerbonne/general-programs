#I @"ResultsAnalysis\bin\ReleaseMingw"
#r "ResultsAnalysis"
#r "LvqLibCli"
#r "LvqGui"
#r "EmnExtensions"
#time "on"

open EmnExtensions
open System.Collections.Generic
open System.IO



ResultAnalysis.analyzedModels ()
    |> Seq.groupBy (fun res-> res.DatasetBaseShorthand) 
    |> Seq.map 
        (fun (key, group) ->
            let sortedGroup = group |> Seq.sortBy (fun res -> res.MeanError)
            let mappedGroup = sortedGroup |> Seq.map (fun res -> 
                let trn = res.Results |> Seq.map (fun x->x.TrainingError*100.) |> Seq.toList |> Utils.sampleDistribution
                let tst = res.Results |> Seq.map (fun x->x.TestError*100.) |> Seq.toList |> Utils.sampleDistribution
                let nn = res.Results |> Seq.map (fun x->x.NnError*100.) |> Seq.toList |> Utils.sampleDistribution
                let errStr =
                    Utils.latexstderr trn + "&" + Utils.latexstderr tst + "&" + 
                        if nn.Mean.IsFinite() then
                            Utils.latexstderr nn
                        else
                            ""
                "   " +  ResultAnalysis.latexLiteral res.DatasetTweaks + "&" + ResultAnalysis.latexLiteral (res.ModelSettings.WithDefaultLr().ToShorthand()) + "&" + errStr
                )
            @"\noindent " + ResultAnalysis.niceDatasetName key + "\n\n"
                + @"\noindent\begin{longtable}{lllll}\toprule \multicolumn{2}{l}{model \& tweaks} & training error & test error& NN error\\\midrule" + "\n" 
                + String.concat "\\\\\n" mappedGroup
                + "\n" + @"\\ \bottomrule\end{longtable}" + "\n\n" 
        )
    |> String.concat ""
    |>  (fun contents -> File.WriteAllText(EmnExtensions.Filesystem.FSUtil.FindDataDir(@"uni\Thesis\doc", System.Reflection.Assembly.GetAssembly(typeof<LvqGui.CreateDataset>)).FullName + @"\RankResults.tex", contents))
    

