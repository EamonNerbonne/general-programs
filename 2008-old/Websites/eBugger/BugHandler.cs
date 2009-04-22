using System;
using System.Collections;
using System.Web;
using System.Web.UI;
using System.Xml;
//using System.Xml.XPath;
using System.Xml.Xsl;
using System.Net;
using System.IO;
//using System.Text;
using System.Text.RegularExpressions;

namespace eBugger {
	public class BugHandler : IHttpHandler {

		public bool IsReusable{get{return true;}}
		
		class GenCol {
			public string name;
			//public int reci;
		}

		/*class ItemCol:GenCol {
			public char type;
			public override string ToString() {return type.ToString() +name;}
		}*/
		
		class DivCol:GenCol {
			public int a, b;
		}
		
		class NodeType:IComparer {
			string label,pattern;
			int sortby;

			Regex regex,titleregex;
			GenCol[] cols;
			Hashtable indexByName=new Hashtable();
			ArrayList records;//contains arrays of objects
			ArrayList kids;
			public NodeType(XmlElement xe) {
				label=xe.Attributes["label"].Value;
				if(xe.HasAttribute("regex")){
					pattern=xe.Attributes["regex"].Value;
					regex=new Regex(pattern,RegexOptions.ExplicitCapture | RegexOptions.Compiled);
					string[] names = regex.GetGroupNames(), exprs=xe.HasAttribute("cols")?xe.Attributes["cols"].Value.Split(','):new string[0];
					cols=new GenCol[names.Length+exprs.Length-1];
					int index=1;
					for(int i =1; i<names.Length;i++) {
						GenCol col=new GenCol();
						col.name=names[i];
						int reci=("sMain"==col.name)?0:(index++);
						indexByName.Add(col.name,reci);
						cols[reci]=col;
					}
					if(xe.HasAttribute("title")){
						titleregex=new Regex(xe.Attributes["title"].Value,RegexOptions.Compiled);
					}
					for(int i=0;i<exprs.Length;i++) {
						int ix=exprs[i].IndexOf('/');
						DivCol col=new DivCol();
						col.name=exprs[i].Trim();
						col.a=(int)indexByName["n"+exprs[i].Substring(0,ix).Trim()];
						col.b=(int)indexByName["n"+exprs[i].Substring(ix+1).Trim()];
						indexByName.Add(col.name,index);
						cols[index++]=col;
					}
					sortby=(int)indexByName[xe.Attributes["sort"].Value];
					records=new ArrayList();
				}
				kids=new ArrayList();
				foreach(XmlElement kid in xe.SelectNodes("node")) kids.Add(new NodeType(kid));
			}

			public bool Match(string line) {
				if(titleregex!=null && titleregex.Match(line).Success) {label=line; return true;}
				foreach(NodeType nt in kids) if(nt.Match(line)) return true;
				if(regex==null) return false;

				Match m=regex.Match(line);

				if(!m.Success) return false;

				IComparable[] rec=new IComparable[cols.Length];
				for(int i=0;i<cols.Length;i++)
					if(cols[i] is DivCol){//cols[i] is DivCol
						DivCol col=(DivCol)cols[i];
						rec[i]=((double)rec[col.a]/(double)rec[col.b]);
					} else {
						try{
							GenCol col=cols[i];
							switch(col.name[0]) {
								case 's':	rec[i]=m.Groups[col.name].Value; break;
								case 'n':rec[i]=double.Parse(m.Groups[col.name].Value); break;
							}
						} catch(Exception e) {
							throw new Exception("Error in line: "+line+"\nPattern: "+pattern+"\nName: "+cols[i].name+"\nValue: "+m.Groups[cols[i].name].Value,e);
						}
					}
				records.Add(rec);
				return true;
			}

			public void WriteOut(XmlWriter xw) {
				xw.WriteStartElement("node");//node for this nodetype
				xw.WriteStartElement("col");//node for this nodetype
				xw.WriteAttributeString("type","string");
				if(sortby==0) xw.WriteAttributeString("sortprior","1");
				xw.WriteString(label);
				xw.WriteEndElement();
				foreach(NodeType nt in kids) nt.WriteOut(xw);
				if(regex!=null  && records.Count!=0) {
					if(kids.Count!=0) {
						xw.WriteStartElement("node");//node for this nodetype
						xw.WriteStartElement("col");//node for this nodetype
						xw.WriteAttributeString("type","string");
						if(sortby==0) xw.WriteAttributeString("sortprior","1");
						xw.WriteString("(others)");
						xw.WriteEndElement();
					}

					for(int i=1; i<cols.Length; i++) {
						xw.WriteStartElement("col");//node for this nodetype
						xw.WriteAttributeString("type",(cols[i] is DivCol || cols[i].name[0]=='n')?"number":"string");
						if(sortby==i) xw.WriteAttributeString("sortprior","1");
						xw.WriteString((cols[i] is DivCol)?cols[i].name:cols[i].name.Substring(1));
						xw.WriteEndElement();
					}

					records.Sort(this);
					foreach(IComparable[] rec in records) {
						xw.WriteStartElement("leaf");//that record
						for(int i=0;i<rec.Length;i++) xw.WriteAttributeString("col"+i,cvt(rec[i]));
						xw.WriteEndElement();//end record
					}

					if(kids.Count!=0)	xw.WriteEndElement();//(others)
				}
				xw.WriteEndElement();
			}
			static string cvt(IComparable thing) 
			{
				if(thing is double) return ((double)thing).ToString("##########0.##");
				else return thing.ToString();
			}
			public int Compare(object x, object y) {
				return ((IComparable[])x)[sortby].CompareTo(((IComparable[])y)[sortby]);
			}
		}

		public void ProcessRequest(HttpContext context) {
			System.Threading.Thread.CurrentThread.CurrentCulture=new System.Globalization.CultureInfo("en-GB",false);

			//Load config-file
			XmlDocument conf=new XmlDocument();
			conf.Load(context.Server.MapPath(context.Request.CurrentExecutionFilePath));

			NodeType root=new NodeType(conf.DocumentElement);
			string[] preisliste = new StreamReader(WebRequest.Create("http://www.e-bug.de/preisliste.txt").GetResponse().GetResponseStream(),System.Text.Encoding.GetEncoding(1252)).ReadToEnd().Split('\n');
			foreach(string line in preisliste) root.Match(line.Trim());//match all those lines

			context.Response.ContentType="text/xml";
			XmlTextWriter xtw=new XmlTextWriter(context.Response.Output);
			xtw.Formatting=Formatting.Indented;
			xtw.WriteStartDocument();
			xtw.WriteProcessingInstruction("xml-stylesheet","type=\"text/xsl\" href=\"tree2html.xsl\"");
			root.WriteOut(xtw);
			xtw.WriteEndDocument();
		}
	}
}