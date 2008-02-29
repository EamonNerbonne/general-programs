using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.WebServer;
using System.Net;
using System.IO;

namespace XspTest
{
	class Program
	{
		static void Main(string[] args) {
			WebSource webSource;
			ApplicationServer appServer;
			int port = Int32.Parse(args[0]);
			string sitePath = new DirectoryInfo(args[1]).FullName;
			if(!Directory.Exists(sitePath))throw new DirectoryNotFoundException("Site directory "+sitePath+"not found.");
			Console.WriteLine("Site Path: "+sitePath);
			webSource = new XSPWebSource(IPAddress.Any, port);
			Environment.CurrentDirectory = sitePath;
			appServer = new ApplicationServer(webSource);
			appServer.Verbose = true;
			appServer.AddApplication(null, port, "/", sitePath);
			appServer.Start(true);
			Console.WriteLine("Hit Enter to stop the webserver.");
			Console.ReadLine();
			appServer.Stop();
		}
	}
}
