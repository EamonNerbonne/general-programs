using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmnExtensions;
using EmnExtensions.MathHelpers;

namespace DnDalternateCharRoll
{
	static class HelperCast
	{
		public static int RoundToInt32(this double v)
		{
			return (int)(v + 0.5);
		}
	}

	public struct SmallSortedPositiveList
	{
		readonly ulong val;
		public byte this[int n] { get { return (byte)((val >> n * 8) & 0xff); } }
		public SmallSortedPositiveList Add(int newval)
		{
			if (newval <= byte.MinValue || newval > byte.MaxValue)
				throw new ArgumentOutOfRangeException("newval");
			return AddByte((byte)newval);
		}

		public SmallSortedPositiveList AddByte(byte newval)
		{
			if (this[7] > 0) throw new InvalidOperationException("8 positive bytes already stored");
			int idx = 0;
			while (newval < this[idx]) idx++;//must fail when idx==7
			ulong areLowerMask = ulong.MaxValue << idx * 8;
			return new SmallSortedPositiveList(
			val & ~areLowerMask | (ulong)newval << idx * 8 | (val & areLowerMask) << 8);
		}

		public SmallSortedPositiveList RemoveAt(int idx)
		{
			ulong areBeforeIdx = ~(ulong.MaxValue << idx * 8);

			return new SmallSortedPositiveList(val & areBeforeIdx | (val & ~areBeforeIdx << 8) >> 8);
		}


		SmallSortedPositiveList(ulong data) { val = data; }
		public static readonly SmallSortedPositiveList Empty = default(SmallSortedPositiveList);

		public IEnumerable<byte> Values
		{
			get
			{
				for (ulong curr = val; curr != 0; curr >>= 8)
					yield return (byte)(curr & 0xff);
			}
		}
		public override string ToString()
		{
			return "[" + string.Join(", ", Values.Select(v => v.ToString())) + "]";
		}
	}


	public struct AbilityScores
	{
		public int Str, Con, Dex, Int, Wis, Cha;

		public IEnumerable<int> AllScores
		{
			get
			{
				yield return Str;
				yield return Con;
				yield return Dex;
				yield return Int;
				yield return Wis;
				yield return Cha;
			}
		}

		public static AbilityScores RollInOrder()
		{
			var r = RndHelper.ThreadLocalRandom;
			return new AbilityScores
					{
						Str = Dice.R4d6DropLowest.Try(r),
						Con = Dice.R4d6DropLowest.Try(r),
						Dex = Dice.R4d6DropLowest.Try(r),
						Int = Dice.R4d6DropLowest.Try(r),
						Wis = Dice.R4d6DropLowest.Try(r),
						Cha = Dice.R4d6DropLowest.Try(r),
					};
		}

		public static int Modifier(int score) { return score / 2 - 5; }

		public int TotalMod
		{
			get { return AllScores.Sum((Func<int, int>)Modifier); }
		}

		public override string ToString() { return "Str " + Str + "(" + Modifier(Str) + ")" + "; Con" + Con + "(" + Modifier(Con) + ")" + "; Dex" + Dex + "(" + Modifier(Dex) + ")" + "; Int" + Int + "(" + Modifier(Int) + ")" + "; Wis" + Wis + "(" + Modifier(Wis) + ")" + "; Cha" + Cha + "(" + Modifier(Cha) + ")" + (TotalMod <= 0 ? "!!!" : ""); }
	}


	static class Program
	{
		static readonly int[] costs = { 0, 1, 2, 3, 5, 7, 9, 12, 16 };

		static IEnumerable<SmallSortedPositiveList> PointBuys(SmallSortedPositiveList current, int points, int scoresLeft, int maxScore)
		{
			if (scoresLeft == 1)
			{
				if (points <= 2)
				{
					yield return current.AddByte((byte)(8 + points));
				}
				else
				{
					points -= 2;
					int offset = Array.BinarySearch(costs, points);
					if (offset >= 0)
					{
						yield return current.AddByte((byte)(offset + 10));
					}
				}
			}
			else
			{
				int minPointUsage = (points - 2) / scoresLeft;
				int offset = Array.BinarySearch(costs, minPointUsage);
				int minScore = (offset < 0 ? ~offset : offset) + 10;

				for (int score = maxScore; score >= minScore; score--)
				{
					int pointsLeft = points - costs[score - 10];
					if (pointsLeft >= 0)
						foreach (var res in PointBuys(current.AddByte((byte)score), pointsLeft, scoresLeft - 1, score))
							yield return res;
				}
			}
		}

		const double OverpoweredQuality = 60;
		const double MaxPointBuyQuality = 20;
		const double DecentQuality = 10;
		const double MinOkQuality = 0;
		static double DistrValue(SmallSortedPositiveList stats) { return (stats[0] - 3) * (stats[0] - 3) + (stats[1] - 3) * Math.Sqrt(stats[1] - 3) * 3 + (stats[2] - 3) * 4 + Math.Sqrt(stats[3] - 3.0) * 4.0 + Math.Sqrt(stats[4] - 3.0) * 4.0 + Math.Sqrt(stats[5] - 3.0) * 8.0 - 388.0; }


		static void ShowQualityDistribution(ProbDensity<SmallSortedPositiveList> naturalRollDistribution)
		{
			var qDistr = naturalRollDistribution.MapValues(DistrValue);
			var overpoweredRatio = qDistr.rawprobs.Where(pv => pv.result > OverpoweredQuality).Sum(pv => pv.p);
			var betterRatio = qDistr.rawprobs.Where(pv => pv.result > MaxPointBuyQuality && pv.result <= OverpoweredQuality).Sum(pv => pv.p);
			var goodRatio = qDistr.rawprobs.Where(pv => pv.result <= MaxPointBuyQuality && pv.result > DecentQuality).Sum(pv => pv.p);
			var mediocreRatio = qDistr.rawprobs.Where(pv => pv.result <= DecentQuality && pv.result > MinOkQuality).Sum(pv => pv.p);
			var okRatio = betterRatio + goodRatio + mediocreRatio;
			var worseRatio = qDistr.rawprobs.Where(pv => pv.result <= MinOkQuality).Sum(pv => pv.p);
			Console.WriteLine("Overpowered: {0:f2}; Better: {1:f2}; Good: {2:f2}; Mediocre: {3:f2}; Worse: {4:f2}",
							  overpoweredRatio * 100.0, betterRatio * 100.0, goodRatio * 100.0, mediocreRatio * 100.0, worseRatio * 100.0);
			Console.WriteLine("OK: {0:f2}%", okRatio * 100.0);
			var mv = qDistr.rawprobs.Aggregate(new MeanVarDistrib(), (acc, pv) => acc.Add(pv.result, pv.p));
			var meanScoreStdDev = naturalRollDistribution.MapValues(scores => scores.Values.Aggregate(new MeanVarDistrib(), (acc, v) => acc.Add(v)).StdDev).rawprobs.Aggregate(new MeanVarDistrib(), (acc, pv) => acc.Add(pv.result, pv.p)).Mean;
			Console.WriteLine("Q: {0:f2} ~ {1:f2};    Spread: {2:f2}", mv.Mean, mv.StdDev, meanScoreStdDev);
			Console.WriteLine("18,16... or better: {0:f2}%", naturalRollDistribution.rawprobs.Where(pv => pv.result[0] == 18 && pv.result[1] >= 16).Select(pv => pv.p).OrderBy(p => p).Sum()*100.0);
			Console.WriteLine("At least +10: {0:f2}%", naturalRollDistribution.rawprobs.Where(pv => pv.result.Values.Sum(score => AbilityScores.Modifier(score)) >= 10).Select(pv => pv.p).OrderBy(p => p).Sum() * 100.0);
			Console.WriteLine("Less than +5: {0:f2}%", naturalRollDistribution.rawprobs.Where(pv => pv.result.Values.Sum(score => AbilityScores.Modifier(score)) < 5).Select(pv => pv.p).OrderBy(p => p).Sum() * 100.0);
			Console.WriteLine("3x 16 or better: {0:f2}%", naturalRollDistribution.rawprobs.Where(pv => pv.result[2] >= 16).Select(pv => pv.p).OrderBy(p => p).Sum() * 100.0);
			Console.WriteLine("2x 18: {0:f2}%", naturalRollDistribution.rawprobs.Where(pv => pv.result[1] == 18).Select(pv => pv.p).OrderBy(p => p).Sum() * 100.0);

		}

		static void ShowOverpoweredExamples(ProbDensity<SmallSortedPositiveList> naturalRollDistribution)
		{
			Console.WriteLine("Overpowered:");
			foreach (
				var result in
					naturalRollDistribution.rawprobs.Where(pv => DistrValue(pv.result) > OverpoweredQuality).OrderByDescending(
						pv => pv.p).Take(10)) Console.WriteLine(result);
		}
		static void ShowWeakExamples(ProbDensity<SmallSortedPositiveList> naturalRollDistribution)
		{
			Console.WriteLine("Weak:");
			foreach (
				var result in
					naturalRollDistribution.rawprobs.Where(pv => DistrValue(pv.result) < MinOkQuality).OrderByDescending(
						pv => pv.p).Take(10)) Console.WriteLine(result);
		}
		static void ShowOkExamples(ProbDensity<SmallSortedPositiveList> naturalRollDistribution)
		{
			Console.WriteLine("Normal:");
			foreach (
				var result in
					naturalRollDistribution.rawprobs.Where(pv => DistrValue(pv.result) > MinOkQuality && DistrValue(pv.result) < OverpoweredQuality).OrderByDescending(
						pv => pv.p).Take(5)) Console.WriteLine(result);
		}
		static void ShowTypicalExample(ProbDensity<SmallSortedPositiveList> naturalRollDistribution)
		{
			var orderedDistribution = naturalRollDistribution.rawprobs.OrderBy(pv => DistrValue(pv.result)).ToArray();
			double cumulativeProb = 0;
			var best = ProbValue.Create(0.0,default(SmallSortedPositiveList));
			foreach (var prob in orderedDistribution)
			{
				cumulativeProb += prob.p;
				if (cumulativeProb >= 0.4 && cumulativeProb <= 0.6 && prob.p >best.p)
				{
					best = prob;
				}
			}
			Console.WriteLine("Typical Example: {0:f2}%: {1}", best.p*100.0, best.result);
		}

		static void EvaluateDistributionTop2Scores(ProbDensity<SmallSortedPositiveList> naturalRollDistribution)
		{
			var twoHighestSum = naturalRollDistribution.MapValues(stats => stats[0] + stats[1]);

			Console.WriteLine("Top2: {0}; <32: {1:f2}%"
								, string.Join(";  ", twoHighestSum.rawprobs.OrderByDescending(pv => pv.result).Take(5).Select(pv => pv.result + " " + (pv.p * 100.0).ToString("f2") + "%"))
							  , twoHighestSum.rawprobs.Where(pv => pv.result < 32).Sum(pv => pv.p) * 100.0);
			
			var highest = naturalRollDistribution.MapValues(stats => stats[0]);

			Console.WriteLine("Top:  18: {0:f2}%;  17: {1:f2}%;  16: {2:f2}%;  15: {3:f2}%;  <15: {4:f2}%;"
							  , highest.rawprobs.SingleOrDefault(pv => pv.result == 18).p * 100.0
							  , highest.rawprobs.SingleOrDefault(pv => pv.result == 17).p * 100.0
							  , highest.rawprobs.SingleOrDefault(pv => pv.result == 16).p * 100.0
							  , highest.rawprobs.SingleOrDefault(pv => pv.result == 15).p * 100.0
							  , highest.rawprobs.Where(pv => pv.result < 15).Sum(pv=>pv.p) * 100.0
							  );
			var highest2 = naturalRollDistribution.MapValues(stats => SmallSortedPositiveList.Empty.Add(stats[0]).Add(stats[1]));
			Console.WriteLine(string.Join("; ", highest2.rawprobs.OrderByDescending(pv => pv.p).Take(5).Select(pv => pv.result + " " + (pv.p * 100.0).ToString("f2") + "%")));
		}
		static ProbDensity<SmallSortedPositiveList> NaturalRollDistribution()
		{
			var oneRoll = Dice.R4d6DropLowest;

			Func<ProbDensity<SmallSortedPositiveList>, ProbDensity<SmallSortedPositiveList>> addScore =
				currStatDistributions =>
				ProbDensity.Combine(currStatDistributions, oneRoll, (stats, score) => stats.AddByte((byte)score));

			var statsDistr = ProbDensity.Create(new[] { ProbValue.Create(1.0, SmallSortedPositiveList.Empty) });
			for (int i = 0; i < 6; i++)
				statsDistr = addScore(statsDistr);
			statsDistr = addScore(statsDistr.MapValues(stats => stats.RemoveAt(5))); 
			//statsDistr = ProbDensity.Create(statsDistr.rawprobs.Where(pv => IsOK(pv.result)));
			return statsDistr;
		}

		static ProbDensity<SmallSortedPositiveList> NaturalRoll2Distribution()
		{
			var normalRoll = Dice.R4d6DropLowest;
			var weakRoll = Dice.R3d6;
			var powerRoll = ProbDensity.Combine(Dice.Rd12, Dice.Rd12, Math.Max).MapValues(v => v + 6); // Dice.R5d6DropLowest2;
			var powerRoll2 = ProbDensity.Combine(Dice.Rd8, Dice.Rd8, Math.Max).MapValues(v => v + 10); // Dice.R5d6DropLowest2;
			var powerRoll3 = ProbDensity.Combine(ProbDensity.Combine(Dice.Rd12, Dice.Rd12, Math.Max), Dice.Rd12, Math.Max).MapValues(v => v + 6); // Dice.R5d6DropLowest2;
			var powerRoll4 = ProbDensity.Combine(Dice.Rd6, Dice.Rd6, Math.Max).MapValues(v => v + 12); // Dice.R5d6DropLowest2;


			var addRolledScore = F.Create((SmallSortedPositiveList scores, int roll) => scores.Add(roll));
			var addRoll = F.Create((ProbDensity<SmallSortedPositiveList> currStatDistributions, ProbDensity<int> die) => ProbDensity.Combine(currStatDistributions, die, addRolledScore));

			var statsDistr = ProbDensity.Create(new[] { ProbValue.Create(1.0, SmallSortedPositiveList.Empty) });
			for (int i = 0; i < 6; i++)
				statsDistr = addRoll(statsDistr, normalRoll);

			Func<int, int> lower = score => Math.Max(score - 1, 3);

			for (int i = 0; i < 3; i++)
			{
				statsDistr = ProbDensity.Combine(statsDistr, powerRoll, (scores, roll) =>
				{
					int bestMod = AbilityScores.Modifier(scores[0]);
					//int secondBestScore = Math.Max(8, scores[1] - bestMod);
					return SmallSortedPositiveList.Empty.Add(scores[0]).Add(Math.Max(scores[1] - bestMod, 3)).Add(scores[2]).Add(scores[3]).Add(scores[4]).Add(roll);
				});
				//statsDistr = ProbDensity.Combine(statsDistr, weakRoll, (scores, roll) => scores.RemoveAt(1).Add(roll));
			}
			return statsDistr;
		}

		static ProbDensity<SmallSortedPositiveList> NaturalRoll2bDistribution()
		{
			var normalRoll = Dice.R4d6DropLowest;
			var weakRoll = Dice.R3d6;
			var powerRoll = ProbDensity.Combine(Dice.Rd12, Dice.Rd12, Math.Max).MapValues(v => v + 6); // Dice.R5d6DropLowest2;
			var powerRoll2 = ProbDensity.Combine(Dice.Rd8, Dice.Rd8, Math.Max).MapValues(v => v + 10); // Dice.R5d6DropLowest2;
			var powerRoll3 = ProbDensity.Combine(ProbDensity.Combine(Dice.Rd12, Dice.Rd12, Math.Max), Dice.Rd12, Math.Max).MapValues(v => v + 6); // Dice.R5d6DropLowest2;
			var powerRoll4 = ProbDensity.Combine(Dice.Rd6, Dice.Rd6, Math.Max).MapValues(v => v + 12); // Dice.R5d6DropLowest2;


			var addRolledScore = F.Create((SmallSortedPositiveList scores, int roll) => scores.Add(roll));
			var addRoll = F.Create((ProbDensity<SmallSortedPositiveList> currStatDistributions, ProbDensity<int> die) => ProbDensity.Combine(currStatDistributions, die, addRolledScore));

			var statsDistr = ProbDensity.Create(new[] { ProbValue.Create(1.0, SmallSortedPositiveList.Empty) });
			for (int i = 0; i < 6; i++)
				statsDistr = addRoll(statsDistr, weakRoll);

			for (int i = 0; i < 3; i++)
			{
				statsDistr = ProbDensity.Combine(statsDistr, weakRoll, (scores, roll) => scores.RemoveAt(1).Add(roll));
				statsDistr = ProbDensity.Combine(statsDistr, powerRoll, (scores, roll) => scores.RemoveAt(5).Add(roll));
			}
			return statsDistr;
		}

		static ProbDensity<SmallSortedPositiveList> NaturalRoll3Distribution()
		{
			
			var powerRoll = ProbDensity.Combine(Dice.Rd12, Dice.Rd12, Math.Max).MapValues(v => v + 6); // Dice.R5d6DropLowest2;
			var powerRoll2 = ProbDensity.Combine(Dice.Rd8, Dice.Rd8, Math.Max).MapValues(v => v + 10); // Dice.R5d6DropLowest2;
			var powerRoll3 = ProbDensity.Combine(ProbDensity.Combine(Dice.Rd12, Dice.Rd12, Math.Max), Dice.Rd12, Math.Max).MapValues(v => v + 6); // Dice.R5d6DropLowest2;
			var powerRoll4 = ProbDensity.Combine(Dice.Rd6, Dice.Rd6, Math.Max).MapValues(v => v + 12); // Dice.R5d6DropLowest2;
			var powerRoll5 = ProbDensity.Combine(Dice.Rd8, Dice.Rd8, Math.Max).MapValues(v => v + 10); // Dice.R5d6DropLowest2;

			var addRolledScore = F.Create((SmallSortedPositiveList scores, int roll) => scores.Add(roll));
			var addRoll = F.Create((ProbDensity<SmallSortedPositiveList> currStatDistributions, ProbDensity<int> die) => ProbDensity.Combine(currStatDistributions, die, addRolledScore));

			var statsDistr = ProbDensity.Create(new[] { ProbValue.Create(1.0, SmallSortedPositiveList.Empty) });
			for (int i = 0; i < 6; i++)
				statsDistr = addRoll(statsDistr, Dice.R2d8.MapValues(v => v + 2));

			for (int i = 0; i < 3; i++)
			{
				//statsDistr = ProbDensity.Combine(statsDistr, weakRoll, (scores, roll) => scores.RemoveAt(1).Add(roll));
				//statsDistr = statsDistr.MapValues(scores => scores.RemoveAt(1).Add(Math.Max(scores[1] - AbilityScores.Modifier(scores[0]), 3)));
				statsDistr = ProbDensity.Combine(statsDistr, Dice.R3d8DropLowest.MapValues(v=>v+2), (scores, roll) => scores.RemoveAt(5).Add(roll));
			}
			//return statsDistr;
			statsDistr = ProbDensity.Create(statsDistr.rawprobs.Where(pv => IsOK(pv.result)));
			return statsDistr;
		}

		static bool IsOK(SmallSortedPositiveList scores)
		{
			return scores[0] + scores[1] >= 32
				&& AbilityScores.Modifier(scores[0]) + AbilityScores.Modifier(scores[1]) + AbilityScores.Modifier(scores[2]) + AbilityScores.Modifier(scores[3]) + AbilityScores.Modifier(scores[4]) + AbilityScores.Modifier(scores[5]) > 0;
		}
		
		static ProbDensity<SmallSortedPositiveList> NaturalRoll4Distribution()
		{
			var weakRoll = Dice.R3d6;
			var R2d8p2 = Dice.R2d8.Add(2).MapValues(roll => roll > 10 ? roll - 8 : roll + 8);
			var variableWeakRoll = Dice.R2d8.MapValues(v => v + 2);

			var Rd64 = ProbDensity.UniformDistribution(Enumerable.Range(0, 64));
			var R2d12max = ProbDensity.Combine(Dice.Rd12, Dice.Rd12, Math.Max); // Dice.R5d6DropLowest2;
			var R2d12min = ProbDensity.Combine(Dice.Rd12, Dice.Rd12, Math.Min); // Dice.R5d6DropLowest2;
			var R2d6max = ProbDensity.Combine(Dice.Rd6, Dice.Rd6, Math.Max); // Dice.R5d6DropLowest2;
			var R2d6min = ProbDensity.Combine(Dice.Rd6, Dice.Rd6, Math.Min); // Dice.R5d6DropLowest2;
			var addRolledScore = F.Create((SmallSortedPositiveList scores, int roll) => scores.Add(roll));
			var addRoll = F.Create((ProbDensity<SmallSortedPositiveList> currStatDistributions, ProbDensity<int> die) => ProbDensity.Combine(currStatDistributions, die, addRolledScore));

			var statsDistr = ProbDensity.Create(new[] { ProbValue.Create(1.0, SmallSortedPositiveList.Empty) });
			for (int i = 0; i < 6; i++)
				statsDistr = addRoll(statsDistr, weakRoll);


			for (int i = 0; i < 20; i++)
			{
				statsDistr =
					ProbDensity.Create(
					statsDistr.rawprobs.SelectMany(pv =>
					{
						return 
						IsOK(pv.result) ? Enumerable.Repeat(pv,1)
							:
							Dice.Rd6.rawprobs.Select(rollPv => ProbValue.Create(rollPv.p * pv.p, 
								pv.result
								//	.RemoveAt(rollPv.result - 1).Add(Math.Max(3, Math.Min(18, pv.result[rollPv.result] + 1)))	
									.RemoveAt(5)
									.Add(18 - Math.Abs(pv.result[1] + rollPv.result - 18))
//									.Add(12 + rollPv.result/2)
									))
							;
					}));
					
			}
			//statsDistr = ProbDensity.Create(statsDistr.rawprobs.Where(pv => IsOK(pv.result)));
			return statsDistr;
		}


		static void Main()
		{
			//ShowPointBuyQualities();
			var naturalRollDistribution = NaturalRoll4Distribution();

			ShowOverpoweredExamples(naturalRollDistribution);
			ShowOkExamples(naturalRollDistribution);
			ShowWeakExamples(naturalRollDistribution);
			ShowTypicalExample(naturalRollDistribution);
			EvaluateDistributionTop2Scores(naturalRollDistribution);
			ShowQualityDistribution(naturalRollDistribution);
		}


		static void ShowPointBuyQualities()
		{
			var pointBuys = PointBuys(SmallSortedPositiveList.Empty, 22, 6, 18)
				.Where(distr => distr[4] == 10 && distr[5] == 8)
				.OrderByDescending(DistrValue).ToArray();

			Console.WriteLine(pointBuys.Length);
			foreach (var distr in pointBuys.Take(40))
			{
				Console.WriteLine(DistrValue(distr) + ": " + distr + "  (spread: " + distr.Values.Aggregate(new MeanVarDistrib(), (acc, v) => acc.Add(v)).StdDev + ")");
			}

			/*
18.6164997430956: [17, 16, 11, 10, 10, 8]
16.4486180817282: [18, 14, 11, 10, 10, 8]
10.7076581449592: [17, 15, 13, 10, 10, 8]
9.86832980505142: [18, 13, 13, 10, 10, 8]
7.70765814495917: [17, 15, 12, 11, 10, 8]
6.86832980505142: [18, 13, 12, 11, 10, 8]
0.616499743095574: [16, 16, 13, 11, 10, 8]
-0.551381918271773: [17, 14, 14, 10, 10, 8]
-2.38350025690443: [16, 16, 12, 12, 10, 8]
-2.55138191827177: [17, 14, 13, 12, 10, 8]
-6: [18, 12, 12, 12, 10, 8]
-11.2923418550408: [16, 15, 14, 11, 10, 8]
-13.2923418550408: [16, 15, 13, 13, 10, 8]
-24.5513819182718: [16, 14, 14, 13, 10, 8]
-32.2923418550408: [15, 15, 15, 11, 10, 8]
-34.2923418550408: [15, 15, 14, 13, 10, 8]
-48.5513819182718: [15, 14, 14, 14, 10, 8]
			 * */
		}
	}
}
