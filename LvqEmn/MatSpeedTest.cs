//#define PRINTRESULT
//#define DO_CS_VARIANTS
#define DO_NATIVE_CPP
//#define DO_CLI_CPP
#define DO_dnA
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmnExtensions.MathHelpers;
using EmnExtensions.DebugTools;
using dnAnalytics.LinearAlgebra;
using System.Threading;

namespace LVQeamon
{
	static class MatSpeedTest
	{
		public static void Test() {
			const int iters = 500000;
			const int dims = 20;
			const int parallelFactor = 8;
			//QMatrixSquare a1 = new double[,] { { 1.0, 2.0, 3.0 }, { 4.0, 5.0, 6.0 }, { 7.0, 8.0, 9.0 }, };
			//QMatrixSquare b1 = new double[,] { { 0.0, 0.0, 1.0 }, { 1.0, 0.0, 0.0 }, { 0.0, 1.0, 0.0 }, };
			QMatrixSquare a1 = QMatrixSquare.Factory.NewIdentityMatrix(dims);
			QMatrixSquare b1 = QMatrixSquare.Factory.NewIdentityMatrix(dims);
#if DO_CS_VARIANTS
#if PRINTRESULT
			Console.WriteLine(a1.StringRep());
#endif
			NiceTimer.Time("MultCM", () => {
				var a = QMatrixCM.Factory.InitializeFrom(a1);
				var b = QMatrixRM.Factory.InitializeFrom(b1);
				for (int i = 0; i < iters; i++)
					a = QMatrixHelper.MultCM(b, a);
#if PRINTRESULT
				Console.WriteLine(a.StringRep());
#endif
			});
			NiceTimer.Time("Mult(CM)", () => {
				var a = QMatrixCM.Factory.InitializeFrom(a1);
				var b = QMatrixRM.Factory.InitializeFrom(b1);
				for (int i = 0; i < iters; i++)
					a = QMatrixHelper.Mult(b, a, a);
#if PRINTRESULT
				Console.WriteLine(a.StringRep());
#endif
			});

			NiceTimer.Time("MultRM", () => {
				var a = QMatrixRM.Factory.InitializeFrom(a1);
				var b = QMatrixRM.Factory.InitializeFrom(b1);
				for (int i = 0; i < iters; i++)
					a = QMatrixHelper.MultRM(b, a);
#if PRINTRESULT
				Console.WriteLine(a.StringRep());
#endif
			});
			NiceTimer.Time("Mult(RM)", () => {
				var a = QMatrixRM.Factory.InitializeFrom(a1);
				var b = QMatrixRM.Factory.InitializeFrom(b1);
				for (int i = 0; i < iters; i++)
					a = QMatrixHelper.Mult(b, a, a);
#if PRINTRESULT
				Console.WriteLine(a.StringRep());
#endif
			});
#endif
#if DO_dnA
			DenseMatrix matA = new DenseMatrix(dims, dims);
			DenseMatrix matB = new DenseMatrix(dims, dims);
			for (int i = 0; i < dims; i++)
				for (int j = 0; j < dims; j++)
					matA[i, j] = matB[i, j] = RndHelper.ThreadLocalRandom.NextNormal();
			ParallelTime("Mult(dnA)", parallelFactor, () => {
				var a = new DenseMatrix(matA);
				var b = new DenseMatrix(matB);
				for (int i = 0; i < iters; i++)
					a = b * a;
#if PRINTRESULT
				Console.WriteLine(a.StringRep());
#endif
			});

#endif

#if DO_CLI_CPP
			ParallelTime("Mult(uBlas)", parallelFactor, () => {				LVQCppCli.Class1.Testublas(iters, dims);			});
#endif
#if DO_NATIVE_CPP
			ParallelTime("ublasNative", parallelFactor, () => { LVQCppCli.Class1.TestublasNative(iters, dims); });
#endif
		}

		static void ParallelTime(string msg, int parallelFactor, Action func) {
			NiceTimer.Time(msg, () => {
				using (Semaphore done = new Semaphore(0, parallelFactor)) {
					for (int cnt = 0; cnt < parallelFactor; cnt++)
						new Thread(() => {
							func();
							done.Release();
						}).Start();
					for (int cnt = 0; cnt < parallelFactor; cnt++)
						done.WaitOne();
				}
			});
		}
	}
}
