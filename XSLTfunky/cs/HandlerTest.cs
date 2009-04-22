// Name this C# file HandlerTest.cs and compile it with the
// command line: csc /t:Library /r:System.Web.dll HandlerTest.cs.
// Copy HandlerTest.dll to your \bin directory.

using System.Web;
using System.IO;
using System.Xml;
using System.Xml.Xsl;

namespace EamonNS {
    public class XslFileHandler : IHttpHandler {
        // Override the ProcessRequest method.
        public void ProcessRequest(HttpContext context) {
            HttpRequest inp=context.Request;
            HttpResponse outp=context.Response;
            string realpath=context.Server.MapPath(inp.FilePath);
            if(File.Exists(realpath)) {
                string fileattr=inp.QueryString.Get("file");
                if(fileattr!=null) {
                    string xmlfile=context.Server.MapPath(fileattr);
                    if (File.Exists(xmlfile)) {
                        XslTransform xslt=new XslTransform();
                        XmlDocument xmldoc=new XmlDocument();
                        xslt.Load(realpath);
                        xmldoc.Load(xmlfile);
                        string type=inp.QueryString.Get("mime");
                        outp.ContentType=type==null?"text/xml":type;
                        xslt.Transform(xmldoc,null,outp.Output);
                    } else {
                        outp.StatusCode=404;
                        outp.Write("XSLT Data Not Found: '"+fileattr+"'");
                    }
                } else {
                    outp.ContentType="text/xml";
                    outp.WriteFile(realpath);
                }
            } else {
                outp.StatusCode=404;
                outp.Write("404 Not Found: '"+inp.FilePath+"'");
            }
        }

        // Override the IsReusable property.
        public bool IsReusable {
            get { return true; }
        }
    }
}

/*
______________________________________________________________

To use this handler, include the following lines in a Web.config file.

<configuration>
   <system.web>
      <httpHandlers>
         <add verb="*" path="handler.aspx" type="HandlerExample.MyHttpHandler,HandlerTest"/>
      </httpHandlers>
   </system.web>
</configuration>
*/