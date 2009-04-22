//::::::::::::VERIFY PREREQUISITES
try {
    JSLib.Event.verify();
    JSLib.xml.verify();
} catch (e) {
    alert("JSLib is not loaded correctly.\n"+
          "This page might not work because of an page error or a browser problem.\n"+
          "Supported Browsers are:\n"+
          "    Internet Explorer 6.0, Netscape 6.0, and Mozilla 1.0 (or higher)");
    alert("Exception Details:\n"+e);
}

var PakketView=new Object();
PakketView.Data=new Object();
PakketView.LoadStatus=false;
PakketView.LayoutStatus=0;//0-Not Busy, 1-Busy, 2-Busy&Redo
PakketView.Service=null;
PakketView.genFullSvcUrl=function(studentnummer,pakketnummer) {
  if(!PakketView.Service) throw "PakketView.Service unspecified";
  return PakketView.Service+'/'+(JSLib.xml.transform?'':'Html')+'Show?studentnummer='+studentnummer+'&pakketnummer='+pakketnummer;
}

PakketView.LoadStudent=function(idstr,studentnummer,pakketnummer) {
  if(!pakketnummer)pakketnummer=50;
  var retval=new Object();
  PakketView.Data[idstr]=retval;
  retval.studentnummer=studentnummer;
  retval.pakketnummer=pakketnummer;
  retval.xmldoc=null;
  retval.htmlEl=null;
  retval.stat0=1;//Used to determine relayout timing.
  retval.stat1=0;//0=unloaded,1=loading,2=xmlloaded,3=active
  JSLib.xml.loadXMLDoc(PakketView.genFullSvcUrl(studentnummer,pakketnummer),function(thefile){
    retval.xmldoc=thefile;
    retval.stat0=2;
    //document.write("SVC|");
    PakketView.layout();
  });
  PakketView.layout();
}

PakketView.layout=function() {//this function is constantly called and controls when to update what.
  switch(PakketView.LayoutStatus) {
    case 2: case 1: PakketView.LayoutStatus=2; return;
    case 0: PakketView.LayoutStatus=1;
  }
  while(PakketView.LayoutStatus==1) {
    for (var idstr in PakketView.Data) {
      var DataObj=PakketView.Data[idstr];
      var HtmlObj=null;
      if(PakketView.LoadStatus) {
        HtmlObj=document.getElementById(idstr);
        if(HtmlObj==null) {
          alert("oops");
          delete PakketView.Data[idstr];
          continue;
        }
      }
      if(DataObj.stat0!=DataObj.stat1) {
        //alert("idstr: "+idstr+"\nLoad: "+PakketView.LoadStatus+"\nLayout: "+PakketView.LayoutStatus+
          //"\nHtml: "+String(HtmlObj)+"\nstat(0,1): ("+DataObj.stat0+", "+DataObj.stat1+")");
        switch(DataObj.stat0){
          case 1:         
            DataObj.stat1=1;
            break;
          case 2:
            if(JSLib.xml.transform) {
              if(!PakketView.XSLTransformFile) break;
              DataObj.stat1=2;
              DataObj.stat0=3;
              var temp=JSLib.xml.transform(DataObj.xmldoc,PakketView.XSLTransformFile);
              DataObj.htmlEl=JSLib.xml.xml2html(temp.documentElement);
            } else {
              DataObj.stat1=2;
              DataObj.stat0=3;
              DataObj.htmlEl=JSLib.xml.xml2html(DataObj.xmldoc.documentElement);
            }//fall-through
          case 3:
            if(!PakketView.LoadStatus) break;
            //alert(DataObj.htmlEl.innerHTML);
            DataObj.stat1=3;
            var kid = JSLib.xml.nl2a(HtmlObj.childNodes);
            for (var i in kid) {
              HtmlObj.removeChild(kid[i]);
            }
            HtmlObj.appendChild(DataObj.htmlEl);
            PakketView.EventizeTree(HtmlObj);
            delete PakketView.Data[idstr];
            break;
          default:
            throw "Illegal Layout Case Reached";
        }
      }
    }
    PakketView.LayoutStatus--;
  }
}

JSLib.Event.addObserver(window, 'onload',function(){
  PakketView.LoadStatus=true;
  PakketView.layout();
});

PakketView.EventizeTree=function(htmlEl) {
  var divEl = JSLib.xml.nl2a(htmlEl.getElementsByTagName("div"));
  for(var i in  divEl){
    if(JSLib.css.testClass(divEl[i],"treehead")) {
      JSLib.Event.addObserver(divEl[i],"onclick",this.treeonclick);
      JSLib.Event.addObserver(divEl[i],"onmouseover",this.treeonhover);
      JSLib.Event.addObserver(divEl[i],"onmouseout",this.treenohover);
    }
  }
}

PakketView.treeonclick=function(e) {
  JSLib.css.flipClass(this.parentNode,"collapsed");
  if(e && e.stopPropagation) e.stopPropagation();
  if(window.event) window.event.cancelBubble=true;
}
PakketView.treeonhover=function(e) {
  JSLib.css.setClass(this,"hovering");
  if(e && e.stopPropagation) e.stopPropagation();
  if(window.event) window.event.cancelBubble=true;
}
PakketView.treenohover=function(e) {
  JSLib.css.unsetClass(this,"hovering");
  if(e && e.stopPropagation) e.stopPropagation();
  if(window.event) window.event.cancelBubble=true;
}
if (JSLib.xml.transform) {
  JSLib.xml.loadXMLDoc("../xsl/tree2html.xsl",function(thefile){
    PakketView.XSLTransformFile=thefile;
    PakketView.layout();
  });
}
