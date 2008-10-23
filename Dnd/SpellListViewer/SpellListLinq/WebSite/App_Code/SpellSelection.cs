using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Collections.Generic;
/// <summary>
/// Summary description for SpellSelection
/// </summary>
public class SpellSelection
{
    public class Section {
        public string name;
        public string[] spells;
    }
    public List<Section> sections = new List<Section>();
	public SpellSelection(SpellList list,string selection)
	{
        foreach (string section in selection.Split('.')) {
            string sectionTrim = section.Trim();
            if (sectionTrim.Length == 0)
                continue;
            Section ss = new Section();
            string[] split = sectionTrim.Split(':');
            ss.spells = split.Length > 1 ? split[1].Split(';') :split[0].Split(';');
            ss.name = split.Length > 1 ? split[0]: "Unnamed spell list";
            for (int i = 0; i < ss.spells.Length; i++)
                ss.spells[i] = ss.spells[i].Trim();
            Array.Sort(ss.spells);
            sections.Add(ss);
        }
	}
}
