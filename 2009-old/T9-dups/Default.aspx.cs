using System;
using MoreLinq;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.IO;
using System.Globalization;
using System.Text;

public partial class _Default : System.Web.UI.Page
{
	static T9Lookup eng1dict;
	static T9Lookup ned1dict;
	static T9Lookup eng2dict;


	protected void Page_Load(object sender, EventArgs e) {

		if (eng1dict == null) {
			eng1dict = new T9Lookup(Server.MapPath("App_Data/english-words.ngl"));
			ned1dict = new T9Lookup(Server.MapPath("App_Data/nederlands.ngl"));
			eng2dict = new T9Lookup(Server.MapPath("App_Data/354984si.ngl"));
		}

		string word = SourceWord.Text;
		rawt9code.Text = T9Lookup.T9digits(word);
		if (string.IsNullOrEmpty(word)) {
			eng1.DataSource = eng2.DataSource = ned1.DataSource = null;
		} else {
			ned1.DataSource = ned1dict.T9Matches(word);
			eng1.DataSource = eng1dict.T9Matches(word);
			eng2.DataSource = eng2dict.T9Matches(word);
		}
		ned1.DataBind();
		eng1.DataBind();
		eng2.DataBind();
	}
}


public class T9Lookup
{
	object sync = new object();
	public static string T9digits(string word) {
		char[] canonicalized = word.ToLowerInvariant().Normalize(NormalizationForm.FormD).AsEnumerable().Where(c => char.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray();
		if (!canonicalized.All(c => c >= 'a' && c <= 'z' || c >= '0' && c <= '9'))
			return null; //no good.
		else //only a-z!
			return new string(canonicalized.Select(c => c >= '0' && c <= '9' ? c : c == 'z' ? '9' : (char)(((int)c - (int)'a') / 3 + (int)'2')).ToArray());
	}
	ILookup<string, string> wordByT9;

	public T9Lookup(string dictPath) {
		lock (sync) {
			wordByT9 = (from word in File.ReadAllLines(dictPath)
						let t9ver = T9digits(word)
						where t9ver != null
						select new { Word = word, T9 = t9ver }).ToLookup(w => w.T9, w => w.Word);
		}
	}
	public IEnumerable<string> T9Matches(string word) {
		var t9ver = T9digits(word);
		if (t9ver == null)
			return Enumerable.Empty<string>();
		else
			lock (sync)
				return wordByT9[t9ver].ToArray();
	}
}