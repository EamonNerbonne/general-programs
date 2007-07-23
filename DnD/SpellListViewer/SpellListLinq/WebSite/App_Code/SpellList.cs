using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EamonExtensionsLinq.Text;

public class SpellList
{
    public static SpellList Unique
    {
        get
        {

            if (HttpContext.Current.Application["SpellList"] as SpellList != null) {
                return (SpellList)HttpContext.Current.Application["SpellList"];
            }
            else {
                SpellList retval = new SpellList(HttpContext.Current);
                HttpContext.Current.Application["SpellList"] = retval;
                return retval;
            }
        }
    }


    static string[] xmlDBpaths = new string[] { "App_Data\\SpellCompendiumOCR.xml", "App_Data\\ScrapedSRD.xml", "App_Data\\EamonPHB2.xml" };//most significant last
    XmlDocument xmldoc;
    Dictionary<string, XmlElement> spellLookup;
    private SpellList(HttpContext context)
    {
        spellLookup = new Dictionary<string, XmlElement>();
        foreach (string curpath in xmlDBpaths) {
            string spelllistPath = Path.Combine(context.Request.PhysicalApplicationPath, curpath);
            xmldoc = new XmlDocument();
            xmldoc.Load(spelllistPath);
            foreach (XmlElement elem in xmldoc.SelectNodes("//div[@spell]")) {
                spellLookup[Canonicalize.Basic(elem.GetAttribute("spell"))] = elem;
            }
        }
    }

    public XmlElement this[string name] { get { return spellLookup[Canonicalize.Basic(name)]; } }
    public bool Contains(string name) { return spellLookup.ContainsKey(Canonicalize.Basic(name)); }
    public IEnumerable<string> Spells { get { return spellLookup.Keys; } }
    public string SpellsAsText { get { return string.Join("; ", Spells.OrderBy(s => s).ToArray()) + "."; } }
}
