using System.Windows;

namespace LvqGui
{
    public partial class App : Application
    {
        void Application_Startup(object sender, StartupEventArgs e) => new LvqWindow().Show();
    }
}
