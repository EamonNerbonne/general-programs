using System.Collections.Generic;
using System.Text.RegularExpressions;
using LvqLibCli;

namespace LvqGui
{
    public interface IDatasetCreator
    {
        LvqDatasetCli CreateDataset();
        uint InstanceSeed { get; set; }
        bool ExtendDataByCorrelation { get; set; }
        bool NormalizeDimensions { get; set; }
        bool NormalizeByScaling { get; set; }
        int Folds { get; set; }
        string Shorthand { get; }
        IDatasetCreator Clone();
    }
    public abstract class DatasetCreatorBase<T> : CloneableAs<T>, IDatasetCreator, IHasShorthand where T : DatasetCreatorBase<T>, new()
    {
        public uint InstanceSeed { get; set; }
        public bool ExtendDataByCorrelation { get; set; }
        public bool NormalizeDimensions { get; set; }
        public bool NormalizeByScaling { get; set; }
        int _Folds = 10;
        public int Folds { get => _Folds; set => _Folds = value; }
        IDatasetCreator IDatasetCreator.Clone() => Clone();
        protected abstract string RegexText { get; }
        protected abstract string GetShorthand();
        protected static readonly T defaults = new T();

        public static readonly Regex shR = new Regex(defaults.RegexText, RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace);

        public string Shorthand { get => GetShorthand(); set => ShorthandHelper.ParseShorthand(this, defaults, shR, value); }
        public string ShorthandErrors => ShorthandHelper.VerifyShorthand(this, shR);
        public abstract LvqDatasetCli CreateDataset();
        public static T ParseSettings(string shorthand) => new T { Shorthand = shorthand };
        public static T TryParse(string shorthand) => ShorthandHelper.TryParseShorthand(defaults, shR, shorthand).AsNullable();
    }

    public static class CreateDataset
    {
        public static void IncInstanceSeed(this IDatasetCreator obj) => obj.InstanceSeed++;
        public static IDatasetCreator BaseClone(this IDatasetCreator obj)
        {
            obj = obj.Clone();
            obj.ExtendDataByCorrelation = false;
            obj.NormalizeDimensions = false;
            obj.NormalizeByScaling = false;
            obj.InstanceSeed = 0;
            return obj;
        }

        public static IDatasetCreator CreateFactory(string shorthand) => StarDatasetSettings.TryParse(shorthand) ?? GaussianCloudDatasetSettings.TryParse(shorthand) ?? (IDatasetCreator)LoadedDatasetSettings.TryParse(shorthand);

        public static IEnumerable<IDatasetCreator> StandardDatasets()
        {
            yield return CreateFactory(@"page-blocks.data-10D-5,5473");
            yield return CreateFactory(@"colorado.data-6D-14,28000");
            yield return CreateFactory(@"star-8D-9x10000,3(5Dr)x10i0.8n7g5[a9cd2154,1]");
            yield return CreateFactory(@"pendigits.combined.data-16D-10,10992");
            yield return CreateFactory(@"segmentation.data-19D-7,2100");
            //yield return CreateFactory(@"nrm-24D-3x30000,1[5122ea19,]");
            //yield return CreateFactory(@"optdigits.combined.data-64D-10,5620");
            yield return CreateFactory(@"letter-recognition.data-16Dn-26,20000");
        }

        public static IEnumerable<IDatasetCreator> StandardAndNormalizedDatasets()
        {
            foreach (var factory in StandardDatasets()) {
                var normalized = factory.Clone();
                normalized.NormalizeDimensions = true;
                yield return factory;
                yield return normalized;
            }
        }


        public static string LrTrainingShorthand(this IDatasetCreator obj)
        {
            obj = obj.Clone();
            obj.InstanceSeed = 0;
            obj.Folds = 10;
            if (obj is LoadedDatasetSettings) {
                ((LoadedDatasetSettings)obj).TestFilename = null;
            }

            return obj.Shorthand;
        }
        public static bool HasTestfile(this IDatasetCreator obj) => obj is LoadedDatasetSettings && ((LoadedDatasetSettings)obj).TestFilename != null;


        public static LvqDatasetCli CreateFromShorthand(string shorthand)
        {
            var factory = StarDatasetSettings.TryParse(shorthand) ?? GaussianCloudDatasetSettings.TryParse(shorthand) ?? (IDatasetCreator)LoadedDatasetSettings.TryParse(shorthand);
            return factory.CreateDataset();
        }
    }
}
