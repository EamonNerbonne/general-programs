using System.Linq;
using System.Windows;

namespace LvqGui
{
	public partial class App
	{
		void Application_Startup(object sender, StartupEventArgs e) {
			new LvqWindow().Show();
		}
	}
}
