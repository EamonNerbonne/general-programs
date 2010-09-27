using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using EmnExtensions.MathHelpers;
using EmnExtensions.DebugTools;

namespace EmnExtensions.Algorithms {
	public static class SelectionAlgorithm {
		public static double QuickSelect(double[] list, int k) {
			return QuickSelect(list, k, 0, list.Length);
		}
		public static double QuickSelect(double[] list, int k, int startI, int endI) {
			while (true) {
				// Assume startI <= k < endI
				int pivotI = (startI + endI) / 2;//arbitrary, but good if sorted, and doesn't pick first element unnecessarily.
				int splitI = partition(list, startI, endI, pivotI);
				if (k < splitI)
					endI = splitI;
				else if (k > splitI)
					startI = splitI + 1;
				else //if (k == splitI)
					return list[k];
			}
			//when this returns, all elements of list[i] <= list[k] iif i <= k
		}
		static int partition(double[] list, int startI, int endI, int pivotI) {
			double pivotValue = list[pivotI];
			list[pivotI] = list[startI];
			list[startI] = pivotValue;

			int storeI = startI + 1;//no need to store @ pivot item, it's good already.
			//Invariant: startI < storeI <= endI
			while (storeI < endI && list[storeI] <= pivotValue) ++storeI;//if sorted this is a big win, else no lose.
			//now storeI == endI || list[storeI] > pivotValue
			//so elem @storeI is either irrelevant or too large.
			for (int i = storeI + 1; i < endI; ++i)
				if (list[i] <= pivotValue) {
					list.swap_elems(i, storeI);
					++storeI;
				}
			int newPivotI = storeI - 1;
			list[startI] = list[newPivotI];
			list[newPivotI] = pivotValue;
			//now [startI, newPivotI] are <= to pivotValue && list[newPivotI] contains newPivotI-th order statistic (zero based).
			return newPivotI;
		}
		static void swap_elems(this double[] list, int i, int j) {
			double tmp = list[i];
			list[i] = list[j];
			list[j] = tmp;
		}

		public static double SlowSelect(double[] list, int k) {
			Array.Sort(list);
			return list[k];
		}
		public static double SlowSelect(double[] list, int k, int startI, int endI) {
			Array.Sort(list, startI, endI - startI);
			return list[k];
		}

		public static double[] AsSorted(this double[] arr) {
			var retval = (double[])arr.Clone();
			Array.Sort(retval);
			return retval;
		}

		[TestFixture]
		class SelectTest {
			const int MaxSize = 2000000;
			IEnumerable<int> Sizes() {
				for (int i = 1; i < MaxSize; i = (int)(i * 1.1) + 1)
					yield return i;
			}

			[Test, TestCaseSource("Sizes")]
			public void RndTest(int size) {
				for (int i = 0; i < MaxSize / size; i++) {
					double[] list = Enumerable.Repeat(0, size).Select(x => RndHelper.ThreadLocalRandom.NextNormal()).ToArray();
					double[] listB = list.ToArray();
					int k = RndHelper.ThreadLocalRandom.Next(size);
					Assert.AreEqual(QuickSelect(list, k), SlowSelect(listB, k));
					Array.Sort(list);
					Assert.That(list, Is.EqualTo(listB));
				}
			}


			[Test]
			public void SpeedTest() {
				MersenneTwister rnd = RndHelper.ThreadLocalRandom;
				double[] list0 = Enumerable.Repeat(0, MaxSize).Select(x => rnd.NextNormal()).ToArray();
				int kf = rnd.Next();
				double ignoreQ = 0, ignoreS = 0;
				var listQ = list0.ToArray();
				var listS = list0.ToArray();
				foreach (int size in Sizes()) {
					double durationS_ms = DTimer.BenchmarkAction(() => { ignoreS += SlowSelect(listS, kf % size, 0, size); }, 10).TotalMilliseconds;
					double durationQ_ms = DTimer.BenchmarkAction(() => { ignoreQ += QuickSelect(listQ, kf % size, 0, size); }, 10).TotalMilliseconds;
					string details = "kf % size: " + kf % size + "\nsize: " + size;
					Assert.AreEqual(ignoreQ, ignoreS);
					Assert.LessOrEqual(durationQ_ms, durationS_ms, details);
					double scaling = 1 + 2 * (Math.Log(Math.E * Math.E * Math.E * Math.E + size) - 4);
					Assert.That(scaling, Is.GreaterThanOrEqualTo(1.0));
					Assert.LessOrEqual(durationQ_ms - TimeSpan.FromTicks(1).TotalMilliseconds, durationS_ms / scaling, "scaling: " + scaling + "\n" + details);
				}
			}
		}
	}
}
