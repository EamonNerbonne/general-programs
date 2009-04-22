using System;
using System.Web;
using System.Xml;
using System.IO;
using System.Collections;
namespace ReqGraph {
    /// <summary>
    /// Summary description for HistoHandler.
    /// </summary>
    public class HistoHandler :IHttpHandler {
        public bool IsReusable{get {return true;}}
        public void ProcessRequest(HttpContext context) {
            string url = context.Server.MapPath(context.Request.Url.LocalPath);
            XmlTextWriter outp=new XmlTextWriter(context.Response.Output);
            context.Response.ContentType="text/xml";
            outp.WriteStartDocument();
            outp.WriteProcessingInstruction("xml-stylesheet","type=\"text/xsl\" href=\"histodisp.xsl\"");
            int divisions,start,end;
            try {
                divisions= int.Parse(context.Request.QueryString["divisions"]);
                start= int.Parse(context.Request.QueryString["start"]);
                end= int.Parse(context.Request.QueryString["end"]);
            } catch (Exception){
                outp.WriteElementString("error","Please specify variables 'divisions'(1 to 10000), 'start'(0 to divisions-1), and 'end' (start+1 to divisions).  [start, end) will be displayed");
                outp.WriteEndDocument();return;
            }
            if (divisions>=10000||divisions<1) {
                outp.WriteElementString("error","The divisions argument must be in [1,10000)");
                outp.WriteEndDocument();return;
            }
            if (start>=divisions||start<0) {
                outp.WriteElementString("error","The start argument must be in [0,divisions)");
                outp.WriteEndDocument();return;
            }
            if (end>divisions||end<=start) {
                outp.WriteElementString("error","The end argument must be in (start,divisions]");
                outp.WriteEndDocument();return;
            }
            ArrayList vals;
            if(context.Cache[url]!=null) vals=(ArrayList)context.Cache[url];
            else {
                TextReader reader=new StreamReader(new FileInfo(url).OpenRead());
                string line;
                vals=new ArrayList();
                while((line=reader.ReadLine())!=null){
                    if(line[0]=='#') continue;
                    vals.Add(TimeSpan.Parse(line.Substring(11,8)).TotalDays);
                }
                vals.Sort();//should be sorted already, but to be safe...
                context.Cache[url]=vals;
            }
            int cumfreq=vals.BinarySearch(((double)start)/divisions);
            if(cumfreq<0)cumfreq=~cumfreq;
            int max=0,init=cumfreq;
            ArrayList histo=new ArrayList();
            for(int i=start;i<end;i++) {
                int newcum=vals.BinarySearch(((double)i+1)/divisions);
                if(newcum<0)newcum=~newcum;
                int freq=newcum-cumfreq;
                histo.Add(freq);
                if(freq>max)max=freq;
                cumfreq=newcum;
            }
            outp.WriteStartElement("datafreq");
            outp.WriteAttributeString("valcount",vals.Count.ToString());
            outp.WriteAttributeString("subcount",(cumfreq-init).ToString());
            outp.WriteAttributeString("maxcount",max.ToString());
            outp.WriteAttributeString("start",start.ToString());
            outp.WriteAttributeString("end",end.ToString());
            outp.WriteAttributeString("divisions",divisions.ToString());
            for(int i=start;i<end;i++) {
                outp.WriteStartElement("data");
                outp.WriteAttributeString("num",i.ToString());
                outp.WriteAttributeString("val",histo[i-start].ToString());
                //outp.WriteAttributeString("vals",vals[i*100].ToString());
                outp.WriteEndElement();
            }
            outp.WriteEndElement();
            outp.WriteEndDocument();
        }
    }
}
