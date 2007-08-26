using System;
using System.Collections.Generic;
using System.Text;

namespace TackOnTest
{
	public enum BasicTexts { Hello, Goobye, Submit, Cancel }

	public interface ITackOn<T> { } //marker


	public class SimpleClass : ITackOn<BasicTexts>
	{
		int n = 0;
		public void Publish(object o) {
			Console.WriteLine(GetString(o));
		}

		public string GetString(object o) {
			return n++.ToString() + ": " + o.GetType().FullName + " " + o.ToString();
		}
	}

	public static class Util
	{
		public static void Resolve<TO, T>(TO o, T t) where TO : ITackOn<T> {
			Console.WriteLine("Whee: " + o.GetType() + " " + t.GetType());
		}

		public static Indexable<T> Resolve2<T>(ITackOn<T> o) {
			return new Indexable<T>();
		}
		public static string Resolve3<T>(ITackOn<T> o,T t) {
			return t.ToString();

		}
	}

	public struct Indexable<T>
	{
		public string this[T index] {
			get {
				return index.ToString();
			}
		}
	}

	class Program
	{
		static void Main(string[] args) {
			SimpleClass webmodule = new SimpleClass();
			webmodule.Publish(3);
			webmodule.Publish("3");
			webmodule.Publish('3');
			webmodule.Publish((short)3);
			Util.Resolve(webmodule, BasicTexts.Hello);
			string retval = Util.Resolve2(webmodule)[BasicTexts.Goobye];
			Util.Resolve3(webmodule, BasicTexts.Hello);
			Util.Resolve3(webmodule,
		}
	}
}
