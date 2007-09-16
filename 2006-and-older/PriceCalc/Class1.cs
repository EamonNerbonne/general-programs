using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
namespace PriceCalc {
    struct Drive:IComparable {
        public double gb;
        public double price;
        public string name;
        public Drive(double ngb,double nprice, string nname) {gb=ngb;price=nprice;name=nname;}
        public int CompareTo(object obj) {
            Drive other=(Drive)obj;
            return (gb/price).CompareTo(other.gb/other.price);
        }
    }
    /// <summary>
    /// Does a quick calc and sort of pure GB/EUR, best at bottom and prints that list.  Understands Ebug HTML.
    /// </summary>
    class Class1 {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args) {
            string pricepre="<TD width=\"55\" class=\"artlistpreis\" nowrap align=right valign=top>";
            string content;
            ArrayList nm=new ArrayList();
            foreach(Match m in new Regex("[0-9]+(\\.[0-9]*)?GB IDE").Matches(content=new FileInfo(args[0]).OpenText().ReadToEnd())) {
                double gb=double.Parse(content.Substring(m.Index,m.Length-6));
                string name=content.Substring(m.Index,content.IndexOf("<BR>",m.Index)-m.Index);
                int index=content.IndexOf(pricepre,m.Index);
                double price=double.Parse(content.Substring(index+pricepre.Length,content.IndexOf("<BR>",index)-index-pricepre.Length));
                nm.Add(new Drive(gb,price,name));
            }
            nm.Sort();
            foreach(Drive drv in nm) {
                Console.WriteLine(drv.name+" "+drv.gb+"GB, "+drv.price+"EUR");
            }
        }
    }
}
