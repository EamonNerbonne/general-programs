var GenEdit3;
///
 // GenEdit3 main Object
 //
 //     Events:
 //
 // onload:                 Called when all files have been loaded.
 //
 //     (static) Functions:
 //
 // matchflag(flag, str):   Determines presence/absence of a particular flag by within a string
 ///
function GenEdit3T() {
    ///
     // Requirement Checking...
     ///
    if (!JSUtil) {alert("Requires JSUtil to be loaded.");return;}
    if (!XMLUtil) {alert("Requires XMLUtil to be loaded.");return;}
    
    ///
     // Variable Declarations
     ///
    this.tables=new Object();  //Hashtable: GenEdit3[idstr]==ge_node (with  ge_node.id==idstr)
    this.filenames=new Array();//array of all loaded filesnames
    this.filedata=new Array(); //corresponding array of xml DOM objects
    this.def2rows=new Object();//placeholder; will be DOM of XSLT transform...
                               //in: data definition, out XSLT(in: data, out 'rows'(data))
    this.def2html=new Object();//placeholder; will be DOM of XSLT transform...
                               //in: data definition, out HTML template for easy javascriptability
    ///
     // [static] Determines presence/absence of a particular flag by within a string
     ///
    function matchflag(flag,str) {//flag is alphanumeric only
        var regex=new RegExp('(^|;)'+flag+'(;|$)');
        return regex.test(str);
    } this.matchflag=matchflag;
    
    
    
    function init() {
        var files=new Array(),files2=new Array(),xmldata=new Array(),i;
        files.push("def2rows.xsl");
        files.push("def2html.xsl");

        function ge_init(ge_node) {
            var table=new geTableT(ge_node);
            files.push(table.getdef());
            files.push(table.getsrc());
            this.tables[table.getid()]=table;
        }
        XMLUtil.forIn(document.getElementsByTagName("genedit3"),JSUtil.makeMethodRef(this,ge_init));

        files.sort();
        files2.push(files[0]);
        for(i=1;i<files.length;i++) 
            if(files[i]!=files[i-1]) 
                files2.push(files[i]);//remove duplicates
        files=files2;//no need of original data;
        
        var filecount=files.length,loadcount=0;
        xmldata.length=filecount;
        
        function mkhandler(pos) {
            function handler(loadedfile) {
                xmldata[pos]=loadedfile;
                loadcount++;
                if(loadcount==filecount) {
                    this.filenames=files;
                    this.filedata=xmldata;
                    this.def2rows=xmldata[binSearch(files,"def2rows.xsl")];
                    this.def2html=xmldata[binSearch(files,"def2html.xsl")];
                    if (this.onload) this.onload();
                }
            }
            return JSUtil.makeMethodRef(this,handler);
        }
        for(i=0;i<files.length;i++) {
            XMLUtil.loadXMLDoc(files[i],mkhandler.call(this,i));     
        }
    } this.init=JSUtil.makeMethodRef(this,init);

    function binSearch(filenames,namestr) {//intended for filenames but generically usable if wanted.
        var maxpos=filenames.length,minpos=0,pos;
        while (minpos!=maxpos-1) {
            pos=(maxpos+minpos)/2;
            if(namestr<filenames[pos]) maxpos=pos;            
            else minpos=pos;
        }
        if (namestr!=filenames[minpos]) alert("Oops... file not found ("+namestr+")");
        return minpos;
    } this.binSearch=binSearch;

    function findFile(namestr) {
        return (this.filedata[binSearch(this.filenames,namestr)]);
    } this.findFile=JSUtil.makeMethodRef(this,findFile);

    JSUtil.addObserver(window,'onload',this.init);
}
GenEdit3=new GenEdit3T();

    ///
     // Constructor function for a generic edit table.
     // takes as parameter a reference to the <genedit3 ...> node in 'document' that it should represent.
     // called during window.onload, BEFORE files are loaded.
     // 
     //     Exports:
     //
     // getnode() returns node it was constructed with.
     // getdef() returns the data definition href.
     // getsrc() returns the data href.
     // init() to be called by GenEdit after all data files have been loaded.
     ///
function geTableT(ge_node) {
    var idstr,formnode,deffile,srcfile,rowsdata,template;
    var rowHash=new Object();
    var colHash=new Object();
    
    function geColT(colObj) {
        this.defObj=colObj;
        this.type=colObj.getAttribute("type");
        this.text=colObj.getAttribute("text");
        this.editable=GenEdit3.matchflag("editable",colObj.getAttribute("flags"));
        this.isID=GenEdit3.matchflag("id",colObj.getAttribute("flags"));
        if(this.editable) this.inputSize=colObj.getAttribute("input-size");
    }
    
    function geRowT(rowObj) {//Warning: cannot be created before setupCols() has been called
        this.rowObj=new Object();
        this.rowObj.xml=rowObj;
        this.rowObj.html=template.htmlRow.cloneNode(true);//deep clone.
        this.colHash=new Object();
        this.colHash.xml=new Object();
        this.colHash.html=new Object();
        setupRow(this.rowObj.html,this.colHash.html);//this.colHash.html[colname].valueNode.nodeValue...
        //alert(this.rowObj.html.innerHTML);
        var xmlhash=this.colHash.xml;
        XMLUtil.forIn(rowObj.getElementsByTagName("col"),hashem);//this.colHash.xml[colname] === DOM node of col;
        function hashem(colref) {
            xmlhash[colref.getAttribute("name")]=colref;                    
        }
        var acol,this2=this;
        for (acol in colHash) {
            function onchangehandler() {
                alert("changed: "+rowObj.getAttribute("id")+", "+acol);
            }
            if (colHash[acol].editable) {//make an onchange
                //alert(this.colHash.html[acol].valueNode.parentNode);
                JSUtil.addObserver(this.colHash.html[acol].valueOwner,'onchange',onchangehandler);
                //unshown observer causes memory leak in IE6.0
            }
        }
    }
 
    function getnode() {return ge_node;} this.getnode=getnode;
    function getdef() {return ge_node.getAttribute("def");} this.getdef=getdef;
    function getsrc() {return ge_node.getAttribute("src");} this.getsrc=getsrc;
    function getid() {return idstr;} this.getid=getid;
    function init() {
        var prop;
        template=new Object();
        deffile=GenEdit3.findFile(getdef());
        srcfile=GenEdit3.findFile(getsrc());
        rowsdata=XMLUtil.transform(srcfile,XMLUtil.transform(deffile,GenEdit3.def2rows));
        //alert(rowsdata.xml);
        template.table=XMLUtil.transform(deffile,GenEdit3.def2html).documentElement;
        template.row=XMLUtil.getChildByName(template.table.getElementsByTagName("tbody").item(0),"tr")[0];
        XMLUtil.removeAllChildNodes(template.table.getElementsByTagName("tbody").item(0));
        template.htmlTable=XMLUtil.xml2html(template.table);
        template.htmlRow=XMLUtil.xml2html(template.row);
        setupCols();//sets up colHash making all columns easily accessible;
        //setupRow(htmlRow,colHash);//sets up the htmlRow by adding a reference from colHash into it.
        setupRows();//creates a Hash of Rows, links to html-rows, which it also creates.
    } this.init=JSUtil.makeMethodRef(this,init);
    
    function setupRows() {
        XMLUtil.forIn(srcfile.getElementsByTagName("row"),eachrow);
        function eachrow(rowref) {
            var idstr=rowref.getAttribute("id");
            rowHash[idstr]=new geRowT(rowref);
        }
    }
    
    function setupCols() {
        function addColHash(colEl) {
            var name=colEl.getAttribute("name");
            colHash[name]=new geColT(colEl);
        }
        JSUtil.forEach((XMLUtil.getChildByName(deffile.getElementsByTagName("cols").item(0),"col")),addColHash);
    }

    function setupRow(ahtmlRow,acolHash) {
        function findXSLGE(elem) {
            if(elem.attributes.getNamedItem("xslge3")) {//attribute found!
                return elem;
            } else {//not found; search children.
                var curr=elem.firstChild;
                while(curr) {//while kids
                    if (curr.nodeType==1) {//only check elements
                        var retval=findXSLGE(curr);
                        if(retval) return retval;//if found, return, else...
                    }
                    curr=curr.nextSibling;//...continue searching.
                }
                return null;//not found at all, indicate this.
            }
        }
        function tdhand(tdEl){
            var colRef=tdEl.getAttribute("colref"),valnode,vnname;
            if(!acolHash[colRef]) acolHash[colRef]=new Object();
            acolHash[colRef].tdEl=tdEl;
            acolHash[colRef].valueOwner=valnode=findXSLGE(tdEl);
            vnname=valnode.getAttribute("xslge3");//put into...
            if(vnname=="") {//...a text node
                var textnode=document.createTextNode("");
                valnode.appendChild(textnode);
                acolHash[colRef].valueNode=textnode;
            } else {//...an attribute
                valnode.setAttribute(vnname,"");
                acolHash[colRef].valueNode=valnode.attributes.getNamedItem(vnname);
            }
        }
        XMLUtil.forIn(ahtmlRow.getElementsByTagName("td"),tdhand);
    }


    if (!ge_node.id) ge_node.id=XMLUtil.getnewid();
    idstr=ge_node.id;
    formnode=document.createElement('form');
    formnode.id=idstr+'_form';
    formnode.appendChild(document.createTextNode("Loading..."));
    ge_node.parentNode.insertBefore(formnode,ge_node);
    JSUtil.addObserver(GenEdit3,'onload',this.init);
}