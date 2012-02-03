#I @"ResultsAnalysis\bin\ReleaseMingw2"
#r "ResultsAnalysis"
#r "LvqLibCli"
#r "LvqGui"
#r "EmnExtensions"
#time "on"

open EmnExtensions
open System.Collections.Generic
open System.IO



LvqRunAnalysis.analyzedModels ()
    |> Seq.groupBy (fun res-> res.DatasetBaseShorthand) 
    |> Seq.map 
        (fun (key, group) ->
            let sortedGroup = group |> Seq.toArray |> Array.map (fun res -> ((res.Results |> Array.map (fun r-> r.TrainingError) |> Array.average, res.MeanError), res)) |> Array.sortBy fst |> Array.map snd
            let mappedGroup = sortedGroup |> Seq.map (fun res -> 
                let trn = res.Results |> Seq.map (fun x->x.TrainingError*100.) |> Seq.toList |> Utils.sampleDistribution
                let tst = res.Results |> Seq.map (fun x->x.TestError*100.) |> Seq.toList |> Utils.sampleDistribution
                let nn = res.Results |> Seq.map (fun x->x.NnError*100.) |> Seq.toList |> Utils.sampleDistribution
                let (bestTrn, bestTest, bestNn) = res.Results |> Array.minBy (fun (x:LvqRunAnalysis.SingleLvqRunOutcome) -> x.TrainingError) |> (fun x -> (x.TrainingError*100.,x.TestError*100.,x.NnError*100.))
                let errStr =
                    Utils.latexstderr trn + "(" + bestTrn.ToString("f1") + ")" + "&" + Utils.latexstderr tst + "(" + bestTest.ToString("f1") + ")" + "&" + 
                        if nn.Mean.IsFinite() then
                            Utils.latexstderr nn + "(" + bestNn.ToString("f1") + ")"
                        else
                            ""
                "   " +  LvqRunAnalysis.latexLiteral res.DatasetTweaks + "&" + LvqRunAnalysis.latexLiteral (res.ModelSettings.WithDefaultLr().ToShorthand()) + "&" + errStr
                )
            @"\section{" + LvqRunAnalysis.friendlyDatasetLatexName key + "}\n\n"
                + @"\noindent\begin{longtable}{lllll}\toprule \multicolumn{2}{l}{model \& tweaks} & training error & test error& NN error\\\midrule" + "\n" 
                + String.concat "\\\\\n" mappedGroup
                + "\n" + @"\\ \bottomrule\end{longtable}" + "\n\n" 
        )
    |> String.concat ""
    |>  (fun contents -> File.WriteAllText(EmnExtensions.Filesystem.FSUtil.FindDataDir(@"uni\Thesis\doc", System.Reflection.Assembly.GetAssembly(typeof<LvqGui.CreateDataset>)).FullName + @"\RankResults.tex", contents))
    

