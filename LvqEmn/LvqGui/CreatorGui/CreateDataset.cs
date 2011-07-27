using LvqLibCli;

namespace LvqGui {
	public interface IDatasetCreator { 
		LvqDatasetCli CreateDataset(); 
		uint InstanceSeed { get; set; }
		bool ExtendDataByCorrelation { get; set; }
		bool NormalizeDimensions { get; set; }
		string Shorthand { get; }
		IDatasetCreator Clone();
	}

	public static class CreateDataset {
		public static void IncInstanceSeed(this IDatasetCreator obj) { obj.InstanceSeed++; }
		public static string BaseShorthand(this IDatasetCreator obj) {
			obj = obj.Clone();
			obj.ExtendDataByCorrelation = false;
			obj.NormalizeDimensions = false;
			obj.InstanceSeed = 0;
			return obj.Shorthand;
		}
		public static IDatasetCreator CreateFactory(string shorthand) {
			return StarSettings.TryParse(shorthand) ?? GaussianCloudSettings.TryParse(shorthand) ?? (IDatasetCreator)LoadedDatasetSettings.TryParse(shorthand);
		}

		public static LvqDatasetCli CreateFromShorthand(string shorthand) {
			var factory = StarSettings.TryParse(shorthand) ?? GaussianCloudSettings.TryParse(shorthand) ?? (IDatasetCreator)LoadedDatasetSettings.TryParse(shorthand);
			return factory.CreateDataset();
		}
	}
}
