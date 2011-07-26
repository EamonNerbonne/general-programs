using LvqLibCli;

namespace LvqGui {
	public interface IDatasetCreator { LvqDatasetCli CreateDataset();}

	public static class CreateDataset {
		public static LvqDatasetCli CreateFromShorthand(string shorthand) {
			var factory = StarSettings.TryParse(shorthand) ?? GaussianCloudSettings.TryParse(shorthand) ?? (IDatasetCreator)LoadDatasetImpl.TryParse(shorthand);
			return factory.CreateDataset();
		}
	}
}
