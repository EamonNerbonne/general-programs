using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Xml;
using System.Xml.XPath;
using System.IO;
using System.Xml.Xsl;
using System.Collections.Generic;
using System.Web.Caching;

/// <summary>
/// A Scraped web page.
/// </summary>
public class Scrape
{
    public Uri uri;
    public XmlDocument doc;

    public Scrape(string uriString, Dictionary<string,string> xslParams,string xslFile):this(new Uri(uriString),xslParams, xslFile){}
    public Scrape(Uri uri,Dictionary<string,string> xslParams,string xslFile)
    {
        this.uri = uri;
        string download = (string)HttpContext.Current.Cache[uri.ToString()];
        if (download == null) 
            HttpContext.Current.Cache.Insert(uri.ToString(),download = (new System.Net.WebClient()).DownloadString(uri));
        download = TidyItUp.CleanupToXml(download); //clean it up
        download = download.Substring(1).Replace("\r\n", "\n"); //remove BOM and strip useless CR's

        doc = new XmlDocument();
        if (xslFile != null)
        {
            XslCompiledTransform xsl = XslCache.Get(xslFile);
            XPathDocument srcdoc = new XPathDocument(new StringReader(download));
            StringWriter result = new StringWriter();
            XsltArgumentList args = new XsltArgumentList();

            if (xslParams != null) 
                foreach (KeyValuePair<string, string> param in xslParams) 
                    args.AddParam(param.Key, "", param.Value);

            xsl.Transform(srcdoc, args, new XmlTextWriter(result));

            doc.LoadXml(result.ToString());
        }
        else
        {
            doc.LoadXml(download);
        }
    }
}
