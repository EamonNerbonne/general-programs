using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmnExtensions.MathHelpers;

namespace DnDalternateCharRoll
{
	public struct ProbValue<T>
	{
		public readonly double p;
		public readonly T result;
		public ProbValue(double p, T result) { this.p = p; this.result = result; }
		public override string ToString() { return (p *100.0).ToString("f2") + "%: " + result; }
	}

	public static class ProbValue
	{
		public static ProbValue<T> Create<T>(double p, T result) { return new ProbValue<T>(p, result); }
	}

	public class ProbDensity<T>
	{
		public readonly ProbValue<T>[] rawprobs;
		public readonly double[] cumprobs;
		public ProbDensity(IEnumerable<ProbValue<T>> probs)
		{
			double totalP = probs.Sum(pr => pr.p);

			rawprobs =
				(from p in probs
				 group p.p by p.result into valueGroupProbs
				 select new ProbValue<T>(valueGroupProbs.OrderBy(p => p).Sum() / totalP, valueGroupProbs.Key)
				).ToArray();

			cumprobs = new double[rawprobs.Length];
			double cumProb = 0.0;
			for (int i = 0; i < cumprobs.Length; i++)
			{
				cumProb += rawprobs[i].p;
				cumprobs[i] = cumProb;
			}
		}

		ProbDensity(ProbValue<T>[] rawprobs, double[] cumprobs) { this.rawprobs = rawprobs; this.cumprobs = cumprobs; }
		public ProbDensity<TOut> MapValues<TOut>(Func<T, TOut> f)
		{
			return new ProbDensity<TOut>(rawprobs.Select(p => ProbValue.Create(p.p, f(p.result))).ToArray());
		}

		public ProbDensity<T> Filter(Func<T, bool> test) {
			return new ProbDensity<T>(rawprobs.Where(p => test(p.result)).ToArray());
			
		}

		public T Try(Random r)
		{
			double roll = r.NextDouble();
			int index = Array.BinarySearch(cumprobs, roll);
			if (index < 0) index = ~index;
			return rawprobs[index].result;
		}
	}

	public static class ProbDensity
	{
		public static ProbDensity<int> Add(this ProbDensity<int> distr, int offset) { return distr.MapValues(v => v + offset); }
		public static ProbDensity<int> Add(this ProbDensity<int> a, ProbDensity<int> b) { return Combine(a, b, (av, bv) => av + bv); }
		public static ProbDensity<double> Add(this ProbDensity<double> distr, double offset) { return distr.MapValues(v => v + offset); }
		public static ProbDensity<double> Add(this ProbDensity<double> a, ProbDensity<double> b) { return Combine(a, b, (av, bv) => av + bv); }

		public static ProbDensity<T> Create<T>(IEnumerable<ProbValue<T>> probabilities) { return new ProbDensity<T>(probabilities); }

		public static ProbDensity<TR> Combine<TA, TB, TR>(this ProbDensity<TA> a, ProbDensity<TB> b, Func<TA, TB, TR> f)
		{
			return Create(
				from ap in a.rawprobs
				from bp in b.rawprobs
				select ProbValue.Create(ap.p * bp.p, f(ap.result, bp.result))
				);
		}

		public static MeanVarDistrib Distribution(this ProbDensity<int> distr) {
			return distr.rawprobs.Aggregate(new MeanVarDistrib(), (acc, pv) => acc.Add(pv.result, pv.p));
		}

		public static ProbDensity<TR> MapMany<TA, TB, TR>(this ProbDensity<TA> a, Func<TA, ProbDensity<TB>> b, Func<TA, TB, TR> f)
		{
			return Create(
				from ap in a.rawprobs
				from bp in b(ap.result).rawprobs
				select ProbValue.Create(ap.p * bp.p, f(ap.result, bp.result))
				);
		}

		public static ProbDensity<T> UniformDistribution<T>(IEnumerable<T> values)
		{
			return Create(values.Select(v => ProbValue.Create(1.0, v)));
		}
	}
}
