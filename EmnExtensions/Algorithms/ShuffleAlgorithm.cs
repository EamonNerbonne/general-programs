using System;
using System.Collections.Generic;
using EmnExtensions.MathHelpers;

namespace EmnExtensions.Algorithms
{
    public static class ShuffleAlgorithm
    {
        public static void Shuffle<T>(this IList<T> arrayToShuffle)
        {//array's are ILists
            Random r = RndHelper.ThreadLocalRandom;
            for (var i = arrayToShuffle.Count - 1; i > 0; i--) {
                T tmp;
                var rndIndex = r.Next(i + 1);
                //                if(rndIndex==i)continue;//this optimization probably doesn't speed things up.
                tmp = arrayToShuffle[i];
                arrayToShuffle[i] = arrayToShuffle[rndIndex];
                arrayToShuffle[rndIndex] = tmp;
            }
        }
    }
}
