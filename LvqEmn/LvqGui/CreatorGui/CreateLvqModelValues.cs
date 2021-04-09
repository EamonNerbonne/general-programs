// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EmnExtensions.Wpf;
using LvqGui.CoreGui;
using LvqLibCli;

namespace LvqGui.CreatorGui
{
    public sealed class CreateLvqModelValues : HasShorthandBase, IHasSeed
    {
        readonly LvqWindowValues owner;

        [NotInShorthand]
        public LvqWindowValues Owner => owner;

        [NotInShorthand]
        public LvqDatasetCli ForDataset
        {
            get => _ForDataset;
            set {
                if (!Equals(_ForDataset, value)) {
                    _ForDataset = value;
                    _propertyChanged("ForDataset");
                    if (value != null) {
                        Dimensionality = Math.Min(Dimensionality, value.Dimensions);
                    }
                }
            }
        }

        LvqDatasetCli _ForDataset;

        [NotInShorthand]
        public double EstCost => _ForDataset == null || PrototypesPerClass <= 0
            ? double.NaN
            : settings.EstimateCost(_ForDataset.ClassCount, _ForDataset.Dimensions);

        [NotInShorthand]
        public double AnimEpochSuggestion => _ForDataset == null || PrototypesPerClass <= 0
            ? double.NaN
            : 1000.0 * 1000.0 / ((settings.EstimateCost(_ForDataset.ClassCount, _ForDataset.Dimensions) + 0.5) * _ForDataset.PointCount(0));


        LvqModelSettingsCli settings;

        public LvqModelType ModelType
        {
            get => settings.ModelType;
            set {
                if (!Equals(settings.ModelType, value)) {
                    //                    if (Dimensionality != 0 && value.IsFixedDimModel()) Dimensionality = 0;
                    settings.ModelType = value;
                    _propertyChanged("ModelType");
                }
            }
        }

        public int Dimensionality
        {
            get => settings.Dimensionality;
            set {
                if (value < 0 || ForDataset != null && value > ForDataset.Dimensions) {
                    throw new ArgumentException("Internal dimensionality must be 0 (auto) or between 1 and the dimensions of the data.");
                }

                if (settings.ModelType.IsFixedDimModel() && value != 0) {
                    throw new ArgumentException("Fixed dimension models cannot specify custom dimensionality.");
                }

                if (!Equals(settings.Dimensionality, value)) {
                    settings.Dimensionality = value;
                    _propertyChanged("Dimensionality");
                }
            }
        }

        public int PrototypesPerClass
        {
            get => settings.PrototypesPerClass;
            set {
                if (!Equals(settings.PrototypesPerClass, value)) {
                    settings.PrototypesPerClass = value;
                    _propertyChanged("PrototypesPerClass");
                }
            }
        }

        public int ParallelModels
        {
            get => settings.ParallelModels;
            set {
                if (value < 1 || value > 100) {
                    throw new ArgumentException("# of models must be in range [1,100]");
                }

                if (!settings.ParallelModels.Equals(value)) {
                    settings.ParallelModels = value;
                    _propertyChanged("ParallelModels");
                }
            }
        }

        public int FoldOffset
        {
            get => settings.FoldOffset;
            set {
                if (!settings.FoldOffset.Equals(value)) {
                    settings.FoldOffset = value;
                    _propertyChanged("FoldOffset");
                }
            }
        }

        public bool NoNnErrorRateTracking
        {
            get => settings.NoNnErrorRateTracking;
            set {
                if (!settings.NoNnErrorRateTracking.Equals(value)) {
                    settings.NoNnErrorRateTracking = value;
                    _propertyChanged("NoNnErrorRateTracking");
                }
            }
        }

        public bool neiP
        {
            get => settings.neiP;
            set {
                if (!settings.neiP.Equals(value)) {
                    settings.neiP = value;
                    _propertyChanged("neiP");
                }
            }
        }

        public bool scP
        {
            get => settings.scP;
            set {
                if (!settings.scP.Equals(value)) {
                    settings.scP = value;
                    _propertyChanged("scP");
                }
            }
        }

        public bool noKP
        {
            get => settings.noKP;
            set {
                if (!settings.noKP.Equals(value)) {
                    settings.noKP = value;
                    _propertyChanged("noKP");
                }
            }
        }

        public bool neiB
        {
            get => settings.neiB;
            set {
                if (!settings.neiB.Equals(value)) {
                    settings.neiB = value;
                    _propertyChanged("neiB");
                }
            }
        }

        public bool LocallyNormalize
        {
            get => settings.LocallyNormalize;
            set {
                if (!settings.LocallyNormalize.Equals(value)) {
                    settings.LocallyNormalize = value;
                    _propertyChanged("LocallyNormalize");
                }
            }
        }

        public bool Ppca
        {
            get => settings.Ppca;
            set {
                if (!settings.Ppca.Equals(value)) {
                    settings.Ppca = value;
                    _propertyChanged("Ppca");
                }
            }
        }

        public bool RandomInitialBorders
        {
            get => settings.RandomInitialBorders;
            set {
                if (!settings.RandomInitialBorders.Equals(value)) {
                    settings.RandomInitialBorders = value;
                    _propertyChanged("RandomInitialBorders");
                }
            }
        }

        public bool NGu
        {
            get => settings.NGu;
            set {
                if (!settings.NGu.Equals(value)) {
                    settings.NGu = value;
                    _propertyChanged("NGu");
                }
            }
        }

        public bool NGi
        {
            get => settings.NGi;
            set {
                if (!Equals(settings.NGi, value)) {
                    settings.NGi = value;
                    _propertyChanged("NGi");
                }
            }
        }

        public bool Popt
        {
            get => settings.Popt;
            set {
                if (!Equals(settings.Popt, value)) {
                    settings.Popt = value;
                    _propertyChanged("Popt");
                }
            }
        }

        public bool Bcov
        {
            get => settings.Bcov;
            set {
                if (!Equals(settings.Bcov, value)) {
                    settings.Bcov = value;
                    _propertyChanged("Bcov");
                }
            }
        }

        public bool LrRaw
        {
            get => settings.LrRaw;
            set {
                if (!Equals(settings.LrRaw, value)) {
                    settings.LrRaw = value;
                    _propertyChanged("LrRaw");
                }
            }
        }

        public bool LrPp
        {
            get => settings.LrPp;
            set {
                if (!Equals(settings.LrPp, value)) {
                    settings.LrPp = value;
                    _propertyChanged("LrPp");
                }
            }
        }

        public bool wGMu
        {
            get => settings.wGMu;
            set {
                if (!settings.wGMu.Equals(value)) {
                    settings.wGMu = value;
                    _propertyChanged("wGMu");
                }
            }
        }

        public double LrScaleP
        {
            get => settings.LrScaleP;
            set {
                if (!settings.LrScaleP.Equals(value)) {
                    settings.LrScaleP = value;
                    _propertyChanged("LrScaleP");
                }
            }
        }

        public double LrScaleB
        {
            get => settings.LrScaleB;
            set {
                if (!settings.LrScaleB.Equals(value)) {
                    settings.LrScaleB = value;
                    _propertyChanged("LrScaleB");
                }
            }
        }

        public double LR0
        {
            get => settings.LR0;
            set {
                if (!settings.LR0.Equals(value)) {
                    settings.LR0 = value;
                    _propertyChanged("LR0");
                }
            }
        }

        public double LrScaleBad
        {
            get => settings.LrScaleBad;
            set {
                if (!settings.LrScaleBad.Equals(value)) {
                    settings.LrScaleBad = value;
                    _propertyChanged("LrScaleBad");
                }
            }
        }

        public double decay
        {
            get => settings.decay;
            set {
                if (!settings.decay.Equals(value)) {
                    settings.decay = value;
                    _propertyChanged("decay");
                }
            }
        }

        public double iterScaleFactor
        {
            get => settings.iterScaleFactor;
            set {
                if (!settings.iterScaleFactor.Equals(value)) {
                    settings.iterScaleFactor = value;
                    _propertyChanged("iterScaleFactor");
                }
            }
        }

        public double MuOffset
        {
            get => settings.MuOffset;
            set {
                if (!settings.MuOffset.Equals(value)) {
                    settings.MuOffset = value;
                    _propertyChanged("MuOffset");
                }
            }
        }

        public bool SlowK
        {
            get => settings.SlowK;
            set {
                if (!settings.SlowK.Equals(value)) {
                    settings.SlowK = value;
                    _propertyChanged("SlowK");
                }
            }
        }

        public uint ParamsSeed
        {
            get => settings.ParamsSeed;
            set {
                if (!Equals(settings.ParamsSeed, value)) {
                    settings.ParamsSeed = value;
                    _propertyChanged("ParamsSeed");
                }
            }
        }

        public uint InstanceSeed
        {
            get => settings.InstanceSeed;
            set {
                if (!settings.InstanceSeed.Equals(value)) {
                    settings.InstanceSeed = value;
                    _propertyChanged("InstanceSeed");
                }
            }
        }

        static readonly Regex shR =
            new Regex(@"^([^:]*\:|\s*\\lvqseed\{)?\s*?(" +
                @"(?<ModelType>\b[A-Z][A-Za-z0-9]*)
                (\[(?<Dimensionality>[^\]]+)\]|(?<NoNnErrorRateTracking_>\+?)),
                (?<PrototypesPerClass>[0-9]+),
                (rP(?<Ppca_>\+?),)?
                (rB(?<RandomInitialBorders>\+?),)?
                (nP(?<neiP_>\+?),)?
                (nB(?<neiB_>\+?),)?
                (gn(?<LocallyNormalize_>\+?),)?
                (NG(?<NGu>\+?),)?
                (NGi(?<NGi>\+?),)?
                (Pi(?<Popt>\+?),)?
                (Bi(?<Bcov>\+?),)?
                (noB(?<wGMu>\+?),)?
                (mu(?<MuOffset>[0-9]*(\.[0-9]*)?([eE]-?[0-9]+)?),)?
                (lrX(?<LrScaleBad>[0-9]*(\.[0-9]*)?([eE]-?[0-9]+)?),)?
                (?<SlowK>\!?)
                (lr0(?<LR0>[0-9]*(\.[0-9]*)?([eE]-?[0-9]+)?),)?
                (lrP(?<LrScaleP>[0-9]*(\.[0-9]*)?([eE]-?[0-9]+)?),)?
                (lrB(?<LrScaleB>[0-9]*(\.[0-9]*)?([eE]-?[0-9]+)?),)?
                (\[(?<ParamsSeed_>[0-9a-fA-F]+)?\,(?<InstanceSeed_>[0-9a-fA-F]+)?\])?(\^(?<ParallelModels>[0-9]+))?(_(?<FoldOffset>[0-9]+))?\,?
            "
                + @"
                |
                (?<ModelType>\b[A-Z][A-Za-z0-9]*)(\[(?<Dimensionality>[^\]]+)\])?
                -(?<PrototypesPerClass>[0-9]+),
                (
                    (?<Ppca>Ppca,)
                    |(?<RandomInitialBorders>RandomInitialBorders,)
                    |(?<neiP>neiP,)
                    |(?<scP>scP,)
                    |(?<noKP>noKP,)
                    |(?<neiB>neiB,)
                    |(?<LocallyNormalize>LocallyNormalize,)
                    |(?<NGu>NGu,)
                    |(?<NGi>NGi,)
                    |(?<Popt>Popt,)
                    |(?<Bcov>Bcov,)
                    |(?<wGMu>wGMu,)
                    |(?<LrRaw>LrRaw,)
                    |(?<LrPp>LrPp,)
                    |(?<SlowK>SlowK,)
                    |(?<NoNnErrorRateTracking>NoNnErrorRateTracking,)
                    |mu(?<MuOffset>[0-9]*(\.[0-9]*)?([eE]-?[0-9]+)?),
                    |d(?<decay>[0-9]*(\.[0-9]*)?([eE]-?[0-9]+)?),
                    |is(?<iterScaleFactor>[0-9]*(\.[0-9]*)?([eE]-?[0-9]+)?),
                    |lrX(?<LrScaleBad>[0-9]*(\.[0-9]*)?([eE]-?[0-9]+)?),
                    |lr(?<LR0>[0-9]*(\.[0-9]*)?([eE]-?[0-9]+)?),
                    |lrP(?<LrScaleP>[0-9]*(\.[0-9]*)?([eE]-?[0-9]+)?),
                    |lrB(?<LrScaleB>[0-9]*(\.[0-9]*)?([eE]-?[0-9]+)?),
                )*
                (\[(?<ParamsSeed_>[0-9a-fA-F]+)?\,(?<InstanceSeed_>[0-9a-fA-F]+)?\])?(\^(?<ParallelModels>[0-9]+))?(_(?<FoldOffset>[0-9]+))?\,?
            "
                + @")(--.*|\}\{[^\}]*\})?\s*$"
                ,
                RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace
            );

        public override string Shorthand
        {
            get => settings.ToShorthand()
                + (ForDataset == null ? "" : "--" + ForDataset.DatasetLabel);
            set {
                settings = ParseShorthand(value);
                foreach (var group in shR.GetGroupNames()) {
                    if (!string.IsNullOrEmpty(group)) {
                        _propertyChanged(group.TrimEnd('_'));
                    }
                }
            }
        }

        public override string ShorthandErrors => ShorthandHelper.VerifyShorthand(this, shR);

        public static LvqModelSettingsCli ParseShorthand(string shorthand)
        {
            var maybeParsed = ShorthandHelper.TryParseShorthand(default(LvqModelSettingsCli), shR, shorthand);
            if (maybeParsed.HasValue) {
                return maybeParsed.Value;
            }

            throw new ArgumentException("Can't parse: " + shorthand);
        }

        public static LvqModelSettingsCli? TryParseShorthand(string shorthand) => ShorthandHelper.TryParseShorthand(default(LvqModelSettingsCli), shR, shorthand).AsNullableStruct<LvqModelSettingsCli>();

        public CreateLvqModelValues(LvqWindowValues owner)
        {
            this.owner = owner;
            settings = new LvqModelSettingsCli();
            //this.ReseedBoth();
        }

        public Task ConfirmCreation() => CreateSingleModel(owner, ForDataset, settings.Canonicalize());

        static Task CreateSingleModel(LvqWindowValues owner, LvqDatasetCli dataset, LvqModelSettingsCli settingsCopy)
        {
            var whenDone = new TaskCompletionSource<object>();
            Task.Factory
                .StartNew(() => {
                        if (settingsCopy.LR0 == 0.0 && settingsCopy.LrScaleP == 0.0 && settingsCopy.LrScaleB == 0.0) {
                            settingsCopy = LrGuesser.ChooseReasonableLr(settingsCopy);
                        }

                        if (settingsCopy.LR0 == 0.0) {
                            Console.WriteLine("Cannot create model with 0 LR!");
                        } else {
                            var newModel = new LvqMultiModel(dataset, settingsCopy);
                            Console.WriteLine("Created: " + newModel.ModelLabel);
                            owner.Dispatcher.BeginInvoke(owner.LvqModels.Add, newModel).Completed += (s, e) => whenDone.SetResult(null);
                        }
                    }
                );
            return whenDone.Task;
        }


        static readonly string[] depProps = {
            "HasOptimizedLr", "OptimizeButtonText", "OptimizedLrAllIncomplete", "OptimizedLrAllStatus",
            "OptimizeAllButtonText", "EstCost", "AnimEpochSuggestion"
        };

        protected override IEnumerable<string> GloballyDependantProps => base.GloballyDependantProps.Concat(depProps);
    }

    static class LvqModelTypeHelpers
    {
        public static bool IsFixedDimModel(this LvqModelType modelType) => LvqModelSettingsCli.IsFixedDimensionalityModel(modelType);
    }
}
