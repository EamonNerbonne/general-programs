using System.Windows;
using LvqGui.CoreGui;

namespace LvqGui
{
    public sealed partial class App
    {
        void Application_Startup(object sender, StartupEventArgs e) => new LvqWindow().Show();
    }
}
