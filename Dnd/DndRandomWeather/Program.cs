using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DndRandomWeather
{

    /// <summary>
    /// This program currently for use with the Red Hand of Doom
    /// </summary>
    class Program
    {
        static Random r=new Random();
        static void Main(string[] args)
        {
            Console.WriteLine("Weather:");
            var weather = from roll in d100() select LookupWarmWeather(AssertValidRoll(roll));
            foreach (string w in weather.Take(100))
            {
                Console.WriteLine(w);
            }
        }

        public static IEnumerable<int> d100() {
            while(true) {
                yield return r.Next(1, 101);
            }
        }

        public static int AssertValidRoll(int roll)
        {
            if (roll <= 0 || roll > 100)
                throw new ArgumentException("A random roll should be in range 1-100, not " + roll);
            return roll;
        }
        public static string LookupWarmWeather(int roll)
        {
            if (roll <= 70) return "Dry 36 or 30-42";
            if (roll <= 75) return "Dry 30 or 24-36 (cooler)";
            if (roll <= 80) return "Dry 43 or 36-49 (hotter)";
            if (roll <= 83) return "Foggy/Hazy 37";
            if (roll <= 89) return "Rainy/Humid 33";
            if (roll <= 90) return "Hail/Rain 35";
            if (roll <= 99) return "Severe Thunderstorm 30";
            if (roll <= 100) return "Tornado/storm 20";
            throw new Exception("Should be impossible, invalid input");
        }
    }
}
