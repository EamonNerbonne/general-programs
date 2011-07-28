#I @"ResultsAnalysis\bin\ReleaseMingw"
#r "ResultsAnalysis"
#r "LvqLibCli"
#r "LvqGui"
#r "EmnExtensions"
#time "on"

open EmnExtensions

for resStr in
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
                                Utils.latexstderr trn + " " + Utils.latexstderr tst + 
                                    if nn.Mean.IsFinite() then
                                        " " + Utils.latexstderr nn
                                    else
                                        ""
                            let avgResult = (res.Results |> Array.average) * 100.
                            "   " + res.DatasetTweaks + " " + (res.ModelSettings.ToShorthand()) + errStr
                            )
                        key + "\n" + String.concat "\n" mappedGroup + "\n\n"
                    )
        do
    printfn "%s" resStr
    

