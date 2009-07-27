using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ThreadDispose
{
	class MyHelper : IDisposable
	{
		static int valHelp = 0;
		public int value = valHelp++;
		public MyHelper() {
			Console.WriteLine("Starting: "+Thread.CurrentThread.ManagedThreadId);
		}
		public void Dispose() {
			Console.WriteLine("Disposing: " + Thread.CurrentThread.ManagedThreadId);
		}
	}

	static class Program
	{
		[ThreadStatic] 
		static MyHelper helper;
		static void Main(string[] args) {
			helper = new MyHelper();
			new Thread(() => {
				helper = new MyHelper();
			}) .Start();
			AppDomain.CurrentDomain.DomainUnload += new EventHandler(CurrentDomain_DomainUnload);
			AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_DomainUnload);
			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException); 
			throw new Exception("whoops");
			
		}

		static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e) {
			Console.WriteLine("Dieing: " + Thread.CurrentThread.ManagedThreadId);
		}

		static void CurrentDomain_DomainUnload(object sender, EventArgs e) {
			Console.WriteLine("Disposing: " + Thread.CurrentThread.ManagedThreadId);
		}
	}
}
