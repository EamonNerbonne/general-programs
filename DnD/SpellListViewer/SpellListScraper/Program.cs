using System;
using System.Collections.Generic;
using System.Text;
using System.Query;
using System.Xml.XLinq;
using System.Data.DLinq;
using WebpageScraper;

namespace LINQConsoleApplication1 {
    class Program {
        static void Main(string[] args) {
            List<XElement> divs = new List<XElement>();
            SpellTextSource sts = new SpellTextSource();
            foreach (string key in sts.name2uri.Keys) {
                Console.Write(key + ": ");
                divs.Add(sts.GetSpellDescription(key));
                Console.WriteLine("done.");
            }
            XNamespace ns = divs[0].GetDefaultNamespace();
            new XElement(ns + "html", new XElement(ns + "body", divs)).Save("spells.xml");
        }
    }
}
