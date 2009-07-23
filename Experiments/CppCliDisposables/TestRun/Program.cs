using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MyDisposables;

namespace TestRun
{
	class DispA : IDisposable
	{
		string label;
		public DispA(string label)
		{
			this.label = label;
			Console.WriteLine("Constructing({0})", label);
		}
		public void Dispose()
		{
			Console.WriteLine("Dispose({0})", label);
		}
	}

	static class Program
	{
		static void Main(string[] args)
		{
			DispA test = new DispA("test");
			MyDisposableContainer a = new MyDisposableContainer(new DispA("Xa"), new DispA("Ya"));
			using (MyDisposableContainer b = new MyDisposableContainer(new DispA("Xb"), new DispA("Yb")))
				Console.WriteLine("inside using");

			Console.WriteLine("last line of Main()");
		}
	}
}
