using System;
//using System.Data;
//using System.Data.SqlClient;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Collections.Generic;
using System.IO;

public partial class _Default : System.Web.UI.Page {

    protected void Page_Load(object sender, EventArgs e) {
        SpellBox.Text = Request.Form["SpellBox"];

        SpellListLabel.Text = SpellList.Unique.SpellsAsText;
    }
    protected void sylvia4_Click(object sender, EventArgs e) {
        SpellBox.Text = File.OpenText(Path.Combine(
            Context.Request.PhysicalApplicationPath, "App_Data\\sylvia4.txt")).ReadToEnd();
    }
    protected void SpellBox_TextChanged(object sender, EventArgs e) {

    }
    protected void GenerateButton_Click(object sender, EventArgs e) {
    }
    protected void sylvia6_Click(object sender, EventArgs e) {
        SpellBox.Text = File.OpenText(Path.Combine(
            Context.Request.PhysicalApplicationPath, "App_Data\\sylvia6.txt")).ReadToEnd();

    }
    protected void NormalCasterLink_Click(object sender, EventArgs e)
    {
        string casterclass = ((LinkButton)sender).Text;
        SpellBox.Text = SpellList.Unique.SpellListByClass(casterclass);
    }
}
