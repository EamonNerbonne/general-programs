//#define USECPP
#define DOSLOWTESTS

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmnExtensions;
using EmnExtensions.MathHelpers;
using System.Diagnostics;
using System.Collections.ObjectModel;
using EmnExtensions.DebugTools;

namespace CollectionSpeedTest
{
	static class Program
	{
		public const int ITER = 10000000;
		public const int SIZE = 100;
		static Random r = new Random();
		static double[] vecA, vecB;
		static void Main(string[] args)
		{
			vecA = F.AsEnumerable(() => r.NextNorm()).Take(SIZE).ToArray();
			vecB = F.AsEnumerable(() => r.NextNorm()).Take(SIZE).ToArray();
			DoTest();
		}
		public static double DotWithLength(this double[] vecA, double[] vecB)
		{
			double sum = 0.0;
			for (int i = 0; i < vecA.Length; i++)
				sum += vecA[i] * vecB[i];
			return sum;
		}
		public static double Dot(this double[] vecA, double[] vecB)
		{
			double sum = 0.0;
			for (int i = 0; i < SIZE; i++)
				sum += vecA[i] * vecB[i];
			return sum;
		}
		public static double TestDotLengthParam(double[] vecA, double[] vecB)
		{
			double sumO = 0.0;
			for (int i = 0; i < ITER; i++)
			{
				double sum = 0.0;
				for (int j = 0; j < vecA.Length; j++)
					sum += vecA[j] * vecB[j];
				sumO += sum;
			}
			return sumO;
		}
		public static double TestDotLengthMember()
		{
			double sumO = 0.0;
			for (int i = 0; i < ITER; i++)
			{
				double sum = 0.0;
				for (int j = 0; j < vecA.Length; j++)
					sum += vecA[j] * vecB[j];
				sumO += sum;
			}
			return sumO;
		}
		public static double TestDotParam(double[] vecA, double[] vecB)
		{
			double sumO = 0.0;
			for (int i = 0; i < ITER; i++)
			{
				double sum = 0.0;
				for (int j = 0; j < SIZE; j++)
					sum += vecA[j] * vecB[j];
				sumO += sum;
			}
			return sumO;
		}
		public static double TestDotMember()
		{
			double sumO = 0.0;
			for (int i = 0; i < ITER; i++)
			{
				double sum = 0.0;
				for (int j = 0; j < SIZE; j++)
					sum += vecA[j] * vecB[j];
				sumO += sum;
			}
			return sumO;
		}

		public static double TestDotSubroutineParam(double[] vecA, double[] vecB)
		{
			double sumO = 0.0;
			for (int i = 0; i < ITER; i++)
			{
				double sum = vecA.Dot(vecB);
				sumO += sum;
			}
			return sumO;
		}
		public static double TestDotSubroutineLengthParam(double[] vecA, double[] vecB)
		{
			double sumO = 0.0;
			for (int i = 0; i < ITER; i++)
			{
				double sum = vecA.DotWithLength(vecB);
				sumO += sum;
			}
			return sumO;
		}

		static void DoTest()
		{
			var vecAl = vecA.ToList();
			var vecBl = vecB.ToList();
			IList<double> vecAi = vecA;
			IList<double> vecBi = vecB;
			IList<double> vecAi2 = vecAl;
			IList<double> vecBi2 = vecBl;
			var vecAlr = vecAl.AsReadOnly();
			var vecBlr = vecBl.AsReadOnly();
			var vecAr = new ReadOnlyCollection<double>(vecA);
			var vecBr = new ReadOnlyCollection<double>(vecB);
			Console.WriteLine("word-size:" + System.Runtime.InteropServices.Marshal.SizeOf(typeof(IntPtr)) * 8);
			for (int trials = 0; trials < 2; trials++)
			{
				Console.WriteLine("\n\n\n\n\nTest-run #{0}:", trials);
#if USECPP
                NiceTimer.Time("C++/CLI .Dot " + ITER + "x" + SIZE, () => {
                    Console.WriteLine(CollectionSpeedTestCpp.TestWithDotProd(vecA, vecB));
                });
                NiceTimer.Time("C++/CLI v2 .Dot " + ITER + "x" + SIZE, () => {
                    Console.WriteLine(CollectionSpeedTestCpp.TestWithDotProdExt2(vecA, vecB));
                });
                NiceTimer.Time("C++/native .Dot " + ITER + "x" + SIZE, () => {
                    Console.WriteLine(CollectionSpeedTestCpp.TestWithDotProdExt(vecA, vecB));
                });
#endif
				NiceTimer.Time("FOR (param) dot " + ITER + "x" + SIZE, () =>
				{
					double sumO = TestDotParam(vecA, vecB);
					Console.WriteLine(sumO);
				});
				NiceTimer.Time("FOR (variable) dot " + ITER + "x" + SIZE, () =>
				{
					double sumO = TestDotMember();
					Console.WriteLine(sumO);
				});
				NiceTimer.Time("FOR using Length(param) dot " + ITER + "x" + SIZE, () =>
				{
					double sumO = TestDotLengthParam(vecA, vecB);
					Console.WriteLine(sumO);
				});
				NiceTimer.Time("FOR using Length(variable) dot " + ITER + "x" + SIZE, () =>
				{
					double sumO = TestDotLengthMember();
					Console.WriteLine(sumO);
				});
				NiceTimer.Time(".Dot " + ITER + "x" + SIZE, () =>
				{
					double sumO = TestDotSubroutineParam(vecA, vecB);
					Console.WriteLine(sumO);
				});
				NiceTimer.Time(".Dot using Length" + ITER + "x" + SIZE, () =>
				{
					double sumO = TestDotSubroutineLengthParam(vecA, vecB);
					Console.WriteLine(sumO);
				});
#if DOSLOWTESTS
				NiceTimer.Time("FOR LIST dot " + ITER + "x" + SIZE, () =>
				{
					double sumO = 0.0;

					for (int i = 0; i < ITER; i++)
					{
						double sum = 0.0;

						for (int j = 0; j < SIZE; j++)
							sum += vecAl[j] * vecBl[j];
						sumO += sum;
					}
					Console.WriteLine(sumO);
				});
				NiceTimer.Time("FOR ILIST ARRAY dot " + ITER + "x" + SIZE, () =>
				{
					double sumO = 0.0;

					for (int i = 0; i < ITER; i++)
					{
						double sum = 0.0;

						for (int j = 0; j < SIZE; j++)
							sum += vecAi[j] * vecBi[j];
						sumO += sum;
					}
					Console.WriteLine(sumO);
				});
				NiceTimer.Time("FOR ILIST LIST dot " + ITER + "x" + SIZE, () =>
				{
					double sumO = 0.0;

					for (int i = 0; i < ITER; i++)
					{
						double sum = 0.0;

						for (int j = 0; j < SIZE; j++)
							sum += vecAi2[j] * vecBi2[j];
						sumO += sum;
					}
					Console.WriteLine(sumO);
				});
				NiceTimer.Time("FOR ReadOnly LIST dot " + ITER + "x" + SIZE, () =>
				{
					double sumO = 0.0;

					for (int i = 0; i < ITER; i++)
					{
						double sum = 0.0;

						for (int j = 0; j < SIZE; j++)
							sum += vecAlr[j] * vecBlr[j];
						sumO += sum;
					}
					Console.WriteLine(sumO);
				});
				NiceTimer.Time("FOR ReadOnly array dot " + ITER + "x" + SIZE, () =>
				{
					double sumO = 0.0;

					for (int i = 0; i < ITER; i++)
					{
						double sum = 0.0;

						for (int j = 0; j < SIZE; j++)
							sum += vecAr[j] * vecBr[j];
						sumO += sum;
					}
					Console.WriteLine(sumO);
				});
#endif
			}




			/*
			NiceTimer.Time("Enum dot " + ITER + "x" + SIZE, () => {
				for (int i = 0; i < ITER; i++) {
					double sum = 0.0;
					var eA = vecA.AsEnumerable().GetEnumerator();
					var eB = vecB.AsEnumerable().GetEnumerator();
					while(eA.MoveNext() && eB.MoveNext()) {
						sum += eA.Current*eB.Current;
					}
				}
			});
			NiceTimer.Time("Enum' dot " + ITER + "x" + SIZE, () => {
				for (int i = 0; i < ITER; i++) {
					double sum = 0.0;
					var eA = ((IEnumerable<double>) vecA).GetEnumerator();
					var eB = ((IEnumerable<double>)vecB).GetEnumerator();
					while (eA.MoveNext() && eB.MoveNext()) {
						sum += eA.Current * eB.Current;
					}
				}
			});
			NiceTimer.Time("FOREACH in RANGE dot " + ITER + "x" + SIZE, () => {
				foreach (int i in Enumerable.Range(0,ITER)) {
					double sum = 0.0;
					for (int j = 0; j < SIZE; j++)
						sum += vecA[j] * vecB[j];
				}
			});
			NiceTimer.Time("FOREACH in RANGE,2 dot " + ITER + "x" + SIZE, () => {
				for (int i = 0; i < ITER; i++) {
					double sum = 0.0;
					foreach (int j in Enumerable.Range(0, SIZE))
						sum += vecA[j] * vecB[j];
				}
			});
			NiceTimer.Time("FOREACH in RANGE*2 dot " + ITER + "x" + SIZE, () => {
				foreach (int i in Enumerable.Range(0, ITER)) {
					double sum = 0.0;
					foreach (int j in Enumerable.Range(0,SIZE))
						sum += vecA[j] * vecB[j];
				}
			});*/
		}
	}
}
