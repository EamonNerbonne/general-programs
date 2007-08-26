using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DndTwfStats
{

	public class Weapon
	{
		public ProbDensity damage;
		public int multiplier;
		public int threat;
		public Weapon(ProbDensity damage, int multiplier, int threat) { this.damage = damage; this.multiplier = multiplier; this.threat = threat; }
	}

	public class WeaponAttack
	{
		int threatRange;//lower range
		int multiplier;
		int attackRollMod;//WF, str, etc... only constant things, no BAB and no TWF or PA penalties
		ProbDensity critDamage;//baseDamage*multiplier + extraDamage
		ProbDensity normDamage;//baseDamage+extraDamage

		public ProbDensity AttackRoll(int mod,//BAB+ variable things like such as TWF penalty or iterative -5 penalty or powerattack
															int AC,
															int bonusDmg//such as power-attack related.
															) {
			int hitOpts = Math.Min(Math.Max(mod + attackRollMod + 21 - AC, 1), 19);
			int threatOpts = Math.Min(21 - threatRange, hitOpts);
			double hitChance = hitOpts/20.0;
			double threatChance=threatOpts/20.0;
			
			double critHitChance = threatChance*hitChance;
			double normHitChance = hitChance - critHitChance;
			double noHitChance = 1-hitChance;

			//noHit+normHit+crit == 1

			var normDmg = normDamage.Probabilities.Select(prob => new Prob(prob.p * normHitChance, prob.result+bonusDmg));
			var critDmg = critDamage.Probabilities.Select(prob => new Prob(prob.p * critHitChance, prob.result+bonusDmg*multiplier));
			var noDmg = new Prob[] { new Prob(noHitChance, 0) };
			var retval = new ProbDensity(normDmg.Concat(critDmg).Concat(noDmg));
			return retval;
		}

		public WeaponAttack(Weapon weapon, ProbDensity extraDamageDice, ProbDensity critExtraDamageDice, int damageBonus, int attackBonus) {//const attackbonus excl. BAB or powerattack
			var baseDamage=weapon.damage.MapValues(dmg => dmg + damageBonus);
			var multDamage = Enumerable.Repeat(baseDamage, weapon.multiplier).Aggregate((a, b) => ProbDensity.AddDistributions(a, b));
			this.critDamage = ProbDensity.AddDistributions(ProbDensity.AddDistributions(multDamage, extraDamageDice),critExtraDamageDice);
			this.normDamage = ProbDensity.AddDistributions(baseDamage, extraDamageDice);
			this.threatRange = weapon.threat;
			this.multiplier = weapon.multiplier;
			this.attackRollMod = attackBonus;
		}
	}

	static class Dice
	{
		public static ProbDensity Rd4, R2d4, Rd6, Rd8, Rd10, Rd12, Rd20, R2d6, R3d6, R4d6, R5d6, R6d6, R7d6, R8d6, R9d6, R10d6, R11d6,R12d6, NoDice;

		static Dice() {
			Rd4 = ProbDensity.UniformDistribution(Enumerable.Range(1, 4));
			R2d4 = ProbDensity.AddDistributions(Rd4, Rd4);
			Rd6 = ProbDensity.UniformDistribution(Enumerable.Range(1, 6));
			Rd8 = ProbDensity.UniformDistribution(Enumerable.Range(1, 8));
			Rd10 = ProbDensity.UniformDistribution(Enumerable.Range(1, 10));
			Rd12 = ProbDensity.UniformDistribution(Enumerable.Range(1, 12));
			Rd20 = ProbDensity.UniformDistribution(Enumerable.Range(1, 20));
			R2d6 = ProbDensity.AddDistributions(Rd6, Rd6);
			R3d6 = ProbDensity.AddDistributions(R2d6, Rd6);
			R4d6 = ProbDensity.AddDistributions(R3d6, Rd6);
			R5d6 = ProbDensity.AddDistributions(R4d6, Rd6);
			R6d6 = ProbDensity.AddDistributions(R5d6, Rd6);
			R7d6 = ProbDensity.AddDistributions(R6d6, Rd6);
			R8d6 = ProbDensity.AddDistributions(R7d6, Rd6);
			R9d6 = ProbDensity.AddDistributions(R8d6, Rd6);
			R10d6 = ProbDensity.AddDistributions(R9d6, Rd6);
			R11d6 = ProbDensity.AddDistributions(R10d6, Rd6);
			R12d6 = ProbDensity.AddDistributions(R11d6, Rd6);
			NoDice = ProbDensity.UniformDistribution(Enumerable.Range(0, 1));
		}
	}



	public struct Prob
	{
		public double p;
		public int result;
		public double WeightedValue { get { return p * result; } }
		public Prob(CumProb prob) { this.p = prob.p; this.result = prob.result; }
		public Prob(double p, int result) { this.p = p; this.result = result; }
	}
	public struct CumProb:IComparable<CumProb>
	{
		public double p;
		public double cumP;
		public int result;


		public int CompareTo(CumProb other) {
			return cumP.CompareTo(other.cumP);
		}
		public CumProb(double cumP) { this.cumP = cumP; result = 0; p = 0; }
		public CumProb(Prob firstProb) : this(firstProb, 0.0) { }
		public CumProb(Prob prob, double prevCumP) { this.result = prob.result; this.p = prob.p; this.cumP = p + prevCumP; }
	}

	public class ProbDensity
	{
		public CumProb[] probs;
		double totalP;
		double average;
		public ProbDensity(IEnumerable<Prob> probs) {
			//probs = probs.GroupBy(p => p.result).Select(g => new Prob(g.Select(p => p.p).Sum(), g.Key));

			probs =
				from p in probs
				group p by p.result into g
				select new Prob((from p in g select p.p).Sum(), g.Key);
			probs = probs.ToArray();

			totalP = probs.Sum(pr => pr.p);
			probs = probs.Select(pr => new Prob(pr.p / totalP, pr.result)).ToArray();
			CumProb last=new CumProb(0);
			var cprobs =new List<CumProb>();
			average = 0;
			totalP = 0;
			foreach(var prob in probs) {
				cprobs.Add(last=new CumProb(prob, last.cumP));
				average += prob.WeightedValue;
				totalP += prob.p;
			}
			average /= totalP;
			//Console.WriteLine(totalP);
			this.probs = cprobs.ToArray();
		}
		public double Try() {
			double roll = Program.R.NextDouble() * totalP;
			int index = Array.BinarySearch(probs,new CumProb(roll));
			if(index<0) index = ~index;
			return probs[index].result;
		}
		public IEnumerable<Prob> Probabilities { get { return probs.Select(cumprob => new Prob(cumprob)); } }
		public double Average { get { return average; } }

		public static ProbDensity AddDistributions(ProbDensity a, ProbDensity b) {
			return new ProbDensity(				
				from ap in a.Probabilities
				from bp in b.Probabilities
				select new Prob(ap.p * bp.p, ap.result + bp.result)
				);
		}

		public static ProbDensity UniformDistribution(IEnumerable<int> values) {
			return new ProbDensity(values.Select(val => new Prob(1, val)));
		}
		public ProbDensity MapValues(Func<int, int> f) { 
			return new ProbDensity(Probabilities.Select(p => new Prob(p.p, f(p.result)) ) ); }
	}

	class Program
	{
		public static IEnumerable<int> FullAttackSeq(int bab) {
			var attackSeq=new List<int>();
			for(int ab=bab;ab>0;ab-=5) attackSeq.Add(ab);
			return attackSeq.ToArray();
		}

		public static IEnumerable<int> SingleAttackSeq(int bab) {			return new int[]{bab};		}

		public static int OptimalPowerAttack(WeaponAttack wa, int paMult, IEnumerable<int> attackSeq, int bab,int AC) {
			int bestPow=0;
			double bestDmg=0;
			for(int powAtt = 0; powAtt <= bab; powAtt++) {
				double totDmg = attackSeq.Select(attack=>  wa.AttackRoll(attack - powAtt, AC, paMult * powAtt).Average).Sum();
				if(totDmg > bestDmg) {
					bestPow = powAtt;
					bestDmg = totDmg;
				}
			}
			return bestPow;
		}



		public static Random R = new Random();
		static void Main(string[] args) {
			int strBonusH = 8;
			int babH=16;

			int enhH = 5;
			int wfH = 3;
			int wsH = 4;
			bool thfOfSpeed = false;
			ProbDensity thfBonusDie = Dice.R2d6;
			Weapon falchion = new Weapon(Dice.R2d4, 2, 15);


			int strBonusW = 8;
			int babW = 16;

			int enhAW = 2;
			int enhBW = 1;
			int wfW = 3;
			int wsW = 4;
			ProbDensity twfABonusDie = Dice.R4d6;
			ProbDensity twfBBonusDie = Dice.R4d6;
			int twfPenalty = 2;
			Weapon shortsword = new Weapon(Dice.Rd6, 2, 19);

			WeaponAttack thf = new WeaponAttack(falchion, thfBonusDie, Dice.NoDice, enhH + strBonusH * 3 / 2 + wsH, wfH + strBonusH + enhH);
			WeaponAttack twfA = new WeaponAttack(shortsword, twfABonusDie, Dice.NoDice, enhAW + strBonusW + wsW, wfW + strBonusW + enhAW);
			WeaponAttack twfB = new WeaponAttack(shortsword, twfBBonusDie, Dice.NoDice, enhBW + strBonusW / 2 + wsW, wfW + strBonusW + enhBW);

			double twfTot = 0;
			double thfTot = 0;
			double twf1Tot = 0;
			double thf1Tot = 0;
			int count = 0;
			foreach(int AC in Enumerable.Range(15, 36)) {
				Console.WriteLine("\n=== vs. AC {0} ===", AC);

				int optPowFA = OptimalPowerAttack(thf, 2, FullAttackSeq(babH).Concat(Enumerable.Range(babH,thfOfSpeed?1:0)), babH, AC);
				int optPowSA = OptimalPowerAttack(thf, 2, SingleAttackSeq(babH), babH, AC);

				double ThfDmgFA = FullAttackSeq(babH).Concat(Enumerable.Range(babH, thfOfSpeed ? 1 : 0)).Select(ab => thf.AttackRoll(ab - optPowFA, AC, optPowFA * 2).Average).Sum();
				double ThfDmgSA = SingleAttackSeq(babH).Select(ab => thf.AttackRoll(ab - optPowSA, AC, optPowSA * 2).Average).Sum();

				double TwfDmgFA = FullAttackSeq(babW).Select(ab => twfA.AttackRoll(ab - twfPenalty, AC, 0).Average).Sum() + FullAttackSeq(babW).Take(3).Select(ab => twfB.AttackRoll(ab - twfPenalty, AC, 0).Average).Sum(); 
				double TwfDmgSA = SingleAttackSeq(babW).Select(ab => twfA.AttackRoll(ab, AC, 0).Average ).Sum();


				thf1Tot += ThfDmgSA;
				thfTot += ThfDmgFA;
				twf1Tot += TwfDmgSA;
				twfTot += TwfDmgFA;
				Console.WriteLine("THF-weighted: {0,6:F02}, SA: {1,6:F02}, FA: {2,6:F02}", (ThfDmgFA + ThfDmgSA) / 2, ThfDmgSA, ThfDmgFA);
				Console.WriteLine("TWF-weighted: {0,6:F02}, SA: {1,6:F02}, FA: {2,6:F02}", (TwfDmgFA + TwfDmgSA) / 2, TwfDmgSA, TwfDmgFA);
				count++;
			}
			Console.WriteLine("\n=== Average over all AC's ===");

			Console.WriteLine("THF-weighted: {0,6:F02}, SA: {1,6:F02}, FA: {2,6:F02}", (thf1Tot + thfTot) / 2 / count, thf1Tot / count, thfTot / count);
			Console.WriteLine("TWF-weighted: {0,6:F02}, SA: {1,6:F02}, FA: {2,6:F02}", (twf1Tot + twfTot) / 2 / count, twf1Tot / count, twfTot / count);
			Console.ReadLine();
		}
	}
}
