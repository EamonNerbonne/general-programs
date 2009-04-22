var GenEdit2;
function GenEdit2T() {
    if (!JSUtil) {
        alert("Requires JSUtil to be loaded.");
        return;
    }
    if (!XMLUtil) {
        alert("Requires XMLUtil to be loaded.");
        return;
    }
    
    function matchflag(flag,str) {//flag is alphanumeric only
        var regex=new RegExp('(^|;)'+flag+'(;|$)');
        return regex.test(str);
    }
    
    function preloadXSL() {
        var newobj;
        newobj=this;
        function dynrowshand(xmldoc) {
            newobj.dynrows=xmldoc;
        }
        function dynhtmlhand(xmldoc) {
            newobj.dynhtml=xmldoc;
        }
        XMLUtil.loadXMLDoc('dynrows.xsl',dynrowshand);
        XMLUtil.loadXMLDoc('dynhtml.xsl',dynhtmlhand);
    }
    
    function init() {
        XMLUtil.forIn(document.getElementsByTagName("genedit"),ge_init);
    } this.init=init;

    function ge_init(ge_node) {
        function handler(xmldoc) {
            //alert('done loading: '+idstr);
            tableMaker(xmldoc,idstr);
        }
        if (!ge_node.id) ge_node.id=XMLUtil.getnewid();
        var idstr=ge_node.id+'_form';
        //alert(idstr);
        var formnode=document.createElement('form');
        formnode.id=idstr;
        //JSUtil.addObserver(formnode,'onsubmit',saveEdit);
        ge_node.parentNode.insertBefore(formnode,ge_node);
        //formnode.appendChild(ge_node);//move node into form to make it easier to find.
        XMLUtil.loadXMLDoc(ge_node.getAttribute("src"),handler);
    }

    function tableMaker(xml_doc,idstr) {
        GenEdit2.tabdata[idstr]=new Object();
        GenEdit2.tabdata[idstr].rowsdata=XMLUtil.transform(xml_doc,this.dynrows);
        constructTable(idstr);
    }

    function constructTable(idstr) {
        var data,header, htmlrep;
        data=GenEdit2.tabdata[idstr].rowsdata;
        htmlrep=XMLUtil.transform(data,this.dynhtml);
        GenEdit2.tabdata[idstr].htmlrep=htmlrep;
        displayTable(idstr);
    }
    
    function displayTable(idstr) {
        var formnode=document.getElementById(idstr);
        XMLUtil.removeAllChildNodes(formnode);
        formnode.appendChild(XMLUtil.xml2html(GenEdit2.tabdata[idstr].htmlrep.documentElement));
        installTableHandlers(idstr);
    }
    
    function installTableHandlers(idstr) {
        var formnode=document.getElementById(idstr);
            function up() {
                pageup(idstr);
            }
            function down() {
                pagedown(idstr);
            }
        function installpagehandlers(node) {
            if (matchflag('pageup',node.getAttribute('flags'))) {
                JSUtil.addObserver(node,'onclick',up);
                node.className='XSLGenEdit_button';
            }
            if (matchflag('pagedown',node.getAttribute('flags'))) {
                JSUtil.addObserver(node,'onclick',down);
                node.className='XSLGenEdit_button';
            }
        }
        XMLUtil.forIn(formnode.getElementsByTagName('div'),installpagehandlers);
        //alert(formnode.innerHTML);
        function installfieldhandlers(node) {//called for each input
            JSUtil.addObserver(node,'onfocus',field_onfocus);
            JSUtil.addObserver(node,'onblur',field_onblur);
        }
        XMLUtil.forIn(formnode.getElementsByTagName('input'),installfieldhandlers);
    }
    
    function field_onfocus(e) {
    	var obj;
    	if (window.Event) obj = e.target;
    	else obj = window.event.srcElement;
        obj.className='XSLGenEdit_focused';
    }

    function field_onblur(e) {
    	var obj,formid,rowid,colid,temp;
    	if (window.Event) obj = e.target;
    	else obj = window.event.srcElement;
    	colid=obj.getAttribute('colref');
    	rowid=obj.getAttribute('rowref');
    	temp=obj;
    	while((String(temp.nodeName)).toUpperCase()!='FORM') temp=temp.parentNode;
    	formid=temp.id;
    	saveChange(formid,rowid,colid,obj.getAttribute('value'),obj);
    }
    
    function saveChange(formid,rowid,colid,newval,inputnode) {
        var temp,cols,i;
    	temp=GenEdit2.tabdata[formid].rowsdata;
    	if (temp.getElementById)
    	    temp=temp.getElementById(rowid);
    	else {
    	    var rows=temp.getElementsByTagName('row');
    	    for(i=0;i<rows.length;i++) {
                if (rows.item(i).getAttribute('id')==rowid) temp=rows.item(i);
    	    }
    	}
    	var cols=XMLUtil.getChildByName(temp,'val');
    	for(i=0;i<cols.length;i++) {
            var node=cols[i];
    	    if(node.getAttribute('name')==colid) {
    	        node.setAttribute('new',newval);
    	        if (node.getAttribute('new')==node.getAttribute('old')) {
    	            inputnode.className='XSLGenEdit_unchanged';
    	        } else {
    	            inputnode.className='XSLGenEdit_changed';
    	        }
    	        break;
    	    }
        }    	
    }

    function pagedown(formid) {
        var doc=GenEdit2.tabdata[formid].rowsdata;
        var winsize,rowcnt,curpos;
        winsize=doc.documentElement.getAttribute('numrowsvisible');
        curpos=doc.documentElement.getAttribute('toprow');
        rowcnt=doc.getElementsByTagName('row').length;
        doc.documentElement.setAttribute('toprow',Math.min(curpos+winsize,rowcnt-winsize+1));
        constructTable(formid);
    } this.pagedown=pagedown;

    function pageup(formid) {
        var doc=GenEdit2.tabdata[formid].rowsdata;
        var winsize,rowcnt,curpos;
        winsize=doc.documentElement.getAttribute('numrowsvisible');
        curpos=doc.documentElement.getAttribute('toprow');
        rowcnt=doc.getElementsByTagName('row').length;
        doc.documentElement.setAttribute('toprow',Math.max(1,curpos-winsize));
        constructTable(formid);
    } this.pageup=pageup;
    
    function saveChanges(formid) {
        alert("Not implemented");
    } this.saveChanges=saveChanges;

    preloadXSL();
    JSUtil.addObserver(window,'onload',init);
    this.tabdata=new Object();
}
GenEdit2=new GenEdit2T();