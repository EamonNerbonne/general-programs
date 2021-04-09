using System.Threading;
using System.Windows;

namespace LvqGui
{
    public sealed partial class CreateStarDataset
    {
        public CreateStarDataset() => InitializeComponent();

        void ReseedParam(object sender, RoutedEventArgs e) => ((IHasSeed)DataContext).ReseedParam();
        void ReseedInst(object sender, RoutedEventArgs e) => ((IHasSeed)DataContext).ReseedInst();

        void buttonGenerateDataset_Click(object sender, RoutedEventArgs e) => ThreadPool.QueueUserWorkItem(o => ((CreateStarDatasetValues)o).ConfirmCreation(), DataContext);
    }
}
