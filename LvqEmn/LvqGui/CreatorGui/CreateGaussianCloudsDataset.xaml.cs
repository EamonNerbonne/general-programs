using System.Threading;
using System.Windows;

namespace LvqGui
{
    public sealed partial class CreateGaussianCloudDataset
    {
        public CreateGaussianCloudDataset() => InitializeComponent();

        void ReseedParam(object sender, RoutedEventArgs e) => ((IHasSeed)DataContext).ReseedParam();
        void ReseedInst(object sender, RoutedEventArgs e) => ((IHasSeed)DataContext).ReseedInst();

        void CreateDatasetButtonPress(object sender, RoutedEventArgs e) => ThreadPool.QueueUserWorkItem(o => ((CreateGaussianCloudsDatasetValues)o).ConfirmCreation(), DataContext);
    }
}
