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
using System.Text.RegularExpressions;

public class SpellList
{
    enum CasterClass {
        Wizard,Cleric,Paladin,Ranger,Assassin
    }

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
    Dictionary<string, XmlElement> spellLookup = new Dictionary<string,XmlElement>();
    Dictionary<CasterClass, Dictionary<int, List<string>>> lists = new Dictionary<CasterClass, Dictionary<int, List<string>>>();

    void AddSpell(CasterClass cc, int level, string name)
    {
        if (!lists.ContainsKey(cc)) lists.Add(cc, new Dictionary<int, List<string>>());
        Dictionary<int, List<string>> levels = lists[cc];
        if (!levels.ContainsKey(level)) levels.Add(level, new List<string>());
        List<string> spells = levels[level];
        spells.Add(name);
    }
    Regex levelMatch = new Regex("^("+ string.Join("|",classes)+ "|([,\\s]+|(Branch.+Monkey)|(The kelp.+its target)))+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    static string cm(string classname) { return cm(classname, classname); }
    static string cm(string classname, string match)    {        return "("+match+"\\s+(?<"+classname+">[0-9]+))";    }
    static string[] classes = new string[] { cm("Assassin"), cm("Wizard", "Sorcerer/Wizard"), cm("Cleric"), cm("Bard"), cm("Ranger"), cm("Druid"), cm("Paladin"), 
                                  cm("Blackguard"),cm("Domain",@"(Abyss|Hunger|Gluttony|Portal|Spell|Windstorm|Darkness|Death\s+Bound|Force|Celerity|Glory|Madness|Mind|Mechanus|Purification|Greed|Balance|Courage|Drow|Pestilence|Pact|Dragon|Dream|Craft|Gnome|Trade|Celestia|Ocean|Limbo|Hades|Elysium|Cavern)")};

    private SpellList(HttpContext context)
    {
        int fail = 0;
        int success = 0;
        foreach (string curpath in xmlDBpaths) {
            string spelllistPath = Path.Combine(context.Request.PhysicalApplicationPath, curpath);
            var xmldoc = new XmlDocument();
            xmldoc.Load(spelllistPath);
            foreach (XmlElement elem in xmldoc.SelectNodes("//div[@spell]")) {
                string canonSpellName = Canonicalize.Basic(elem.GetAttribute("spell"));
                if (canonSpellName == "greater (spell name)"||canonSpellName=="legion's (spell name)"||canonSpellName=="lesser (spell name)"||canonSpellName=="greater (spell name)"||canonSpellName=="mass (spell name)") continue;
                string levels = elem.SelectSingleNode("table[@class='statBlock']/tr/th/text()[contains(.,'Level')]/../../td/text()").Value;

                if (!levelMatch.Match(levels).Success)
                    fail++;
                else
                    success++;

                string wizard = levelMatch.Match(levels).Groups["Wizard"].Value;
                string cleric = levelMatch.Match(levels).Groups["Cleric"].Value;
                string paladin = levelMatch.Match(levels).Groups["Paladin"].Value;
                string ranger = levelMatch.Match(levels).Groups["Ranger"].Value;
                string assassin = levelMatch.Match(levels).Groups["Assassin"].Value;
                string bard=levelMatch.Match(levels).Groups["Bard"].Value;
                string druid = levelMatch.Match(levels).Groups["Druid"].Value;

                spellLookup[canonSpellName] = elem;

            }
        }
    }

    public XmlElement this[string name] { get { return spellLookup[Canonicalize.Basic(name)]; } }
    public bool Contains(string name) { return spellLookup.ContainsKey(Canonicalize.Basic(name)); }
    public IEnumerable<string> Spells { get { return spellLookup.Keys; } }
    public string SpellsAsText { get { return string.Join("; ", Spells.OrderBy(s => s).ToArray()) + "."; } }
}
