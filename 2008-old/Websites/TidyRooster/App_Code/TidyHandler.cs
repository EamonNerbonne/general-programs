using System;
using System.Web;
using System.Xml;
using System.Xml.Xsl;
using System.Xml.XPath;
using System.IO;
using System.Collections.Generic;

public class TidyHandler : IHttpHandler {
    XslCompiledTransform xsl;
    
    public TidyHandler()
    {
        xsl=new XslCompiledTransform(false);
        xsl.Load(new XPathDocument(new StreamReader(HttpContext.Current.Server.MapPath("roosterLinks.xsl"))));
    }

    public void ProcessRequest (HttpContext context) {
        context.Response.ContentType = "text/html";
        Dictionary<string, string> xslArgs = new Dictionary<string, string>();
        xslArgs["periode"] = context.Request["periode"];

        Scrape roosterIndex = new Scrape("http://www.rug.nl/informatica/onderwijs/rooster/rooster-2005-2006/index",xslArgs,"roosterLinks.xsl");

        XmlDocument xmldoc = new XmlDocument();
        XmlElement rootElem = xmldoc.CreateElement("root");
        xmldoc.AppendChild(rootElem);

        foreach (XmlText xt in roosterIndex.doc.SelectNodes("/root/href/text()"))
        {
            try
            {
                Scrape lastSub = new Scrape(new Uri(roosterIndex.uri, xt.Value), null, "roosterCut.xsl");
                rootElem.AppendChild(xmldoc.ImportNode(lastSub.doc.DocumentElement, true));
            }
            catch (System.Net.WebException we)
            {//ignore bad pages
            }
        }

        XslCompiledTransform xsl = XslCache.Get("extractClass.xsl");

        StringWriter sw = new StringWriter();
        xsl.Transform(xmldoc,new XmlTextWriter(sw));

        XmlDocument entries = new XmlDocument();
        entries.LoadXml(sw.ToString());
        //context.Response.Output.Write(sw.ToString());
        
        XsltArgumentList vakCodes = new XsltArgumentList();
        vakCodes.AddParam("vakcodes","",context.Request["vakcodes"]);

        XslCompiledTransform xsl2 = XslCache.Get("showEntries.xsl");

        xsl2.Transform(entries, vakCodes, new XmlTextWriter(context.Response.Output));
    }
 
    public bool IsReusable { get { return true; } }

}