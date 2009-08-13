using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceProcess;

namespace IISrestart
{
	class Program
	{
		static void Main(string[] args)
		{
		}
		static void RestartIIS()
		{
			ServiceController w3svc = ServiceController.GetServices().First(service => service.ServiceName.ToUpperInvariant() == "W3SVC");
			Console.Write("Stopping");
			w3svc.Stop();
			Console.WriteLine("...");
			w3svc.WaitForStatus(ServiceControllerStatus.Stopped);
			Console.Write("Restarting");
			w3svc.Start();
			Console.WriteLine("...");
			w3svc.WaitForStatus(ServiceControllerStatus.Running);
			Console.WriteLine("Restarted.");
		}
	}
}
