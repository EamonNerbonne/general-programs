using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Text;
public partial class Validate : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        bool allOK = true;
        int okCount = 0;
        string spellListText;
        Session["SpellBox"] = spellListText=Request.Form["SpellBox"];
        SpellList sl = SpellList.Unique;

        SpellSelection ss = new SpellSelection(sl, spellListText);
        StringBuilder sb = new StringBuilder();
        sb.Append("<ul>");
        foreach (SpellSelection.Section s in ss.sections) {
            sb.Append("<li>"+Server.HtmlEncode( s.name)+":");
            sb.Append("<ul>");
            foreach(string spellname in s.spells) {
                sb.Append("<li>");
                if(!sl.Contains(spellname)) {
                    allOK = false;
                    sb.Append("<i>"+Server.HtmlEncode(spellname)+"</i>");
                }else {
                    okCount++;
                    sb.Append(Server.HtmlEncode(spellname));
                }
                sb.Append("</li>");
            }
            sb.Append("</ul>");
            sb.Append("</li>");
        }
        sb.Append("</ul>");
        if (allOK && okCount > 0) Context.Response.Redirect("SpellList.ashx", true);
        SpellListHTML.InnerHtml=sb.ToString();
    }
}
