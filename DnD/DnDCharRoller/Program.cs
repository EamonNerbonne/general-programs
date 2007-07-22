using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DndStochasticStats
{
    class Program
    {
        public static Random r = new Random();
        public static int makeStat()
        {
            var rolls = Enumerable.Repeat(0, 4).Select(n => r.Next(1,7)).ToArray();
            var lowest = rolls.Min();
            return rolls.Sum() - lowest;
        }

        static int valueOf(int stat)
        {
            if (stat >= 16) return (stat - 16) * 3 + 10;
            if (stat >= 14) return (stat - 14) * 2 + 6;
            if (stat >=6) return stat - 8;
            return (stat - 6) * 3 + -2;
        }
        static int modOf(int stat)
        {
            return (stat - 10) / 2;
        }

        static int[] makeStats()
        {
            return Enumerable.Repeat(0, 6).Select(n => makeStat()).ToArray();
        }

        static bool statsOK(int[] stats)
        {
            if (stats.Max() > 13 && stats.Sum(n => modOf(n)) > 1)
                return true;
            else
                return false;
        }

        
        static int commonerLevel()
        {
            return Enumerable.Repeat(0, 6).Select(n => r.Next(1, 7)).OrderBy(n => n).Take(3).Sum() - 3;
        }
        static void Main(string[] args)
        {
                foreach(var g in Enumerable.Repeat(0,1000).Select(n=>commonerLevel()).Where(lev=>lev>0).Take(102).GroupBy(lev=>lev).OrderBy(group=>group.Key)) {
                    Console.WriteLine("Commoner Level: " + g.Key + "; Count = " + g.Count());
                }
            Console.WriteLine(Enumerable.Repeat(0, 1000000).Select(n => makeStats()).Where(statsOK).Select(stats => stats.Select(stat => valueOf(stat)).Sum()).Average());
            Console.WriteLine(Enumerable.Repeat(0, 100000).Select(n => makeStat()).Average());
            Console.WriteLine(Enumerable.Repeat(0, 100000).Select(n => valueOf(makeStat())).Average()*6);
            Console.WriteLine(Enumerable.Repeat(0, 100000).Select(n => valueOf(makeStat())).Average() * 6);
            Console.ReadLine();


        }
    }
}
