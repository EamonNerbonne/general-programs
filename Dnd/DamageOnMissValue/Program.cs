using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using EmnExtensions.MathHelpers;

namespace DamageOnMissValue
{
	

	class Program
	{
		static void Main(string[] args) {
			var rangeOfHitpoints = Enumerable.Range(42, 56-42);  //Enumerable.Range(42, 56 - 42);
			int trialIters = 10000000;

			Parallel.ForEach(
				new[]{
					 new Program(0.75, 14, 0),
					 new Program(0.5, 15, 6),
					 new Program(0.75, 12, 6),
					 new Program(0.75, 13, 3),
					 new Program(0.75, 14, 1),
				}, (prg) => {
					Console.WriteLine("{0}: {1}", prg, prg.MeanAttacksOverRange(rangeOfHitpoints, trialIters));
				});
		}

		Program(double hitChance, double hitDmg,double missDmg) {
			cumProbs = new[] { hitChance, 1.0 };
			dmg = new[] { hitDmg, missDmg };
			this.hitChance = hitChance;
			this.hitDmg = hitDmg;
			this.missDmg = missDmg;
		}

		double[] cumProbs;
		double[] dmg;
		double hitChance,  hitDmg, missDmg;

		double rollDamage2(MersenneTwister rnd) {		
			double val = rnd.NextDouble0To1();
			int i=0;
			while (val > cumProbs[i]) i++;
			return dmg[i];
		}

		double rollDamage(MersenneTwister rnd) {
			return rnd.NextDouble0To1() < hitChance? hitDmg:missDmg;
		}

		int attacksToKill(int totalHP, MersenneTwister rnd) {
			int attackCount = 0;
			double damage = 0;
			while (damage < totalHP) {
				damage += rollDamage(rnd);
				attackCount++;
			}

			return attackCount;
		}

		MeanVarCalc MeanAttacks( MersenneTwister rnd, int totalHP, int numberOfTrials) {
			MeanVarCalc resultsSink = new MeanVarCalc();
			for (int i = 0; i < numberOfTrials; i++)
				resultsSink.Add(attacksToKill(totalHP, rnd));
			return resultsSink;
		}

		MeanVarCalc MeanAttacksOverRange(IEnumerable<int> totalHPs, int numberOfTrials) {
			MeanVarCalc resultsSink = new MeanVarCalc();
			object lck = new object();
			Parallel.ForEach(totalHPs, (totalHP) => {
				var res = MeanAttacks(RndHelper.ThreadLocalRandom, totalHP, numberOfTrials);
				lock(lck)
					resultsSink.Add(res);
			});
			
			return resultsSink;
		}

		public override string ToString() {
			return string.Format("hitprob:{0}; hitDmg:{1}; missDmg:{2};",hitChance,hitDmg,missDmg) ;
		}

	}
}
