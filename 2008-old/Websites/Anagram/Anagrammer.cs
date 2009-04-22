using System;
using System.IO;
using System.Web;
using System.Xml;
using System.Collections;
namespace AnaGram {
    /// <summary>
    /// Summary description for Anagrammer.
    /// </summary>
    public class Anagrammer :IHttpHandler {
        struct Pair:IComparable {
            public string srt;
            public string orig;
            public Pair(string str){
                orig=str;
                srt=new string(sortstr(str));
            }
            public int CompareTo(object other) {
                return srt.CompareTo(((Pair)other).srt);
            }
        }
        /// <summary>
        /// Required by ASP.NET; indicates whether or not an instance of <see cref="IHttpHandler"/> can be
        /// reused, which <c>RequestHandler</c> instances always can.
        /// </summary>
        public bool IsReusable {get { return true; }}

        static char[] sortstr(string str) {
            ArrayList temp= new ArrayList(str.ToLower().ToCharArray());
            temp.Sort();
            return (char[])temp.ToArray(typeof(char));;
        }
        /*
         //checks whether a is substr of b
        static bool matches(char[] a, char[] b) {
            int ai=0;
            foreach(char c in b) {
                if (ai>=a.Length) return false;
                while(c!=a[ai]){
                    if(c<a[ai]) return false;
                    if (++ai>=a.Length) return false;
                }
                ai++;
            }
            return true;
        }*/
        public void ProcessRequest(HttpContext context) {
            string url = context.Server.MapPath(context.Request.Url.LocalPath);
            XmlTextWriter outp=new XmlTextWriter(context.Response.Output);
            context.Response.ContentType="text/xml";
            outp.WriteStartDocument();
            outp.WriteProcessingInstruction("xml-stylesheet","type=\"text/xsl\" href=\"anagram.xsl\"");
            outp.WriteStartElement("matches");
            string query=context.Request.QueryString["word"];
            if(query!=null){
                query.Replace(" ","");
                Pair qCA=new Pair(query.ToLower());

                ArrayList dict;
                if(context.Cache[url]!=null) dict=(ArrayList)context.Cache[url];
                else {
                    TextReader reader=new StreamReader(new FileInfo(url).OpenRead());
                    string word;
                    dict=new ArrayList();
                    while((word=reader.ReadLine())!=null){
                        //if(word.Length<3) continue;
                        dict.Add(new Pair(word));
                    }
                    dict.Sort();
                    context.Cache[url]=dict;
                }
                
                outp.WriteAttributeString("dictsize",dict.Count.ToString());
                int i=dict.BinarySearch(qCA);
                if(i>0) for(;i<dict.Count&&((Pair)dict[i]).srt==qCA.srt;i++)
                    outp.WriteElementString("match",((Pair)dict[i]).orig);
                
            }
            outp.WriteEndElement();
            outp.WriteEndDocument();
        }
    }
}
