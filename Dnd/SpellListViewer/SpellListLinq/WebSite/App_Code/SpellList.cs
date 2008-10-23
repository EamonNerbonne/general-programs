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
    Dictionary<string, Dictionary<int, List<string>>> casterclasslists = new Dictionary<string, Dictionary<int, List<string>>>();

    void AddSpell(string casterclass, int level, string name)
    {
        if (!casterclasslists.ContainsKey(casterclass)) casterclasslists.Add(casterclass, new Dictionary<int, List<string>>());
        Dictionary<int, List<string>> levels = casterclasslists[casterclass];
        if (!levels.ContainsKey(level)) levels.Add(level, new List<string>());
        List<string> spells = levels[level];
        spells.Add(name);
    }
    Regex levelMatch = new Regex("^("+ string.Join("|",classes)+ "|([,\\s]+))+$", RegexOptions.IgnoreCase | RegexOptions.Compiled|RegexOptions.ExplicitCapture);

    static List<string> classnames=new List<string>();
    static string cm(string classname) { return cm(classname, classname); }
    static string cm(string classname, string match)    {   classnames.Add(classname);  return "(("+match+")\\s+(?<"+classname+">[0-9]+))";    }
    static string[] classes = new string[] { cm("Assassin"),  cm("Bard","Bard|Brd"),cm("Blackguard"), cm("Cleric","Cleric|Clr"),  cm("Druid","Druid|Drd"), cm("Paladin","Paladin|Pal"), cm("Ranger","Ranger|Rgr"),cm("Wizard", "Sorcerer/Wizard|Wizard|Sor/Wiz|Wiz"),
                                  cm("Domain",@"(?<DomainName>Abyss|Hunger|Gluttony|Portal|Spell|Windstorm|Darkness|Death Bound|Force|Celerity|Glory|Madness|Mind|Mechanus|Purification|Greed|Balance|Courage|Drow|Pestilence|Pact|Dragon|Dream|Craft|Gnome|Trade|Celestia|Ocean|Limbo|Hades|Elysium|Cavern|Domination|Moon|Cold|Liberation|Arborea|Creation|Wrath|Spider|Mysticism|Competition|Water|Good|Luck|Air|Animal|Death|Chaos|Law|Plant|Magic|Protection|Travel|War|Evil|Strength|Fire|Knowledge|Trickery|Destruction|Healing|Earth|Sun)")};

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
                if (canonSpellName == "greater (spell name)" || canonSpellName == "legion's (spell name)" || canonSpellName == "lesser (spell name)" || canonSpellName == "greater (spell name)" || canonSpellName == "mass (spell name)" || canonSpellName == "swift (spell name)") continue;
                string levels = elem.SelectSingleNode("table[@class='statBlock']/tr/th/text()[contains(.,'Level')]/../../td/text()").Value;

					 Match match = levelMatch.Match(levels);
					 if(!match.Success)
                    fail++;
                else
                    success++;

                int classcount = 0;

                foreach (string classname in classnames) {
                    if(classname=="Domain")continue;
                    if (match.Groups[classname].Captures.Count == 1) { AddSpell(classname, Int32.Parse(match.Groups[classname].Value), canonSpellName); classcount++; }
                    else if (match.Groups[classname].Captures.Count > 1) throw new Exception("Twice on same spell list?");
                }
                int domainCount = match.Groups["Domain"].Captures.Count;
                if(domainCount!= match.Groups["DomainName"].Captures.Count) throw new Exception("Impossible: inconsisten domains in '"+levels+"'");
                for (int di = 0; di < domainCount; di++) {
                    string classname = "Domain/" + match.Groups["DomainName"].Captures[di].Value; 
                    AddSpell(classname, Int32.Parse(match.Groups["Domain"].Captures[di].Value), canonSpellName); 
                }

                if (domainCount == 0 && classcount == 0) throw new Exception("Impossible: A spell without any casting classes");
                spellLookup[canonSpellName] = elem;

            }
        }
    }

    public string SpellListByClass(string caster)
    {
        StringBuilder sb = new StringBuilder();
        if (!casterclasslists.ContainsKey(caster)) throw new Exception("Unknown Caster");
        var spellistDict = casterclasslists[caster];
        foreach (int level in spellistDict.Keys.OrderBy(lev => lev)) {
            sb.AppendLine(caster + " " + levStr(level) + ":");
            var spells = spellistDict[level];
            sb.Append(string.Join("; ", spells.ToArray()));
            sb.AppendLine(".\n");
        }
        return sb.ToString();
    }
    static string levStr(int level)
    {
        switch (level) {
            case 1: return "1st";
            case 2: return "2nd";
            case 3: return "3rd";
            default: return level.ToString() + "th";
        }
    }


    public XmlElement this[string name] { get { return spellLookup[Canonicalize.Basic(name)]; } }
    public bool Contains(string name) { return spellLookup.ContainsKey(Canonicalize.Basic(name)); }
    public IEnumerable<string> Spells { get { return spellLookup.Keys; } }
    public string SpellsAsText { get { return string.Join("; ", Spells.OrderBy(s => s).ToArray()) + "."; } }

    public string GetDomainSpellLists()
    {
        StringBuilder sb = new StringBuilder();
        foreach (string domainname in casterclasslists.Keys) {
            if (!domainname.StartsWith("Domain/")) continue;//is normal caster list;
            if (casterclasslists[domainname].Count < 9) continue;//isn't complete;
            sb.AppendLine(domainname.Substring("Domain/".Length) + ":");
            sb.Append(string.Join("; ", (from level in casterclasslists[domainname].Keys orderby level from spellname in casterclasslists[domainname][level] select spellname).ToArray()));
            sb.AppendLine(".\n");
        }
        return sb.ToString();
    }
}
