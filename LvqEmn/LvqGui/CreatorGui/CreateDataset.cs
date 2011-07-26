using LvqLibCli;

namespace LvqGui {
	public interface IDatasetCreator { LvqDatasetCli CreateDataset(); void IncInstanceSeed(); }

	public static class CreateDataset {
		public static IDatasetCreator CreateFactory(string shorthand) {
			return StarSettings.TryParse(shorthand) ?? GaussianCloudSettings.TryParse(shorthand) ?? (IDatasetCreator)LoadDatasetImpl.TryParse(shorthand);
		}
		public static LvqDatasetCli CreateFromShorthand(string shorthand) {
			var factory = StarSettings.TryParse(shorthand) ?? GaussianCloudSettings.TryParse(shorthand) ?? (IDatasetCreator)LoadDatasetImpl.TryParse(shorthand);
			return factory.CreateDataset();
		}
	}
}
