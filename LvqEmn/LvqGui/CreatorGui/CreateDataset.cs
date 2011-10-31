using System.Text.RegularExpressions;
using LvqLibCli;

namespace LvqGui {
	public interface IDatasetCreator {
		LvqDatasetCli CreateDataset();
		uint InstanceSeed { get; set; }
		bool ExtendDataByCorrelation { get; set; }
		bool NormalizeDimensions { get; set; }
		bool NormalizeByScaling { get; set; }
		int Folds { get; set; }
		string Shorthand { get; }
		IDatasetCreator Clone();
	}
	public abstract class DatasetCreatorBase<T> : CloneableAs<T>, IDatasetCreator, IHasShorthand where T : DatasetCreatorBase<T>, new() {
		public uint InstanceSeed { get; set; }
		public bool ExtendDataByCorrelation { get; set; }
		public bool NormalizeDimensions { get; set; }
		public bool NormalizeByScaling { get; set; }
		int _Folds = 10;
		public int Folds { get { return _Folds; } set { _Folds = value; } }
		IDatasetCreator IDatasetCreator.Clone() { return Clone(); }
		protected abstract string RegexText { get; }
		protected abstract string GetShorthand();
		protected static readonly T defaults = new T();
		
		public static readonly Regex shR = new Regex(defaults.RegexText, RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace);

		public string Shorthand { get { return GetShorthand(); } set { ShorthandHelper.ParseShorthand(this, defaults, shR, value); } }
		public string ShorthandErrors { get { return ShorthandHelper.VerifyShorthand(this, shR); } }
		public abstract LvqDatasetCli CreateDataset();
		public static T ParseSettings(string shorthand) { return new T { Shorthand = shorthand }; }
		public static T TryParse(string shorthand) { return ShorthandHelper.TryParseShorthand(defaults, shR, shorthand).AsNullable(); }
	}

	public static class CreateDataset {
		public static void IncInstanceSeed(this IDatasetCreator obj) { obj.InstanceSeed++; }
		public static string BaseShorthand(this IDatasetCreator obj) {
			obj = obj.Clone();
			obj.ExtendDataByCorrelation = false;
			obj.NormalizeDimensions = false;
			obj.NormalizeByScaling = true;
			obj.InstanceSeed = 0;
			return obj.Shorthand;
		}

		public static IDatasetCreator CreateFactory(string shorthand) {
			return StarSettings.TryParse(shorthand) ?? GaussianCloudSettings.TryParse(shorthand) ?? (IDatasetCreator)LoadedDatasetSettings.TryParse(shorthand);
		}

		public static string LrTrainingShorthand(this IDatasetCreator obj) {
			obj = obj.Clone();
			obj.InstanceSeed = 0;
			obj.Folds = 10;
			if (obj is LoadedDatasetSettings)
				((LoadedDatasetSettings)obj).TestFilename = null;
			return obj.Shorthand;
		}
		public static bool HasTestfile(this IDatasetCreator obj) {
			return obj is LoadedDatasetSettings && ((LoadedDatasetSettings)obj).TestFilename != null;
		}


		public static LvqDatasetCli CreateFromShorthand(string shorthand) {
			var factory = StarSettings.TryParse(shorthand) ?? GaussianCloudSettings.TryParse(shorthand) ?? (IDatasetCreator)LoadedDatasetSettings.TryParse(shorthand);
			return factory.CreateDataset();
		}
	}
}
