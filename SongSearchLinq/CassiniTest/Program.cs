using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Cassini;
using System.IO;
namespace CassiniTest
{
	class Program
	{
		static void Main(string[] args) {
			Console.WriteLine("Usage:");
			Console.WriteLine("CassiniTest [port-number] [path-to-site]");
			int port = Int32.Parse(args[0]);
			string sitePath = new DirectoryInfo(args[1]).FullName;
			Server s = new Server(port, "/", sitePath);
			s.Start();
			Console.WriteLine("Press enter to exit");
			Console.ReadLine();
			s.Stop();
		}
	}
}
