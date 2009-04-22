//This file contains a container object.  All functionality can be reached through it,
//e.g. JSUtil.myfunc();
var JSLib=new Object();
JSLib.Event=new Object(); {
  var JSEVT=JSLib.Event;
  JSEVT.verify=function(){
    function test(hi,bye) {}
    if(!test.call||!test.apply) throw "Requires Function calling/applying support";
  }

  if(window.addEventListener)
    JSEVT.addObserver=function(theobj, eventname, handler) 
       {theobj.addEventListener(eventname.substr(2), handler, false);}
  else {//MSIE workaBLAH
    JSEVT._eventHandler=new Object();//INTERNAL:will hash handlers by name. (one entry per name!)
    JSEVT._HandlerArrArr=new Array();//INTERNAL:array; for each handler a new handlers array is held
    JSEVT.addObserver=function(theobj, eventname, handler) {
        var thearr=JSEVT._getObservers(theobj,eventname);
        thearr.push(handler);
    }

    //INTERNAL
    JSEVT._getObservers=function(theobj, eventname) {
        if(theobj["JSEVT->"+eventname]==null) {
            var temp=new Array();
            theobj["JSEVT->"+eventname]=JSEVT._HandlerArrArr.length;
            JSEVT._HandlerArrArr.push(temp);
            if(theobj[eventname]!=null) temp.push(theobj[eventname]);
            theobj[eventname]=JSEVT._lookuphandler(eventname);
        }
        return JSEVT._HandlerArrArr[theobj["JSEVT->"+eventname]];
    }
    
    //INTERNAL
    JSEVT._lookuphandler=function(name) {
        if (JSEVT._eventHandler[name]===undefined) 
            JSEVT._inithandler(name);
        return JSEVT._eventHandler[name];
    }
    //INTERNAL
    JSEVT._inithandler=function(eventname) {//INTERNAL
        JSEVT._eventHandler[eventname]=function(){
            var i,afunc,retval=true,funcarr,temp;
            funcarr=JSEVT._HandlerArrArr[this["JSEVT->"+eventname]];
            for(i=0;i<funcarr.length;i++) {
                afunc=(funcarr[i]);
                temp=afunc.apply(this,arguments);
                retval=(temp===undefined||temp!==false)&&retval;
            }
            return retval;
        }
    }//INTERNAL
  }
}

JSLib.Util=new Object(); {
  var Util=JSLib.Util;
  Util.methodRef=function(object,func) {
    return function(){func.apply(object,arguments);}
  }

  //returns the index of the last element of "array" whose value is at most "value".
  //array must be sorted.
  //will return 0 if "value" is smaller than all values in the array.
  //will return array.length -1 if "value" is larger than all values in the array.
  Util.binSearch=function(array,value) {
        if(array.length<2) return 0;
        var maxpos=array.length,minpos=0,pos;
        while (minpos!=maxpos-1) {
            pos=(maxpos+minpos)/2;
            if(value<array[pos]) maxpos=pos;
            else minpos=pos;
        }
        return minpos;
    } 
}

JSLib.css=new Object(); {
  var CSSUtil=JSLib.css;
  //Tests whether a given element is of a class 'className'
  CSSUtil.testClass=function(htmlEl,className) {
    var str=htmlEl.className,strArr;
    if(!str) return false;
    //alert(str);
    strArr=str.split(" ");
    for(var sClass in strArr) {
      if(strArr[sClass]==className) return true;
    }
    return false;
  }

  CSSUtil.setClass=function(htmlEl, className) {
    var str=htmlEl.className,strArr;
    strArr=str.split(" ");
    for(var sClass in strArr) {
      if(strArr[sClass]==className) return;
    }
    strArr.push(className);
    htmlEl.className=strArr.join(" ");
  }

  CSSUtil.unsetClass=function(htmlEl, className) {
    var str=htmlEl.className,strArr;
    strArr=str.split(" ");
    for(var i=0;i<strArr.length;i++) {
      if(strArr[i]==className){
        strArr.splice(i,1);
        htmlEl.className=strArr.join(" ");
        return;
      }
    }
  }

  CSSUtil.flipClass=function(htmlEl, className) {
    var str=htmlEl.className,strArr;
    strArr=str.split(" ");
    for(var i=0;i<strArr.length;i++) {
      if(strArr[i]==className){
        strArr.splice(i,1);
        htmlEl.className=strArr.join(" ");
        return;
      }
    }
    strArr.push(className);
    htmlEl.className=strArr.join(" ");
  }
}

JSLib.xml=new Object();{
  var JSXML=JSLib.xml;
  JSXML.verify=function(){
    if(!(window.XMLHttpRequest) && !window.ActiveXObject)
      throw "Requires functioning XMLHttpRequest";
    else if(window.ActiveXObject) {
      try {
        xyz=new ActiveXObject("MSXML2.XmlHttp.3.0");
      } catch (exceptn){
        throw "Requires functioning XMLHttpRequest";
      }
    }
    JSLib.Event.verify();
  }

  JSXML.idnum=0;//helpervariable
  JSXML.getnewid=function() {
	return "emnutil"+(JSXML.idnum++);
  }
   
  JSXML.nl2a=function(items) {
    var i, retval=new Array(items.length);
    for(i=0;i<retval.length;i++) retval[i]=items.item(i);
    return retval;
  }
   
    //loads an xml file from the given url, calling load_handler(file) when done
    //if load_handler is null, loads synchronously and returns the "Document" as DOM object.
    //otherwise, calls the load_handler with one parameter when done loading, namely the DOM-tree
  JSXML.loadXMLDoc=function(load_url,load_handler) {
    var req;
    if (window.XMLHttpRequest) {
      req=new XMLHttpRequest();
    } else if (window.ActiveXObject)	{//annoying IE xmldom limitation, workaround using ActiveX
      //alert("ActiveX workaround");
	  req = new ActiveXObject("MSXML2.XmlHttp.3.0");
	  //req.preserveWhiteSpace=true;  //unfortunate, Mozilla always does this, so to make things
		                            //more consistent we do it here too.
 	} else {//uhuh
      alert("Your browser can't handle this script");
    }
    //alert("ok");
    if(load_handler==null) {
      req.open("GET", load_url, false);//false==synchronous
      req.send(null);
      return req.responseXML; // responseXML : XmlDocument
    } else {
      req.open("GET", load_url, true);//true==asynchronous
      if(req.readyState) 
        req.onreadystatechange=function() {if (req.readyState==4) load_handler(req.responseXML);};
      else {
        req.addEventListener('load',function(){load_handler(req.responseXML);});
      }
      req.send(null);
    }
  }
    
    JSXML.xml2html=function(xmlnode) {//essentially an MSIE workaround.
        if(document.importNode) return document.importNode(xmlnode,true);
        var newfrag,textrep;
        newfrag=document.createElement('temp');
        function copyTo(target,what) {
            var newtempnode,i;
            switch(what.nodeType) {
                case 1://element
                    newtempnode=document.createElement(what.nodeName);
                    for(i=0;i<what.childNodes.length;i++) 
                        copyTo(newtempnode,what.childNodes.item(i));
                    for(i=0;i<what.attributes.length;i++) 
                        copyTo(newtempnode,what.attributes.item(i));
                    target.appendChild(newtempnode);
                    break;
                case 2://attribute
                    if(what.nodeName=="class") target.className=what.nodeValue;
                    else target.setAttribute(what.nodeName,what.nodeValue);
                    break;
                case 3://text;
                    target.appendChild(document.createTextNode(what.nodeValue));
                    break;
                case 4://CDATA;
                    target.appendChild(document.createCDATASection(what.nodeValue));
                    break;          
                case 8://comment, may be important for script;
                    target.appendChild(document.createComment(what.nodeValue));
                    break;
                default:
                    alert(''+what.nodeType+': '+what.nodeName+'="'+what.nodeValue+'"');
                    throw (''+what.nodeType+': '+what.nodeName+'="'+what.nodeValue+'"');
                    break;
            }
        }
//        copyTo(newfrag,xmlnode);
        newfrag.innerHTML=xmlnode.xml;
        return newfrag.childNodes.item(0);
    }
try{
  var xyz=new ActiveXObject("MSXML2.DOMDocument.3.0");
  JSXML.transform=function(xmldoc,xsltdoc) {//IE6 Only!
    var outdoc;
    outdoc= new ActiveXObject("MSXML2.DOMDocument.3.0");
    outdoc.async=false;
    outdoc.loadXML(xmldoc.transformNode(xsltdoc));
    return outdoc;
  }
}catch(exceptn){}    

}