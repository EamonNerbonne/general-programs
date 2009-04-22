using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.IO;
using System.Xml;
using System.Xml.Xsl;
using System.Web.Caching;

/// <summary>
/// Summary description for XslCache
/// </summary>
public class XslCache
{
    public static XslCompiledTransform Get(string filename)
    {
        HttpContext cur = HttpContext.Current;
        XslCompiledTransform retval = (XslCompiledTransform)cur.Cache[filename];
        if (retval == null)
        {
            retval = new XslCompiledTransform(true);
            string absname = cur.Server.MapPath(filename);
            if (File.Exists(absname))
            {
                retval.Load(absname);
                cur.Cache.Insert(filename, retval, new CacheDependency(absname));
            }
            else retval = null;
        }
        return retval;
    }
    private XslCache(){}
}
