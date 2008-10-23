using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;



namespace DnDCharRoller
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>

    public partial class Window1 : System.Windows.Window
    {

        public static void Main(string[] args)
        {

        }
        Random r =new Random();
        public Window1()
        {
            InitializeComponent();

            Stats.Content = CalcStats();
            rerollButton.Click += delegate(object sender, RoutedEventArgs args) { Stats.Content = CalcStats(); };
            clipset.Click += delegate(object sender, RoutedEventArgs args) { System.Windows.Clipboard.SetText(Stats.Content.ToString()); };
            
        }            

        public int makeStat()
        {
            var rolls = Enumerable.Repeat(0, 4).Select(n => r.Next()%6+1).ToArray();
            var lowest = rolls.Min();
            return  rolls.Sum() - lowest;
        }

        public string CalcStats()
        {
            var stats = Enumerable.Repeat(0,6).Select(n => makeStat()).ToArray();
            var lowest = stats.Min();
            var lows = stats.Select((stat, pos) => new { Stat = stat, Pos = pos }).Where(p => p.Stat == lowest).ToArray();
            int lowCount = lows.Count();
            int choice = r.Next() % lowCount;
            stats[lows[choice].Pos] = Math.Max(stats[lows[choice].Pos], makeStat());

            var statsNames = new string[] { "Str: ", "Dex: ", "Con: ", "Int: ", "Wis: ", "Cha: " };
            return statsNames.Select((name, pos) => name + stats[pos] + "\n").Aggregate((a, b) => a + b);
        }
    }
}