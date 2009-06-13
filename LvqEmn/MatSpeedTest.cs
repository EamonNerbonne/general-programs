//#define PRINTRESULT
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmnExtensions.MathHelpers;
using EmnExtensions.DebugTools;

namespace LVQeamon
{
	static class MatSpeedTest
	{
		public static void Test() {
			const int iters = 50;
			const int dims = 200;
			//QMatrixSquare a1 = new double[,] { { 1.0, 2.0, 3.0 }, { 4.0, 5.0, 6.0 }, { 7.0, 8.0, 9.0 }, };
			//QMatrixSquare b1 = new double[,] { { 0.0, 0.0, 1.0 }, { 1.0, 0.0, 0.0 }, { 0.0, 1.0, 0.0 }, };
			QMatrixSquare a1 = QMatrixSquare.Factory.NewIdentityMatrix(dims);
			QMatrixSquare b1 = QMatrixSquare.Factory.NewIdentityMatrix(dims);

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

			NiceTimer.Time("Mult(uBlas)", () => {
				LVQCppCli.Class1.Testublas(iters, dims, dims);
			});
		}

	}
}
