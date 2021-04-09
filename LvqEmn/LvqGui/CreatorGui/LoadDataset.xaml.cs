using System.Threading;
using System.Windows;

namespace LvqGui
{
    public partial class LoadDataset
    {
        public LoadDataset() { InitializeComponent(); }
        void ReseedInst(object sender, RoutedEventArgs e) => ((IHasSeed)DataContext).ReseedInst();

        void ConfirmLoadDataset(object sender, RoutedEventArgs e) => ThreadPool.QueueUserWorkItem(o => ((LoadDatasetValues)o).ConfirmCreation(), DataContext);
    }
}
