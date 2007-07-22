using System;
using System.Data;
using System.Configuration;
using System.Collections.Generic;
using System.Xml.XLinq;
using System.Query;
using WebpageScraper;
/// <summary>
/// Summary description for SpellTextSource
/// </summary>
namespace WebpageScraper {
    public class SpellTextSource {
        const string baseuri = "http://www.d20srd.org/indexes/spells.htm";
        const string basefilter = "http://www.d20srd.org/srd/spells/";

        public Dictionary<string, Uri> name2uri = new Dictionary<string, Uri>();
        public SpellTextSource() {
            Uri spelllist = new Uri(baseuri);
            WebpageScrape scraper = new WebpageScrape(spelllist);
            XDocument doc = scraper.XDocument;
            XNamespace ns = doc.Root.GetDefaultNamespace();
            foreach (XElement el in doc.Descendants(ns + "a")) {
                Uri href = new Uri(spelllist,el.Attribute("href").Value);
                if (href.ToString().StartsWith(basefilter)) {
                    name2uri[el.Value] = href;
                }
            }
        }

        public XElement GetSpellDescription(string spellname) {
            Uri spellpage = name2uri[spellname];
            if (spellpage == null)
                return null;
            WebpageScrape scraper = new WebpageScrape(spellpage);
            XDocument doc = scraper.XDocument;
            XNamespace ns = doc.Root.GetDefaultNamespace();
            return new XElement(ns+"div",new XAttribute("spell", spellname),doc.Element(ns+"html").Element(ns+"body").Elements()
                .SkipWhile(elem => elem.Name.LocalName != "h1")
                .TakeWhile(elem => !(elem.Name.LocalName == "div" && ((string)elem.Attribute("class")).Contains("footer"))));
        }
    }
}