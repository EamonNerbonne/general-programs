#I @"ResultsAnalysis\bin\ReleaseMingw"
#r "ResultsAnalysis"
#r "LvqLibCli"
#r "LvqGui"
#r "EmnExtensions"
#r "PresentationCore"
#r "WindowsBase"
#r "EmnExtensionsWpf"
#time "on"

open LvqLibCli
open LvqGui
open System.Threading
open System
open System.Windows.Media
open EmnExtensions.Wpf
open Utils
open System.Threading.Tasks

let datasets = 
    [
        for datasetFactory in CreateDataset.StandardDatasets() do
            datasetFactory.NormalizeDimensions <- true
            yield datasetFactory.CreateDataset()
    ]
           
let makeLvqSettings modelType prototypes lrB lrP lr0 = 
    let mutable tmp = new LvqModelSettingsCli()
    tmp.NgInitializeProtos <- true
    tmp.SlowStartLrBad <- true
    tmp.RandomInitialProjection <- true
    tmp.ModelType <- modelType
    tmp.PrototypesPerClass <- prototypes
    tmp.LR0 <- lr0
    tmp.LrScaleB <- lrB
    tmp.LrScaleP <- lrP
    tmp
    

let Ggm1 = makeLvqSettings LvqModelType.Ggm 1
let Ggm5 = makeLvqSettings LvqModelType.Ggm 5
let G2m1 = makeLvqSettings LvqModelType.G2m 1
let G2m5 = makeLvqSettings LvqModelType.G2m 5
let Gm1 = makeLvqSettings LvqModelType.Gm 1 0.
let Gm5 = makeLvqSettings LvqModelType.Gm 5 0.
let Lgm1 = makeLvqSettings LvqModelType.Lgm 1 0.
let Lgm5 = makeLvqSettings LvqModelType.Lgm 5 0.

type TestResults = { GeoMean:float; Mean:float;  Results:float list list []; Settings:LvqModelSettingsCli; }

let printResults results =
    let trainDistr = results.Results |> List.concat |> List.map  (fun es -> List.nth es 0) |> Utils.sampleDistribution
    let testDistr = results.Results |> List.concat |> List.map  (fun es -> List.nth es 1) |> Utils.sampleDistribution
    let x = (results.Results.[0] |> List.head) |> List.length

    if x = 3 then 
        let nnDistr = results.Results |> List.concat |> List.map  (fun es -> List.nth es 2) |> Utils.sampleDistribution
        printfn "%s GeoMean: %f; Training: %f ~ %f; Test: %f ~ %f; NN: %f ~ %f" (results.Settings.ToShorthand ()) results.GeoMean trainDistr.Mean trainDistr.StdErr testDistr.Mean testDistr.StdErr nnDistr.Mean nnDistr.StdErr
    else 
        printfn "%s GeoMean: %f; Training: %f ~ %f; Test: %f ~ %f" (results.Settings.ToShorthand ()) results.GeoMean trainDistr.Mean trainDistr.StdErr testDistr.Mean testDistr.StdErr


    

let iterCount = 1e7

let rnd = new EmnExtensions.MathHelpers.MersenneTwister ()

let testSettings parOverride rndSeed (settings : LvqModelSettingsCli) =
    let results =
        [
            for dataset in datasets ->
                Task.Factory.StartNew( (fun () ->
                    let mutable parsettings = settings
                    parsettings.ParallelModels <- parOverride
                    parsettings.ParamsSeed <- 2u * rndSeed + 1u
                    parsettings.InstanceSeed <- 2u * rndSeed
                    
                    let model = new LvqMultiModel(dataset,parsettings,false)
                    model.TrainUptoIters(iterCount,dataset, CancellationToken.None)
                    let errs = 
                        model.EvaluateFullStats() 
                        |> Seq.map (fun stat-> 
                            [
                                yield stat.values.[LvqTrainingStatCli.TrainingErrorI]
                                yield stat.values.[LvqTrainingStatCli.TestErrorI]
                                if model.nnErrIdx > 0 then
                                    yield stat.values.[model.nnErrIdx]    
                            ]
                        ) |> List.ofSeq
                    errs
                ), TaskCreationOptions.LongRunning)
        ] |> List.map (fun task -> task.Result) |> List.toArray

    let averageErr = results |> Array.averageBy (List.averageBy List.average)
    let geomAverageErr= results  |> Array.averageBy (List.averageBy List.average >> Math.Log) |> Math.Exp
    { GeoMean = geomAverageErr; Mean = averageErr;Settings = settings; Results = results}



let logscale steps (v0, v1) = 
    let lnScale = Math.Log(v1 / v0)
    [ for i in [0..steps-1] -> v0 * Math.Exp(lnScale * (float i / (float steps - 1.))) ]

    //[0.001 -> 0.1]

let lrsChecker rndSeed lr0range settingsFactory = 
    [ for lr0 in lr0range ->  Task.Factory.StartNew ((fun () -> lr0 |> settingsFactory |> testSettings 2 rndSeed), TaskCreationOptions.LongRunning) ]
    |> Array.ofList
    |> Array.map (fun task -> task.Result)
    |> Array.sortBy (fun res -> res.GeoMean)


type ControllerState = { Unpacker: LvqModelSettingsCli -> float; Packer: LvqModelSettingsCli -> float -> LvqModelSettingsCli; DegradedCount: int; LrLogDevScale: float }
let lrBcontrol = { 
        Unpacker = (fun settings-> settings.LrScaleB)
        Packer = fun (settings:LvqModelSettingsCli) lrB -> settings.WithLrChanges(settings.LR0, settings.LrScaleP, lrB)
        DegradedCount = 0
        LrLogDevScale = 1.
    }
let lrPcontrol = {
        Unpacker = fun settings -> settings.LrScaleP
        Packer = fun settings lrP -> settings.WithLrChanges(settings.LR0, lrP, settings.LrScaleB)
        DegradedCount = 0
        LrLogDevScale = 1.
    }
let lr0control = {
        Unpacker = fun settings -> settings.LR0
        Packer = fun settings lr0 -> settings.WithLrChanges(lr0, settings.LrScaleP, settings.LrScaleB)
        DegradedCount = 0
        LrLogDevScale = 1.
    }

let improveLr (testResultList:TestResults list) (lrUnpack, lrPack) =
    let errMargin = 0.0000001
    let unpackLogErrs testResults = testResults.Results |> Seq.concat |> Seq.concat |> List.ofSeq |> List.map (fun err -> Math.Log (err + errMargin))
    let bestToWorst = testResultList |> List.sortBy (unpackLogErrs >> List.average)
    let bestLogErrs = List.head bestToWorst |> unpackLogErrs
           
       
    //extract list of error rates from each testresult
    let logLrs = testResultList |> List.map (fun res-> lrUnpack res.Settings |> Math.Log)
    let relevance = List.Cons (1.0, bestToWorst |> List.tail |> List.map (unpackLogErrs >> Utils.twoTailedPairedTtest bestLogErrs >> snd))

    let relLength = List.length relevance
    let linearlyScaledRelevance = List.init relLength (fun i -> float (relLength - i) / float relLength)

    let effRelevance = List.zip relevance linearlyScaledRelevance |> List.map (fun (a,b) -> a + b)
    
    //printfn "%A" (bestToWorst |> List.map (fun res->lrUnpack res.Settings) |> List.zip relevance)
    let logLrDistr = List.zip logLrs effRelevance |> List.fold (fun (ss:SmartSum) (lr, rel) -> ss.CombineWith lr rel) (new SmartSum ())
    let (logLrmean, logLrdev) = (logLrDistr.Mean, Math.Sqrt logLrDistr.Variance)
    (Math.Exp logLrmean, logLrdev)

let improvementStep (controller:ControllerState) (initialSettings:LvqModelSettingsCli) =
    let currSeed = rnd.NextUInt32 ()
    let initResults = testSettings 10 currSeed initialSettings
    let baseLr = controller.Unpacker initialSettings
    let lowLr = baseLr * Math.Exp(-Math.Sqrt(6.) * controller.LrLogDevScale)
    let highLr = baseLr * Math.Exp(Math.Sqrt(6.) * controller.LrLogDevScale)
    let results = lrsChecker (currSeed + 2u) (logscale 40 (lowLr,highLr)) (controller.Packer initialSettings)
    let (newBaseLr, newLrLogDevScale) = improveLr (List.ofArray results) (controller.Unpacker, controller.Packer)
    let logLrDiff_LrDevScale = Math.Abs(Math.Log(baseLr / newBaseLr))
    let effNewLrDevScale = 0.3*newLrLogDevScale + 0.4*controller.LrLogDevScale + 0.4*logLrDiff_LrDevScale
    let newSettings = controller.Packer initialSettings newBaseLr
    let finalResults =  testSettings 10 currSeed newSettings
    printfn "   [%f..%f]: %f -> %f: %f -> %f"  lowLr highLr baseLr newBaseLr initResults.GeoMean finalResults.GeoMean
    if finalResults.GeoMean > initResults.GeoMean then
        ({ Unpacker = controller.Unpacker; Packer = controller.Packer; DegradedCount = controller.DegradedCount + 1; LrLogDevScale = effNewLrDevScale }, newSettings)
    else
        ({ Unpacker = controller.Unpacker; Packer = controller.Packer; DegradedCount = controller.DegradedCount; LrLogDevScale = effNewLrDevScale }, newSettings)

let improvementSteps (controllers:ControllerState list) (initialSettings:LvqModelSettingsCli) =
    List.fold (fun (controllerStates, settings) nextController ->
            let (newControllerState, newSettings) = improvementStep nextController settings
            (newControllerState :: controllerStates, newSettings)
        ) ([], initialSettings) controllers
    |> apply1st List.rev

let rec fullyImprove (controllers:ControllerState list) (initialSettings:LvqModelSettingsCli) =
    if controllers |> List.sumBy (fun controllerState -> controllerState.DegradedCount) > 3 * (List.length controllers) then
        (initialSettings, controllers)
    else
        let (nextControllers, nextSettings) = improvementSteps controllers initialSettings
        fullyImprove nextControllers nextSettings

let improveAndTest (initialSettings:LvqModelSettingsCli) =
    let needsB = [LvqModelType.G2m; LvqModelType.Ggm ; LvqModelType.Gpq] |> List.exists (fun modelType -> initialSettings.ModelType = modelType)
    let controllers = 
        [
            if needsB then yield lrBcontrol
            yield lrPcontrol
            yield lr0control
       ]
    let improvedSettings = fullyImprove controllers initialSettings |> fst
    let testedResults = testSettings 10 1u improvedSettings //GeoMean: 0.1981672332 Mean: 0.2310214097
    printResults testedResults
    testedResults


[ "Gm+,1,!lr00.002,lrP0.7,"; "Gm+,5,NGi+,!lr00.003,lrP5.0,";  "G2m+,1,!lr00.01,lrP0.2,lrB0.003,"; "G2m+,5,NGi+,!lr00.01,lrP0.1,lrB0.004,"; "Ggm+,1,!lr00.03,lrP0.05,lrB2.0,"; "Ggm+,5,NGi+,!lr00.04,lrP0.05,lrB10.0,"]
    |> List.map CreateLvqModelValues.ParseShorthand |> List.map improveAndTest
//Gm+,1,!lr00.0038877816654436658,lrP0.96761971430277682, GeoMean: 0.197028; Training: 0.234192 ~ 0.018166; Test: 0.236151 ~ 0.018031; NN: 0.228910 ~ 0.013086
//Gm+,5,NGi+,!lr00.0036724339401134373,lrP1.9369353690973385, GeoMean: 0.145380; Training: 0.141175 ~ 0.005675; Test: 0.146742 ~ 0.006303; NN: 0.187359 ~ 0.009059
//G2m+,1,!lr00.010951089127661249,lrP0.37429179637936294,lrB0.0059060776640765212, GeoMean: 0.132667; Training: 0.153962 ~ 0.013695; Test: 0.161566 ~ 0.013537; NN: 0.179985 ~ 0.012951
//G2m+,5,NGi+,!lr00.016371733635629902,lrP0.094684549417746067,lrB0.0037018583189398729, GeoMean: 0.111405; Training: 0.106720 ~ 0.005719; Test: 0.116140 ~ 0.006153; NN: 0.155239 ~ 0.008940
//Ggm+,1,!lr00.034895439303295049,lrP0.041804162402435605,lrB2.7245635557464731, GeoMean: 0.140815; Training: 0.155720 ~ 0.014295; Test: 0.168813 ~ 0.014235; NN: 0.192676 ~ 0.014554
//Ggm+,5,NGi+,!lr00.043391990424161225,lrP0.04680103944403835,lrB16.253814831422766, GeoMean: 0.109878; Training: 0.100954 ~ 0.005817; Test: 0.116450 ~ 0.006573; NN: 0.158399 ~ 0.008808

//Gm+,1,!lr00.0032605836301787235,lrP0.91550094794305437, GeoMean: 0.195892; Training: 0.229223 ~ 0.016745; Test: 0.232463 ~ 0.016841; NN: 0.228119 ~ 0.013028
//Gm+,5,NGi+,!lr00.0016916790280145934,lrP7.3562041993590954, GeoMean: 0.141624; Training: 0.137416 ~ 0.005775; Test: 0.143477 ~ 0.006256; NN: 0.184498 ~ 0.008855
//G2m+,1,!lr00.010538314860805451,lrP0.40631608304354033,lrB0.0064602180503107428, GeoMean: 0.133286; Training: 0.154912 ~ 0.013729; Test: 0.161751 ~ 0.013694; NN: 0.181342 ~ 0.013117
//G2m+,5,NGi+,!lr00.0074874287681329218,lrP0.22531169432780104,lrB0.00493468607857892, GeoMean: 0.109813; Training: 0.106935 ~ 0.005562; Test: 0.115834 ~ 0.006160; NN: 0.150114 ~ 0.008946
//Ggm+,1,!lr00.049422955414705663,lrP0.034563424853277583,lrB2.0526607403667207, GeoMean: 0.131784; Training: 0.148169 ~ 0.013687; Test: 0.160614 ~ 0.013694; NN: 0.189558 ~ 0.014458
//Ggm+,5,NGi+,!lr00.026279882412297943,lrP0.094265037818760056,lrB14.330659337780681, GeoMean: 0.108665; Training: 0.099974 ~ 0.005696; Test: 0.115108 ~ 0.006369; NN: 0.156766 ~ 0.008771


[ "Gm+,1,lr00.002,lrP0.7,"; "Gm+,5,NGi+,lr00.003,lrP5.0,";  "G2m+,1,lr00.01,lrP0.2,lrB0.003,"; "G2m+,5,NGi+,lr00.01,lrP0.1,lrB0.004,"; "Ggm+,1,lr00.03,lrP0.05,lrB2.0,"; "Ggm+,5,NGi+,lr00.04,lrP0.05,lrB10.0,"]
    |> List.map CreateLvqModelValues.ParseShorthand |> List.map improveAndTest
//Gm+,1,lr00.00098270312538638934,lrP9.0626741559847943, GeoMean: 0.206106; Training: 0.253717 ~ 0.021686; Test: 0.256605 ~ 0.021680; NN: 0.244480 ~ 0.015974
//Gm+,5,NGi+,lr00.00081332088181361136,lrP26.174596521634275, GeoMean: 0.145705; Training: 0.140903 ~ 0.005951; Test: 0.145768 ~ 0.006278; NN: 0.189763 ~ 0.009276
//G2m+,1,lr00.016219042764755845,lrP0.27074656229324845,lrB0.013350765377962691, GeoMean: 0.140301; Training: 0.174442 ~ 0.017533; Test: 0.179120 ~ 0.017556; NN: 0.171446 ~ 0.011983
//G2m+,5,NGi+,lr00.013332291869100408,lrP0.18481590981531068,lrB0.023736517302622644, GeoMean: 0.109488; Training: 0.105352 ~ 0.005656; Test: 0.113732 ~ 0.006236; NN: 0.152372 ~ 0.008331
//Ggm+,1,lr00.02105140233885688,lrP0.072165798638470016,lrB2.7326430403530138, GeoMean: 0.136427; Training: 0.149448 ~ 0.012191; Test: 0.160600 ~ 0.012190; NN: 0.180182 ~ 0.011276
//Ggm+,5,NGi+,lr00.029766903978754377,lrP0.072423698854638424,lrB4.0124612266097115, GeoMean: 0.114297; Training: 0.108171 ~ 0.006533; Test: 0.121487 ~ 0.007167; NN: 0.164169 ~ 0.009634

//Gm+,1,lr00.00083129868071435311,lrP9.40140414308464, GeoMean: 0.204682; Training: 0.256835 ~ 0.023181; Test: 0.259658 ~ 0.023270; NN: 0.233951 ~ 0.013975
//Gm+,5,NGi+,lr00.0026863899623534074,lrP5.5799894531904828, GeoMean: 0.151152; Training: 0.146552 ~ 0.006324; Test: 0.153292 ~ 0.006461; NN: 0.197233 ~ 0.009485
//G2m+,1,lr00.0126816946381224,lrP0.3731955896628123,lrB0.015963050125202213, GeoMean: 0.137810; Training: 0.174878 ~ 0.018762; Test: 0.183774 ~ 0.018756; NN: 0.179263 ~ 0.012701
//G2m+,5,NGi+,lr00.015982112807995188,lrP0.16418415484681179,lrB0.020804266072074272, GeoMean: 0.108013; Training: 0.104684 ~ 0.005707; Test: 0.112163 ~ 0.006132; NN: 0.152229 ~ 0.008916
//Ggm+,1,lr00.011287201395125179,lrP0.2677634228825439,lrB5.1700604140197814, GeoMean: 0.132069; Training: 0.152907 ~ 0.014826; Test: 0.162317 ~ 0.014676; NN: 0.181650 ~ 0.012604
//Ggm+,5,NGi+,lr00.013296013237425139,lrP0.15275170287700551,lrB26.064942920488544, GeoMean: 0.108809; Training: 0.101007 ~ 0.005710; Test: 0.114030 ~ 0.006219; NN: 0.156461 ~ 0.008636


[ "G2m+,1,Bi+,!lr00.01,lrP0.2,lrB0.003,"; "G2m+,5,NGi+,Bi+,!lr00.01,lrP0.1,lrB0.004,";  "Ggm+,1,Bi+,!lr00.03,lrP0.05,lrB2.0,"; "Ggm+,5,NGi+,Bi+,!lr00.04,lrP0.05,lrB10.0,"]
    |> List.map CreateLvqModelValues.ParseShorthand |> List.map improveAndTest
//G2m+,1,Bi+,!lr00.0042575713901870215,lrP0.23566779393890058,lrB0.013347759645087141, GeoMean: 0.164773; Training: 0.188449 ~ 0.015121; Test: 0.193657 ~ 0.014934; NN: 0.186347 ~ 0.010953
//G2m+,5,NGi+,Bi+,!lr00.0054105884795896822,lrP0.36619086938970807,lrB0.0019875156608729866, GeoMean: 0.118134; Training: 0.112018 ~ 0.005490; Test: 0.121278 ~ 0.005684; NN: 0.155925 ~ 0.008723
//Ggm+,1,Bi+,!lr00.053193037678940035,lrP0.026519611029561182,lrB1.0508221368550432, GeoMean: 0.132066; Training: 0.145392 ~ 0.013406; Test: 0.157742 ~ 0.013357; NN: 0.183087 ~ 0.013402
//Ggm+,5,NGi+,Bi+,!lr00.024087616475981764,lrP0.10296334404356114,lrB17.232147209488033, GeoMean: 0.109633; Training: 0.099677 ~ 0.005558; Test: 0.115388 ~ 0.006233; NN: 0.157533 ~ 0.008669


[ "Gm+,5,lr00.003,lrP5.0,"; "G2m+,5,lr00.01,lrP0.1,lrB0.004,"; "Ggm+,5,lr00.04,lrP0.05,lrB10.0,"]
     |> List.map CreateLvqModelValues.ParseShorthand |> List.map improveAndTest
//Gm+,5,lr00.013466319727939526,lrP9.2490769881518879, GeoMean: 0.152351; Training: 0.153097 ~ 0.007991; Test: 0.157766 ~ 0.008278; NN: 0.199236 ~ 0.011037
//G2m+,5,lr00.025321404445647736,lrP0.0800462661976979,lrB0.0068513463537344128, GeoMean: 0.109231; Training: 0.110061 ~ 0.006579; Test: 0.117615 ~ 0.007107; NN: 0.153866 ~ 0.009236
//Ggm+,5,lr00.017329664653224847,lrP0.050341633365966781,lrB15.85961104642251, GeoMean: 0.120394; Training: 0.120002 ~ 0.008134; Test: 0.137662 ~ 0.008821; NN: 0.168194 ~ 0.010603


[ "Gm+,5,!lr00.003,lrP5.0,"; "G2m+,5,!lr00.01,lrP0.1,lrB0.004,"; "Ggm+,5,!lr00.04,lrP0.05,lrB10.0,"]
     |> List.map CreateLvqModelValues.ParseShorthand |> List.map improveAndTest
//Gm+,5,!lr00.014436364028250253,lrP4.1723684183930017, GeoMean: 0.145107; Training: 0.142599 ~ 0.006693; Test: 0.147589 ~ 0.007084; NN: 0.191037 ~ 0.009788
//G2m+,5,!lr00.021321341205010495,lrP0.091882049596472823,lrB0.004, GeoMean: 0.112979; Training: 0.109479 ~ 0.005921; Test: 0.117902 ~ 0.006281; NN: 0.156378 ~ 0.008937
//Ggm+,5,!lr00.050458991795075986,lrP0.024937315351058434,lrB6.70093253950599, GeoMean: 0.110744; Training: 0.103601 ~ 0.006250; Test: 0.119595 ~ 0.007006; NN: 0.160679 ~ 0.009186


[ "Gm+,1,rP,!lr00.002,lrP0.7,"; "Gm+,5,rP,NGi+,!lr00.003,lrP5.0,";  "G2m+,1,rP,!lr00.01,lrP0.2,lrB0.003,"; "G2m+,5,rP,NGi+,!lr00.01,lrP0.1,lrB0.004,"; "Ggm+,1,rP,!lr00.03,lrP0.05,lrB2.0,"; "Ggm+,5,rP,NGi+,!lr00.04,lrP0.05,lrB10.0,"]
    |> List.map CreateLvqModelValues.ParseShorthand |> List.map improveAndTest
//Gm+,1,rP,!lr00.003274127681551177,lrP0.19280549102670749, GeoMean: 0.193846; Training: 0.230588 ~ 0.016537; Test: 0.233951 ~ 0.016480; NN: 0.204625 ~ 0.009630
//Gm+,5,rP,NGi+,!lr00.0024536102484049231,lrP3.4724460053817294, GeoMean: 0.144691; Training: 0.138868 ~ 0.005641; Test: 0.145594 ~ 0.005976; NN: 0.187978 ~ 0.008637
//G2m+,1,rP,!lr00.0099241105519269675,lrP0.031037521857139733,lrB0.0065352371884436844, GeoMean: 0.131332; Training: 0.155407 ~ 0.014201; Test: 0.159582 ~ 0.014120; NN: 0.168842 ~ 0.010640
//G2m+,5,rP,NGi+,!lr00.00936991443614944,lrP0.10264028537980083,lrB0.0066972291944533014, GeoMean: 0.110524; Training: 0.104938 ~ 0.005535; Test: 0.115937 ~ 0.006136; NN: 0.152433 ~ 0.008660
//Ggm+,1,rP,!lr00.029535517493578928,lrP0.037032419539976817,lrB3.8896707177396559, GeoMean: 0.127776; Training: 0.140874 ~ 0.013210; Test: 0.154098 ~ 0.012986; NN: 0.167942 ~ 0.010504
//Ggm+,5,rP,NGi+,!lr00.01586769228292827,lrP0.021955029179340516,lrB19.098320487603498, GeoMean: 0.106001; Training: 0.094468 ~ 0.005201; Test: 0.113135 ~ 0.006414; NN: 0.154997 ~ 0.008878

//Gm+,1,rP,!lr00.0023470636537914176,lrP0.64159546099407383, GeoMean: 0.197164; Training: 0.235866 ~ 0.017909; Test: 0.239855 ~ 0.017835; NN: 0.214043 ~ 0.010307
//Gm+,5,rP,NGi+,!lr00.0016158659721191326,lrP3.2468108385746781, GeoMean: 0.140144; Training: 0.134894 ~ 0.005618; Test: 0.141088 ~ 0.006057; NN: 0.183045 ~ 0.008901
//G2m+,1,rP,!lr00.011105139389010876,lrP0.059436558911411613,lrB0.0061673466019574593, GeoMean: 0.147085; Training: 0.167414 ~ 0.016048; Test: 0.172554 ~ 0.015917; NN: 0.182163 ~ 0.012016
//G2m+,5,rP,NGi+,!lr00.0065223330884271709,lrP0.10644132253097752,lrB0.0083807363542505245, GeoMean: 0.107802; Training: 0.104606 ~ 0.005594; Test: 0.113470 ~ 0.006161; NN: 0.148986 ~ 0.008839
//Ggm+,1,rP,!lr00.036522029648289552,lrP0.027741269506042728,lrB1.8616657337784004, GeoMean: 0.122928; Training: 0.135814 ~ 0.012179; Test: 0.149239 ~ 0.012140; NN: 0.166960 ~ 0.010399
//Ggm+,5,rP,NGi+,!lr00.015451755083162108,lrP0.016035575978628532,lrB23.664516018325713, GeoMean: 0.106810; Training: 0.095237 ~ 0.005458; Test: 0.116384 ~ 0.007004; NN: 0.156972 ~ 0.009460


[ "Gm+,1,rP,Pi+,!lr00.002,lrP0.7,"; "Gm+,5,rP,NGi+,Pi+,!lr00.003,lrP5.0,";  "G2m+,1,rP,Pi+,!lr00.01,lrP0.2,lrB0.003,"; "G2m+,5,rP,NGi+,Pi+,!lr00.01,lrP0.1,lrB0.004,"; "Ggm+,1,rP,Pi+,!lr00.03,lrP0.05,lrB2.0,"; "Ggm+,5,rP,NGi+,Pi+,!lr00.04,lrP0.05,lrB10.0,"]
    |> List.map CreateLvqModelValues.ParseShorthand |> List.map improveAndTest
//Gm+,1,rP,Pi+,!lr00.0024621338529958485,lrP1.4333300416134289, GeoMean: 0.196328; Training: 0.229565 ~ 0.016633; Test: 0.232881 ~ 0.016667; NN: 0.224584 ~ 0.011820
//Gm+,5,rP,NGi+,Pi+,!lr00.0023070429441871829,lrP1.6958688200324006, GeoMean: 0.141687; Training: 0.136737 ~ 0.005870; Test: 0.145124 ~ 0.006571; NN: 0.184720 ~ 0.009411
//G2m+,1,rP,Pi+,!lr00.015805127871344311,lrP0.12006502828363812,lrB0.00399374009047381, GeoMean: 0.131810; Training: 0.155392 ~ 0.014361; Test: 0.160943 ~ 0.014340; NN: 0.182258 ~ 0.013472
//G2m+,5,rP,NGi+,Pi+,!lr00.016056757039138083,lrP0.0672885093281154,lrB0.00729471027012326, GeoMean: 0.109810; Training: 0.106990 ~ 0.005565; Test: 0.114263 ~ 0.006186; NN: 0.151996 ~ 0.008808
//Ggm+,1,rP,Pi+,!lr00.00600455666344858,lrP0.019610532810998588,lrB7.3065159834390485, GeoMean: 0.162128; Training: 0.178558 ~ 0.020440; Test: 0.190066 ~ 0.020332; NN: 0.180100 ~ 0.011334
//Ggm+,5,rP,NGi+,Pi+,!lr00.030660295859354536,lrP0.057562752246332685,lrB10, GeoMean: 0.110216; Training: 0.100585 ~ 0.005550; Test: 0.116573 ~ 0.006215; NN: 0.157756 ~ 0.008705

//Gm+,1,rP,Pi+,!lr00.0031804227334126293,lrP1.2531287690029553, GeoMean: 0.196382; Training: 0.232482 ~ 0.017111; Test: 0.234917 ~ 0.017101; NN: 0.220147 ~ 0.011370
//Gm+,5,rP,NGi+,Pi+,!lr00.0014965987555807928,lrP2.0558218657276783, GeoMean: 0.142069; Training: 0.135194 ~ 0.005584; Test: 0.145023 ~ 0.006206; NN: 0.184348 ~ 0.008748
//G2m+,1,rP,Pi+,!lr00.0092403322510045734,lrP0.16048650532539277,lrB0.0072743349381535517, GeoMean: 0.133198; Training: 0.160324 ~ 0.015494; Test: 0.165068 ~ 0.015446; NN: 0.182124 ~ 0.013157
//G2m+,5,rP,NGi+,Pi+,!lr00.0095593126001314466,lrP0.10137113499705307,lrB0.0057461936572128607, GeoMean: 0.108408; Training: 0.105682 ~ 0.005658; Test: 0.113627 ~ 0.006396; NN: 0.150587 ~ 0.009024
//Ggm+,1,rP,Pi+,!lr00.0025148455959565258,lrP0.079990110922454269,lrB25.576900294290173, GeoMean: 0.150617; Training: 0.164501 ~ 0.017728; Test: 0.177967 ~ 0.017515; NN: 0.177057 ~ 0.011727
//Ggm+,5,rP,NGi+,Pi+,!lr00.028246047251619048,lrP0.018545129728874864,lrB7.5814710191741312, GeoMean: 0.109993; Training: 0.099230 ~ 0.005475; Test: 0.117086 ~ 0.006789; NN: 0.157253 ~ 0.008937


[ "G2m+,1,rP,Bi+,!lr00.01,lrP0.2,lrB0.003,"; "G2m+,5,rP,NGi+,Bi+,!lr00.01,lrP0.1,lrB0.004,";  "Ggm+,1,rP,Bi+,!lr00.03,lrP0.05,lrB2.0,"; "Ggm+,5,rP,NGi+,Bi+,!lr00.04,lrP0.05,lrB10.0,"]
    |> List.map CreateLvqModelValues.ParseShorthand |> List.map improveAndTest
//G2m+,1,rP,Bi+,!lr00.0073465822586451755,lrP0.20635087705272231,lrB0.0078093990953440095, GeoMean: 0.170479; Training: 0.199028 ~ 0.016675; Test: 0.204476 ~ 0.016374; NN: 0.189640 ~ 0.011449
//G2m+,5,rP,NGi+,Bi+,!lr00.0041016834167051158,lrP0.14117930931640629,lrB0.0019488272250434663, GeoMean: 0.118191; Training: 0.112642 ~ 0.005094; Test: 0.121636 ~ 0.005985; NN: 0.154076 ~ 0.008524
//Ggm+,1,rP,Bi+,!lr00.032102932227514112,lrP0.029471001544332763,lrB2.6811184828637229, GeoMean: 0.121519; Training: 0.135138 ~ 0.012699; Test: 0.147892 ~ 0.012621; NN: 0.163031 ~ 0.010159
//Ggm+,5,rP,NGi+,Bi+,!lr00.013518875656570728,lrP0.044065072328627664,lrB13.770948971313608, GeoMean: 0.107899; Training: 0.097335 ~ 0.005415; Test: 0.112898 ~ 0.006467; NN: 0.156381 ~ 0.008638


[  "Ggm+,1,rP,Pi+,!lr00.03,lrP0.05,lrB2.0," ; "G2m+,1,Bi+,!lr00.01,lrP0.2,lrB0.003,";]//these have dubious results
    |> List.map CreateLvqModelValues.ParseShorthand |> List.map improveAndTest
//Ggm+,1,rP,Pi+,!lr00.017312776311494311,lrP0.03939857702056547,lrB2.3627186806992886, GeoMean: 0.179763; Training: 0.247141 ~ 0.033278; Test: 0.259587 ~ 0.032659; NN: 0.183608 ~ 0.011793
//G2m+,1,Bi+,!lr00.0075635039116126066,lrP0.23126856601273685,lrB0.0074754688380316075, GeoMean: 0.156818; Training: 0.176094 ~ 0.015290; Test: 0.182465 ~ 0.015208; NN: 0.189536 ~ 0.012917
//Ggm+,1,rP,Pi+,!lr00.0035106660148076637,lrP0.016232278317424579,lrB13.232916613888372, GeoMean: 0.152714; Training: 0.168280 ~ 0.013889; Test: 0.180639 ~ 0.014036; NN: 0.192337 ~ 0.012132
//G2m+,1,Bi+,!lr00.005973312365862838,lrP0.40642138141748779,lrB0.01068668264562603, GeoMean: 0.156372; Training: 0.181192 ~ 0.015413; Test: 0.186727 ~ 0.015355; NN: 0.188637 ~ 0.012829


//opt results found with slightly buggy lr-searching code:
//Gm+,1,!lr00.002198585515,lrP0.6836046038, GeoMean: 0.198965; Training: 0.231766 ~ 0.016449; Test: 0.235295 ~ 0.016556; NN: 0.231495 ~ 0.012812
//Gm+,5,NGi+,!lr00.002672680891,lrP4.536289905, GeoMean: 0.146610; Training: 0.139755 ~ 0.005796; Test: 0.150053 ~ 0.006342; NN: 0.191839 ~ 0.009377
//G2m+,1,!lr00.021797623944739782,lrP0.17013535127904061,lrB0.0028710442546792839, GeoMean: 0.132753; Training: 0.153857 ~ 0.013621; Test: 0.161181 ~ 0.013461; NN: 0.183951 ~ 0.013869
//G2m+,5,NGi+,!lr00.014854479268703827,lrP0.12643192802795739,lrB0.003687418675856426, GeoMean: 0.110085; Training: 0.107585 ~ 0.005728; Test: 0.115503 ~ 0.006225; NN: 0.152425 ~ 0.009088
//Ggm+,1,!lr00.029892794513821885,lrP0.054767623178213938,lrB2.3443026990433924, GeoMean: 0.130877; Training: 0.147363 ~ 0.013599; Test: 0.159768 ~ 0.013463; NN: 0.184574 ~ 0.013776
//Ggm+,5,NGi+,!lr00.041993068719849549,lrP0.05551136786774067,lrB11.462570954856234, GeoMean: 0.109760; Training: 0.100846 ~ 0.005662; Test: 0.115616 ~ 0.006224; NN: 0.156829 ~ 0.008567

//Gm+,1,lr00.0015362340577901401,lrP10.716927113263273, GeoMean: 0.204712; Training: 0.254759 ~ 0.023125; Test: 0.258960 ~ 0.023035; NN: 0.238742 ~ 0.015294
//Gm+,5,NGi+,lr00.0010506456510214184,lrP10.86820020351132, GeoMean: 0.145488; Training: 0.139510 ~ 0.005856; Test: 0.146856 ~ 0.006128; NN: 0.189431 ~ 0.009042
//G2m+,1,lr00.011351487563176185,lrP0.37880915860796677,lrB0.019197822041416398, GeoMean: 0.136813; Training: 0.176091 ~ 0.019647; Test: 0.183255 ~ 0.019495; NN: 0.178905 ~ 0.013180
//Ggm+,1,lr00.026198578230780471,lrP0.13652588690969647,lrB1.2496647995734971, GeoMean: 0.136166; Training: 0.156286 ~ 0.015549; Test: 0.166200 ~ 0.015428; NN: 0.189529 ~ 0.013612

//old manually found generally optimal lrs.
//G2m+,5,NGi+,!lr00.01633390101,lrP0.06698813151,lrB0.005360131131, GeoMean: 0.112609; Training: 0.107573 ~ 0.005773; Test: 0.117510 ~ 0.006234; NN: 0.157420 ~ 0.008930
//Ggm+,5,NGi+,!lr00.03422167947,lrP0.05351299581,lrB5.151758465, GeoMean: 0.108734; Training: 0.099645 ~ 0.005651; Test: 0.115992 ~ 0.006615; NN: 0.156203 ~ 0.008947
//Lgm[2],5,NGi+,!lr00.008685645737,lrP0.656526238, GeoMean: 0.013450; Training: 0.013415 ~ 0.001996; Test: 0.025856 ~ 0.002333
