using System;
using System.IO;
using System.Web;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
namespace DynWeb.Anagram {
	/// <summary>
	/// Summary description for Anagrammer.
	/// </summary>
	public class Anagrammer : IHttpHandler {
		private static string sortstr(string input) {
			char[] wordarr = input.ToLower().ToCharArray();
			Array.Sort<char>(wordarr);
			return new string(wordarr);
		}

		private static int comparePair<U,V>(KeyValuePair<U, V> a, KeyValuePair<U,V> b) where U:IComparable<U> where V:IComparable<V>{
			int test1 = a.Key.CompareTo(b.Key);
			return test1 != 0 ? test1 : a.Value.CompareTo(b.Value);
		}

		class mycomparer : IComparer<KeyValuePair<int, string>> {
			public int Compare(KeyValuePair<int, string> x, KeyValuePair<int, string> y) {
				return comparePair<int, string>(x, y);
			}
		}

		public void ProcessRequest(HttpContext context) {
			string url = context.Server.MapPath(context.Request.Url.LocalPath);
			XmlTextWriter outp = new XmlTextWriter(context.Response.Output);
			context.Response.ContentType = "text/xml";
			outp.WriteStartDocument();
			outp.WriteProcessingInstruction("xml-stylesheet", "type=\"text/xsl\" href=\"../xsl/anagram.xsl\"");
			outp.WriteStartElement("matches");
			string query = context.Request.QueryString["word"];
			if (query != null) {

				Dictionary<string,List<string>> dict;
				List<KeyValuePair<int, string>> coolness;
				if (context.Cache[url] != null) dict = (Dictionary<string, List<string>>)context.Cache[url];
				else {
					TextReader reader = new StreamReader(new FileInfo(url).OpenRead());
					string word;
					dict = new Dictionary<string, List<string>>();
					while ((word = reader.ReadLine()) != null) {
						string keyword = sortstr(word);
						if (dict.ContainsKey(keyword)) dict[keyword].Add(word);
						else dict[keyword] = new List<string>(new string[] { word });
					}
					context.Cache[url] = dict;

					coolness = new List<KeyValuePair<int,string>>();
					foreach (KeyValuePair<string, List<string>> pair in dict) {
						if (pair.Value.Count > 1) {
							coolness.Add(new KeyValuePair<int,string>((pair.Key.Length-1) * (pair.Value.Count-1), pair.Key));
						}
					}
					coolness.Sort(comparePair<int,string>);

					int total = 0;
					for (int i = 0; i < coolness.Count; i++) {
						total+=(coolness[i].Key*coolness[i].Key*coolness[i].Key+9)/10;
						coolness[i] = new KeyValuePair<int, string>(total, coolness[i].Value);
					}
					context.Cache[url + "\\COOLNESS"] = coolness;
				}

				coolness = (List<KeyValuePair<int, string>>)context.Cache[url + "\\COOLNESS"];
				int maxcool = coolness[coolness.Count - 1].Key;
				int suggestion = (int)(new Random().NextDouble() * maxcool);
				int index = coolness.BinarySearch(new KeyValuePair<int, string>(suggestion, "zzzz"), new mycomparer());
				if (index < 0) index = ~index;
				outp.WriteAttributeString("try", dict[coolness[index].Value][0]);

				query = sortstr(query.Replace(" ", ""));
				outp.WriteAttributeString("dictsize", dict.Count.ToString());
				if(dict.ContainsKey(query))
					foreach(string word in dict[query])
						outp.WriteElementString("match", word);

			}
			outp.WriteEndElement();
			outp.WriteEndDocument();
		}

		public bool IsReusable {
			get { return true; }
		}
	}
}
