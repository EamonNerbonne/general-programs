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
			//now [startI, newPivotI] are <= to pivotValue && list[newPivotI] contains storeI-th order statistic.
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

		[TestFixture]
		class SelectTest {
			const int MaxSize = 2000000;
			IEnumerable<int> Sizes() {
				for (int i = 1; i < MaxSize; i = (int)(i * 1.1) + 1)
					yield return i;
			}

			[Test, TestCaseSource("Sizes")]
			public void RndTest(int size) {
				double[] list = Enumerable.Repeat(0, size).Select(x => RndHelper.ThreadLocalRandom.NextNormal()).ToArray();
				int k = RndHelper.ThreadLocalRandom.Next(size);
				Assert.AreEqual(QuickSelect(list, k), SlowSelect(list, k));
			}


			[Test]
			public void SpeedTest() {
				var list0 = Enumerable.Repeat(0, MaxSize).Select(x => RndHelper.ThreadLocalRandom.NextNormal()).ToArray();
				//Array.Sort(list0);
				var list = list0.ToArray();
				int kf = RndHelper.ThreadLocalRandom.Next();
				double ignoreQ = 0, ignoreS = 0;
				TimeSpan durationQ = DTimer.TimeAction(() => {
					foreach (int size in Sizes())
						ignoreQ += QuickSelect(list, kf % size, 0, size);
				});
				list = list0.ToArray();
				TimeSpan durationS = DTimer.TimeAction(() => {
					foreach (int size in Sizes())
						ignoreS += SlowSelect(list, kf % size, 0, size);
				});
				Assert.AreEqual(ignoreQ, ignoreS);
				Assert.Greater(durationQ, durationS);
			}
		}
	}
}
