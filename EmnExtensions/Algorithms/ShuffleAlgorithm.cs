using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EamonExtensionsLinq.Algorithms
{
    public static class ShuffleAlgorithm
    {
        static Random r = new Random((int)(DateTime.Now.Ticks / 50));//we divide by 50 since the windows default timer isn't actually that accurate anyhow.
        public static void Shuffle<T>(this IList<T> arrayToShuffle) {//array's are ILists

            for (int i = arrayToShuffle.Count - 1; i > 0; i--) {
                T tmp;
                int rndIndex = r.Next(i+1);
//                if(rndIndex==i)continue;//optimization probably doesn't speed things up.
                tmp = arrayToShuffle[i];
                arrayToShuffle[i] = arrayToShuffle[rndIndex];
                arrayToShuffle[rndIndex] = tmp;
            }
        }
    }
}
