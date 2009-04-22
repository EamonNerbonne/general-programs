//Requires JSUtil.

//XMLUtil.getnewid(); returns a string
//XMLUtil.forIn(nodelist,function); calls function with each node in nodelist as parameter
//XMLUtil.loadXMLDoc(load_url,load_handler); loads an xmlfile and subsequently calls load_handler with
//                                           the file as parameter
//XMLUtil.removeAllChildNodes(node); removes all child nodes from a given node




//This contains a few basic utilities to make other javascript programs easier to write
//Because of what it is, it should be included in the html file BEFORE any other javascript is
//run.
//It provides xml file loading, id creation, easy looping, and child removal


// This variable is to be instantiated once.
//All functions/variables in this package can be Accessed from this object via
// XMLUtil.[funcname|varname]
JSUtil.verify();

var XMLUtil=new Object();

XMLUtil.verify=function(){
    if(!(document.implementation && document.implementation.createDocument) && !window.ActiveXObject)
        throw "Requires functioning DOM2 or ActiveX";

}

//The creation of the XMLUtil Object is handled by this function.

    //####INT getnewid(VOID);
    //dynamically generates unique id's in the form 'emnutil###' where ### is a number (of any length)
XMLUtil.idnum=0;//helpervariable
XMLUtil.getnewid=function() {
	    return "emnutil"+(XMLUtil.idnum++);
    }
    
    //ITEMS: object must support    .length (returns INT)
    //                              .items(INT) (returns object for all 0<=i<length)
    //#### VOID forIn(ITEMS,VOID f(OBJ))
    //Calls handler for each item in items
XMLUtil.forIn=function(items,handler) {
        var cnt,i;
        cnt=items.length;
        for(i=0;i<cnt;i++) handler(items.item(i));
    }
   
   
    //loads an xml file from the given url, calling load_handler(file) when done
XMLUtil.loadXMLDoc=function(load_url,load_handler) {
        var xmlDoc;
    	if (document.implementation && document.implementation.createDocument) {
	        //alert("implementation.createDocument");
		    xmlDoc = document.implementation.createDocument("", "", null);
	        JSUtil.addObserver(xmlDoc,'onload',function(){load_handler(this);});
	        xmlDoc.load(load_url);
	    }else if (window.ActiveXObject)	{//annoying IE xmldom limitation, workaround using ActiveX
    	    //alert("ActiveX workaround");
		    xmlDoc = new ActiveXObject("MSXML2.DOMDocument.3.0");
		    xmlDoc.async=false;
		    xmlDoc.preserveWhiteSpace=true;//unfortunate, Mozilla always does this, so to make things
		                                //more consistent we do it here too.
            xmlDoc.load(load_url);
            load_handler(xmlDoc);
 	    }else{//uhuh
    		alert('Your browser can\'t handle this script');
    	}
    }
    
    //Removes all node from the given (XMLNODE) node.
XMLUtil.removeAllChildNodes=function(node) {
        while (node.hasChildNodes()) {
            node.removeChild(node.firstChild);    
        }
    }

    //Returns an array of all child elements with the given name
XMLUtil.getChildByName=function(node, name) {
        var retval=new Array();
        function addIfOk(item) {
            if (item.nodeType==1 &&item.nodeName==name) retval.push(item);
        }
        XMLUtil.forIn(node.childNodes,addIfOk);
        return retval;
    }
    
//if (window.ActiveXObject)    
    XMLUtil.xml2html=function(xmlnode) {//IE6 only!!!
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
                    newtempnode=document.createAttribute(what.nodeName);
                    newtempnode.nodeValue=what.nodeValue;
                    target.setAttributeNode(newtempnode);
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
        copyTo(newfrag,xmlnode);
        return newfrag.childNodes.item(0);
    }
    
XMLUtil.transform=function(xmldoc,xsltdoc) {//IE6 Only!
        var outdoc;
        outdoc= new ActiveXObject("MSXML2.DOMDocument.3.0");
        outdoc.async=false;
        outdoc.loadXML(xmldoc.transformNode(xsltdoc));
        return outdoc;
    }
