<%@ WebHandler Language="C#" Class="Handler" %>

using System;
using System.Web;
using System.Text;
using System.IO;
public class Handler : IHttpHandler {

    public void ProcessRequest(HttpContext context) {
        context.Response.ContentType = "text/html";
        SpellList sl = SpellList.Unique;
        SpellSelection ss = new SpellSelection(SpellList.Unique, context.Request.Form["SpellBox"]);
        TextWriter r= context.Response.Output;
        r.WriteLine("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">\n");
        r.WriteLine(@"<html xmlns='http://www.w3.org/1999/xhtml'>
<head><title>
	Spell List
</title>
<style type='text/css'>
body{font-size: 9pt;font-family: 'Sylfaen' 'Andalus' Serif;line-height:97%;}
div.section{
	-moz-column-width: 17em;
	-moz-column-gap: 1em;
	column-width: 17em;
	column-gap: 1em;
}
h1,h2,h3 {margin:0.3em 0 0 0 ;}
h1{ font-size:150%; width:100%;border-bottom:1px solid blue;background:#eef;}
h2{ font-size:130%; width:100%;border-bottom:1px solid gray; text-align:right;background:#ffe;}
h3{ font-size:120%; border:1px solid gray; display:inline}
table,td,tr,th {margin:0;padding:0 0.2em;border:none;border-collapse:collapse;}
td{border:1px dotted gray;}
table{border:1px solid black;width:100%}
tr.odd{background-color: #ddd}
tr.even{background-color: #eee}

p{margin:0.3em 0 0 0;}
p+p{text-indent:2em;}

@media print {
body font-size 8pt;
}
</style>
</head>
<body>
");

        foreach (SpellSelection.Section s in ss.sections) {
            r.WriteLine("<h1>" + context.Server.HtmlEncode(s.name) + "</h1><div class='section'>");
            foreach (string spellname in s.spells) {
                if (!sl.Contains(spellname)) {
                    r.WriteLine("<h2>" + context.Server.HtmlEncode(spellname) + "</h2>");
                } else {
                    r.WriteLine(sl[spellname].OuterXml);
                }
            }
            r.WriteLine("</div>");
        }
        r.WriteLine(@"</body></html>");
    }

    public bool IsReusable {
        get {
            return true;
        }
    }

}