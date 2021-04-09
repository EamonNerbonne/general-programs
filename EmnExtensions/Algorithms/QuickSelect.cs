using System;
using System.Text;

namespace EmnExtensions.Algorithms {
    public static class SelectionAlgorithm {
        public static double Median(double[] list) {
            if (list.Length == 0) return double.NaN;
            else if (list.Length % 2 == 0) return 0.5 * (QuickSelect(list, list.Length / 2 - 1) + QuickSelect(list, list.Length / 2, list.Length / 2 , list.Length));
            else return QuickSelect(list, (list.Length - 1) / 2);
        }

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
            //when this returns, all elements of list[i] <= list[k] for i <= k
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
    }
}
