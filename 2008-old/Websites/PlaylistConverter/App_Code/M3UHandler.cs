using System;
using System.Web;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
namespace PlaylistConv {
    public class M3UHandler : IHttpHandler {


        public void ProcessRequest(HttpContext context) {
            FileInfo fi = new FileInfo(context.Request.PhysicalPath);
            if (!fi.Exists || fi.Extension.ToLower() != ".m3u")
                throw new Exception("This thing can only convert m3u files - config error?");
            context.Response.ContentType = "audio/mpeg-url";
            StreamReader reader = new StreamReader(fi.OpenRead(), Encoding.GetEncoding(0));
            TextWriter writer = context.Response.Output;
            for (string line = reader.ReadLine(); line != null; line = reader.ReadLine()) {
                if (line.StartsWith("#"))
                    writer.WriteLine(line);
                else {
                    string[] path = line.Split('\\');
                    if (path[0].Length == 2 && path[0][1] == ':') { //e.g. C:
                        path[0] = "http://85.145.145.35/" + path[0][0];
                        for (int i = 1; i < path.Length; i++)
                            path[i] = HttpUtility.UrlPathEncode(path[i]);
                        writer.WriteLine(string.Join("/", path));
                    } else {
                        writer.WriteLine(string.Join("\\", path));
                    }
                }
            }
            reader.Close();
        }

        public bool IsReusable {
            get {
                return true;
            }
        }
    }
}