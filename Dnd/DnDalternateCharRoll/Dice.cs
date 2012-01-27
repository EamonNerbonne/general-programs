using System;
using System.Linq;

namespace DnDalternateCharRoll
{
	public static class Dice
	{
		public static readonly ProbDensity<int> Rd4, R2d4, Rd6, Rd8, R2d8, R3d8, Rd10, Rd12, Rd20, R2d6, R3d6, R4d6, R5d6, R6d6, R7d6, R8d6, R9d6, R10d6, R11d6, R12d6, NoDice;
		public static readonly ProbDensity<int> R4d6DropLowest, R5d6DropLowest2, R3d8DropLowest;

		static Dice()
		{
			Rd4 = ProbDensity.UniformDistribution(Enumerable.Range(1, 4));
			Rd6 = ProbDensity.UniformDistribution(Enumerable.Range(1, 6));
			Rd8 = ProbDensity.UniformDistribution(Enumerable.Range(1, 8));
			Rd10 = ProbDensity.UniformDistribution(Enumerable.Range(1, 10));
			Rd12 = ProbDensity.UniformDistribution(Enumerable.Range(1, 12));
			Rd20 = ProbDensity.UniformDistribution(Enumerable.Range(1, 20));

			R2d4 = Rd4.Add(Rd4);
			R2d8 = Rd8.Add(Rd8);
			R3d8 = R2d8.Add(Rd8);


			R2d6 = Rd6.Add(Rd6);
			R3d6 = R2d6.Add(Rd6);
			R4d6 = R3d6.Add(Rd6);
			R5d6 = R4d6.Add(Rd6);
			R6d6 = R5d6.Add(Rd6);
			R7d6 = R6d6.Add(Rd6);
			R8d6 = R7d6.Add(Rd6);
			R9d6 = R8d6.Add(Rd6);
			R10d6 = R9d6.Add(Rd6);
			R11d6 = R10d6.Add(Rd6);
			R12d6 = R11d6.Add(Rd6);

			NoDice = ProbDensity.UniformDistribution(Enumerable.Repeat(0, 1));

			R4d6DropLowest = ProbDensity.Create(
				from a in Enumerable.Range(1, 6)
				from b in Enumerable.Range(1, 6)
				from c in Enumerable.Range(1, 6)
				from d in Enumerable.Range(1, 6)
				let lowest = Math.Min(a, Math.Min(b, Math.Min(c, d)))
				let sum = a + b + c + d
				select ProbValue.Create(1.0, sum - lowest)
				);
			R3d8DropLowest = ProbDensity.Create(
				from a in Enumerable.Range(1, 8)
				from b in Enumerable.Range(1, 8)
				from c in Enumerable.Range(1, 8)
				let lowest = Math.Min(a, Math.Min(b, c))
				let sum = a + b + c 
				select ProbValue.Create(1.0, sum - lowest)
				);
			R5d6DropLowest2 = ProbDensity.Create(
				from a in Enumerable.Range(1, 6)
				from b in Enumerable.Range(1, 6)
				from c in Enumerable.Range(1, 6)
				from d in Enumerable.Range(1, 6)
				from e in Enumerable.Range(1, 6)
				let rolls = SmallSortedPositiveList.Empty.Add(a).Add(b).Add(c).Add(d).Add(e)
				select ProbValue.Create(1.0, rolls[0]+rolls[1]+rolls[2])
				);

		}
	}
}