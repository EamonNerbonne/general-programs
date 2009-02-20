using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmnExtensions.MathHelpers;

namespace EmnExtensions.Algorithms
{
    public static class ShuffleAlgorithm
    {
        public static void Shuffle<T>(this IList<T> arrayToShuffle) {//array's are ILists
			Random r = RndHelper.ThreadLocalRandom;
            for (int i = arrayToShuffle.Count - 1; i > 0; i--) {
                T tmp;
                int rndIndex = r.Next(i+1);
//                if(rndIndex==i)continue;//this optimization probably doesn't speed things up.
                tmp = arrayToShuffle[i];
                arrayToShuffle[i] = arrayToShuffle[rndIndex];
                arrayToShuffle[rndIndex] = tmp;
            }
        }
    }
}
