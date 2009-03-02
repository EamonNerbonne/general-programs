using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmnExtensions.MathHelpers
{
	public struct MeanVarCalc
	{
		double sum;
		double sum2;
		int count;

		public void Add(double val) {
			count += 1;
			sum += val;
			sum2 += val * val;
		}
		public void Add(MeanVarCalc other) {			Add(other.count, other.sum, other.sum2);	}
		public void Add(int count, double sum, double sumSquared) {
			this.count += count;
			this.sum += sum;
			this.sum2 += sumSquared;
		}

		public double Mean { get { return sum / count; } }
		//need Math.Max since it's possible rounding errors cause negative numbers!
		public double Var { get { return Math.Max(0,(sum2 - Mean * Mean * count) / (count - 1)); } }
		public double StdDev { get { return Math.Sqrt(Var); } }
		public int Count { get { return count; } }

		public override string ToString() {			return Mean.ToString() + " +/- " + StdDev.ToString();		}
	}
}
