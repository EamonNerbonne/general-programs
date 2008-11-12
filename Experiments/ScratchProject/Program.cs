using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScratchProject
{
    class Program
    {
        static double[] V = new[]{0.0,1.0,2.0,6.0,6.1,9.0,10.0};
        static double[] D = V.Select(v=> Math.Abs(v-9.5)).ToArray();

        static double GenEnergy(double x) {
            return (1.0/V.Length)*
                V.Select(v=>(x-v)*(x-v)).Sum();
        }

        static double SpecCost(double x) {
            double sum = 0;
            for(int i =0;i<V.Length;i++) {
                double v = V[i];
                double d = D[i];
                sum += Sqr((x - v)/d) ;
            }
            return sum / D.Select(d => Sqr(1 / d)).Sum();
        }
        static double MinGuess() {
            return  V.Select((v, i) => v / Sqr(D[i])).Sum() / V.Select((v, i) => 1 / Sqr(D[i])).Sum();
        }
        static double Sqr(double x) {return x*x;}

        static double[] nums = Enumerable.Range(-5, 25).Select(i => i / 5.0).ToArray();

        static void MinMax(Func<double, double> f, IEnumerable<double> range) {
            double minx=double.NaN,min = double.PositiveInfinity;
            double maxx=double.NaN,max = double.NegativeInfinity;
            foreach (var x in range) {
                var fx = f(x);
                if (fx < min) { min = fx; minx = x; }
                if (fx > max) { max = fx; maxx = x; }
             //   Console.WriteLine("f({0}) = {1}", x, fx);
            }
            Console.WriteLine("min: f({0}) = {1}", minx, min);
            Console.WriteLine("min: f({0}) = {1}", maxx, max);
        }

        static void Main(string[] args) {
            MinMax(x=>SpecCost(x) , Enumerable.Range(-10000,20001).Select(i=>i/100.0));
            Console.WriteLine("minguess:" + MinGuess());
            Console.WriteLine("Speccost@9.5:" + SpecCost(9.5));

        }

    }
}
