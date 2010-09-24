using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmnExtensions.Algorithms {
	public static class QuickSelect {
		public static double QuickSelect(double[] values) {

		}
		static void swap(this double[] list, int i, int j) {
			double tmp = list[i];
			list[i] = list[j];
			list[j] = tmp;
		}
		static int partition(double[] list, int startI, int endI, int pivotI) {
			double pivotValue = list[pivotI];
			list[pivotI] = list[startI];
			list[startI] = pivotValue;

			int storeI = startI+1;//no need to store @ pivot item.
			//while (storeI < endI && list[storeI] <= pivotValue) storeI++;
			for (int i = storeI; i < endI; ++i) //no need to compare pivot item to itself, 
				if (list[i] <= pivotValue)
				{
					list.swap(i, storeI);
					++storeI;
				}
			//now [startI, storeI) are <= to pivotValue.
			return storeI;
		}

		static double select(double[] list, int startI, int endI, int k) {
			while (true) {
				//assume endI>startI
				int pivotI = (startI + endI) / 2;//arbitrary, but good if sorted, and doesn't pick first element unnecessarily.
				int nextPivotI = partition(list, startI, endI, pivotI);
				if (k == nextPivotI)
					return list[k];
				else if (k < nextPivotI) {
					return select(list, startI, nextPivotI, k);
				} else
					return select(list, nextPivotI, endI, k);
			}
		}
		//    function select(list, left, right, k)
		//select pivotIndex between left and right
		//pivotNewIndex := partition(list, left, right, pivotIndex)
		//if k = pivotNewIndex
		//    return list[k]
		//else if k < pivotNewIndex
		//    return select(list, left, pivotNewIndex-1, k)
		//else
		//    return select(list, pivotNewIndex+1, right, k)
		//   static 
	}
}
