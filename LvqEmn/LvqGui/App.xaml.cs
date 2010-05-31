using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Windows;
using LvqGui;

namespace LVQeamon
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		private void Application_Startup(object sender, StartupEventArgs e) {
			//var win2 = new Window2();
			//win2.Show();
			new LvqWindow().Show();
		}
	}
}
