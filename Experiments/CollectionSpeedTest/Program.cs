#define USECPP
#define DOSLOWTESTS

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmnExtensions;
using EmnExtensions.MathHelpers;
using System.Diagnostics;
using System.Collections.ObjectModel;

namespace CollectionSpeedTest
{
    public static class Operators
    {
        public static double DotL(this double[] vecA, double[] vecB) {
            double sum = 0.0;
            for (int i = 0; i < vecA.Length; i++)
                sum += vecA[i] * vecB[i];
            return sum;
        }
        public static double Dot(this double[] vecA, double[] vecB) {
            double sum = 0.0;
            for (int i = 0; i < Program.SIZE; i++)
                sum += vecA[i] * vecB[i];
            return sum;
        }
        public static double DotFullTest(double[] vecA, double[] vecB) {
            double sumO = 0.0;
            for (int i = 0; i < Program.ITER; i++) {
                double sum = 0.0;
                for (int j = 0; j < vecA.Length; j++)
                    sum += vecA[j] * vecB[j];
                sumO += sum;
            }
            return sumO;
            //Console.WriteLine(sumO);

        }
    }
    class Program
    {
        public  const int ITER = 10000000;
        public const int SIZE = 100;
        static Random r = new Random();
        static void Main(string[] args) {
            vecA = Functional.AsEnumerable(() => r.NextNorm()).Take(SIZE).ToArray();
            vecB = Functional.AsEnumerable(() => r.NextNorm()).Take(SIZE).ToArray();
            DoTest();
        }

        static double[] vecA, vecB;
        public static double DotFullTest(double[] vecA, double[] vecB) {
            double sumO = 0.0;
            for (int i = 0; i < ITER; i++) {
                double sum = 0.0;
                for (int j = 0; j < vecA.Length; j++)
                    sum += vecA[j] * vecB[j];
                sumO += sum;
            }
            return sumO;
        }
        public static double DotFullTest2() {
            double sumO = 0.0;
            for (int i = 0; i < ITER; i++) {
                double sum = 0.0;
                for (int j = 0; j < vecA.Length; j++)
                    sum += vecA[j] * vecB[j];
                sumO += sum;
            }
            return sumO;
        }
        public static double DotFullTestC(double[] vecA, double[] vecB) {
            double sumO = 0.0;
            for (int i = 0; i < ITER; i++) {
                double sum = 0.0;
                for (int j = 0; j < SIZE; j++)
                    sum += vecA[j] * vecB[j];
                sumO += sum;
            }
            return sumO;
        }
        public static double DotFullTest2C() {
            double sumO = 0.0;
            for (int i = 0; i < ITER; i++) {
                double sum = 0.0;
                for (int j = 0; j < SIZE; j++)
                    sum += vecA[j] * vecB[j];
                sumO += sum;
            }
            return sumO;
        }

        public static double DotSubTestC(double[] vecA, double[] vecB) {
            double sumO = 0.0;
            for (int i = 0; i < ITER; i++) {
                double sum = vecA.Dot(vecB);
                sumO += sum;
            }
            return sumO;
        }
        public static double DotSubLTestC(double[] vecA, double[] vecB) {
            double sumO = 0.0;
            for (int i = 0; i < ITER; i++) {
                double sum = vecA.DotL(vecB);
                sumO += sum;
            }
            return sumO;
        }

        static void DoTest() {
            var vecAl = vecA.ToList();
            var vecBl = vecB.ToList();
            IList<double> vecAi = vecA;
            IList<double> vecBi = vecB;
            IList<double> vecAi2 = vecAl;
            IList<double> vecBi2 = vecBl;
            var vecAlr= vecAl.AsReadOnly();
            var vecBlr = vecBl.AsReadOnly();
            var vecAr = new ReadOnlyCollection<double>(vecA);
            var vecBr = new ReadOnlyCollection<double>(vecB);
            //var vecA = Functional.AsEnumerable(() => r.NextNorm()).Take(SIZE).ToArray();
            //var vecB = Functional.AsEnumerable(() => r.NextNorm()).Take(SIZE).ToArray();
            Console.WriteLine("word-size:" +System.Runtime.InteropServices.Marshal.SizeOf(typeof(IntPtr))*8 );
            for (int trials = 0; trials < 2; trials++) {
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
                NiceTimer.Time("FOR (param) dot " + ITER + "x" + SIZE, () => {
                    double sumO = DotFullTestC(vecA, vecB);
                    Console.WriteLine(sumO);
                });
                NiceTimer.Time("FOR (variable) dot " + ITER + "x" + SIZE, () => {
                    double sumO = DotFullTest2C();
                    Console.WriteLine(sumO);
                });
                NiceTimer.Time("FOR using Length(param) dot " + ITER + "x" + SIZE, () => {
                    double sumO = DotFullTest(vecA, vecB);
                    Console.WriteLine(sumO);
                });
                NiceTimer.Time("FOR using Length(variable) dot " + ITER + "x" + SIZE, () => {
                    double sumO = DotFullTest2();
                    Console.WriteLine(sumO);
                });
                NiceTimer.Time(".Dot " + ITER + "x" + SIZE, () => {
                    double sumO = DotSubTestC(vecA,vecB);
                    Console.WriteLine(sumO);
                });
                NiceTimer.Time(".Dot using Length" + ITER + "x" + SIZE, () => {
                    double sumO = DotSubLTestC(vecA, vecB);
                    Console.WriteLine(sumO);
                });
#if DOSLOWTESTS
                NiceTimer.Time("FOR LIST dot " + ITER + "x" + SIZE, () => {
                    double sumO = 0.0;

                    for (int i = 0; i < ITER; i++) {
                        double sum = 0.0;

                        for (int j = 0; j < SIZE; j++)
                            sum += vecAl[j] * vecBl[j];
                        sumO += sum;
                    }
                    Console.WriteLine(sumO);
                });
                NiceTimer.Time("FOR ILIST ARRAY dot " + ITER + "x" + SIZE, () => {
                    double sumO = 0.0;

                    for (int i = 0; i < ITER; i++) {
                        double sum = 0.0;

                        for (int j = 0; j < SIZE; j++)
                            sum += vecAi[j] * vecBi[j];
                        sumO += sum;
                    }
                    Console.WriteLine(sumO);
                });
                NiceTimer.Time("FOR ILIST LIST dot " + ITER + "x" + SIZE, () => {
                    double sumO = 0.0;

                    for (int i = 0; i < ITER; i++) {
                        double sum = 0.0;

                        for (int j = 0; j < SIZE; j++)
                            sum += vecAi2[j] * vecBi2[j];
                        sumO += sum;
                    }
                    Console.WriteLine(sumO);
                });
                NiceTimer.Time("FOR ReadOnly LIST dot " + ITER + "x" + SIZE, () => {
                    double sumO = 0.0;

                    for (int i = 0; i < ITER; i++) {
                        double sum = 0.0;

                        for (int j = 0; j < SIZE; j++)
                            sum += vecAlr[j] * vecBlr[j];
                        sumO += sum;
                    }
                    Console.WriteLine(sumO);
                });
                NiceTimer.Time("FOR ReadOnly array dot " + ITER + "x" + SIZE, () => {
                    double sumO = 0.0;

                    for (int i = 0; i < ITER; i++) {
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
