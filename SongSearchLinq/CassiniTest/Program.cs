using System;
using System.IO;
using System.Reflection;
using Cassini;
namespace CassiniTest
{
	class Program
	{
		static void Main(string[] args) {
#if !DEBUG
			try {
#endif
				Console.WriteLine("Usage:");
				Console.WriteLine("CassiniTest [port-number] [path-to-site]");
				string portStr;
				string argPath;
				if(args.Length != 2) {
					portStr = "32109";
					FileInfo assembly = new FileInfo(Assembly.GetExecutingAssembly().Location);
					argPath = Path.Combine(assembly.Directory.Parent.FullName, "Site");
					Console.WriteLine("None specified, choosing defaults.");
				} else {
					portStr = args[0];
					argPath = args[1];
				}
				int port = Int32.Parse(portStr);
				string sitePath = new DirectoryInfo(argPath).FullName;
				Console.WriteLine("Running " + sitePath + " on port " + port);
				Environment.CurrentDirectory = sitePath;
				Server s = new Server(port, "/", sitePath);
				s.Start();
				Console.WriteLine("Press enter to exit");
				Console.ReadLine();
				s.Stop();
#if !DEBUG
			} catch(Exception e) {
				Console.WriteLine("==========================");
				Console.WriteLine("===TERMINAL ERROR=========");
				Console.WriteLine("==========================");
				Console.WriteLine(e.GetType().Name + ": " + (e.Message ?? ""));
				Console.WriteLine("Press any key to view stack trace and abort...");
				Console.WriteLine(e.ToString());
				Console.ReadKey();
				throw;
			}
#endif
		}
	}
}