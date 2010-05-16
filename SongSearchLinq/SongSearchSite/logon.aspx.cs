using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.Security;

namespace SongSearchSite {
	public partial class logon : System.Web.UI.Page {
		protected void Page_Load(object sender, EventArgs e) {

		}
		protected void Submit_OnClick(object sender, EventArgs e) {
			if (MyCustomMethod(txtUserName.Text, txtPassword.Text))
				FormsAuthentication.RedirectFromLoginPage(txtUserName.Text, false);
			else   //' Invalid credentials supplied, display message
				lblMessage.Text = "Invalid login credentials";

		}

		static HashSet<string> users = new HashSet<string> {
			"eamon","emn13","el_martian","auke","bart"
		};
		protected bool MyCustomMethod(string strUsername, string strPassword) {
			return users.Contains(strUsername) && strPassword == "music";
		}
	}
}