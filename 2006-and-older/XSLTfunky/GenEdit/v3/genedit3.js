try {
    JSUtil.verify();
    XMLUtil.verify();
} catch (e) {
    alert("Either JSUtil or XMLUtil are not correctly loaded.\n"+
          "This page might not work because of an html error or a browser problem.\n"+
          "Supported Browsers are:\n"+
          "    Internet Explorer 6 and Netscape 6 (a.k.a. Mozilla)");
    alert("Exception Details:\n"+e);
}


var GenEdit3;
///
 // GenEdit3 main Object
 //
 //     Events:
 //
 // onload:                 Called when all files have been loaded.
 //
 //     static functions:
 //
 // matchflag(flag, str):   Determines presence/absence of a particular flag by within a string
 // hasflag(flag, flagEl):Determines whether given element with attribute flags has the requested flag.
 //
 ///
function GenEdit3T() {
    this.tables=new Object();  //Hashtable: GenEdit3[idstr]==ge_node (with  ge_node.id==idstr)
    this.loaded=0;
    JSUtil.addObserver(window,'onload',
        JSUtil.methodRef(this,
            this.init));
}


// [static] function to determine whether a str contains a given flag
GenEdit3T.prototype.matchflag=function(flag,str) {//flag is alphanumeric only
    var regex=new RegExp('(^|;)'+flag+'(;|$)');
    return regex.test(str);
}

GenEdit3T.prototype.hasflag=function(flag,flagEl) {
    var regex=new RegExp('(^|;)'+flag+'(;|$)');
    return regex.test(flagEl.getAttribute("flags"));
}    

//[static] returns last index whose value is less than or equal to the requested value.
GenEdit3T.prototype.binSearch=function(array,value) {
        if(array.length<2) return 0;
        var maxpos=array.length,minpos=0,pos;
        while (minpos!=maxpos-1) {
            pos=(maxpos+minpos)/2;
            if(value<array[pos]) maxpos=pos;
            else minpos=pos;
        }
        return minpos;
    } 

//PRIVATE constructor helper.
GenEdit3T.prototype.init=function() {
        this.loaded=null;
        XMLUtil.forIn(document.getElementsByTagName("genedit3"),
            JSUtil.methodRef(this,
                function(ge_node) {
                    var table=new xslgeTableT(ge_node);
                    this.tables[table.getID()]=table;
                }
            )
        );
        if(this.onload) this.onload();
    }

GenEdit3=new GenEdit3T();



    ///
     // Constructor function for a generic edit table.
     ///
function xslgeTableT(ge_node) {
    this.ge_node=ge_node;
    this.formnode=document.createElement('form');
    this.colHash=new Object();//Hashtable colname->info about cols (xslgeColT)
    this.rowHash=new Object();//rowID-> xslgeRowT
    this.rowArr=new Array();//rowid in display order.
    this.colArr=new Array();//colid in display order
    this.loader=0;
    if (!ge_node.id) ge_node.id=XMLUtil.getnewid();
    this.formnode.id=ge_node.id+'_form';
    this.formnode.appendChild(document.createTextNode("Loading..."));
    ge_node.parentNode.insertBefore(this.formnode,ge_node);
    JSUtil.addObserver(this.formnode,'onsubmit',new Function("return false;"));//unnecessary?
    JSUtil.addObserver(this.formnode,'onreset',new Function("return false;"));

    XMLUtil.loadXMLDoc(ge_node.getAttribute("def"),
        JSUtil.methodRef(this,
            function(docObj){
                this.defDoc=docObj;
                this.loader++;
                this.testInit();
            }
        )
    );
    XMLUtil.loadXMLDoc(ge_node.getAttribute("src"),
        JSUtil.methodRef(this,
            function(docObj){
                this.loader++;
                this.srcDoc=docObj;
                this.testInit();
            }
        )
    );
    XMLUtil.loadXMLDoc("def2html.xsl?file="+ge_node.getAttribute("def"),
        JSUtil.methodRef(this,
            function(docObj){
                this.loader++;
                this.htmldef=docObj;
                this.testInit();
            }
        )
    );
}

xslgeTableT.prototype.getID=function(){
    return this.ge_node.id;
}

xslgeTableT.prototype.testInit=function(){
    if(this.loader==3) {
        this.loader=null;
        this.init();
    }
}

xslgeTableT.prototype.init=function() {
    this.colCount=0;
    //Construct Hash table of Cols.            
    JSUtil.forEach(
        XMLUtil.getChildByName(this.defDoc.getElementsByTagName("cols").item(0),"col"),
        JSUtil.methodRef(this,
            function(colEl) {
                var name=colEl.getAttribute("name");
                this.colHash[name]=new xslgeColT(colEl);
                this.colCount++;
            }
        )
    );

    //Insert the HTML-table.
    var table=this.htmldef.documentElement;
    //client XSLT:
    //var table=XMLUtil.transform(this.defDoc,GenEdit3.def2html).documentElement;
    this.htmldef=null;
    this.rowTemplate=XMLUtil.xml2html(
        XMLUtil.getChildByName(
            table.getElementsByTagName("tbody").item(0),
            "tr"
        )[0]
    );
    XMLUtil.forIn(this.rowTemplate.getElementsByTagName("td"),
        JSUtil.methodRef(this,
            function(tdEl) {
                var attr=tdEl.getAttributeNode("colref");
                if(!attr) return;
                this.colHash[attr.nodeValue].colNum=this.colArr.length;
                this.colArr.push(attr.nodeValue);
            }
        )
    );
    XMLUtil.removeAllChildNodes(table.getElementsByTagName("tbody").item(0));
    this.htmlTable=XMLUtil.xml2html(table);
    this.tbodyEl=this.htmlTable.getElementsByTagName("tbody").item(0);
    XMLUtil.removeAllChildNodes(this.formnode);
    this.formnode.appendChild(this.htmlTable);
    this.makeButtons();//make buttons after showing table to prevent IE6 JScript mem-leak
    this.makeSorters();

    //Construct Hash table of Rows
    JSUtil.forEach(XMLUtil.getChildByName(this.srcDoc.documentElement,"row"),
        JSUtil.methodRef(this,
            function(rowEl) {
                var rowid=XMLUtil.getnewid(),prop;
                var values=new Object();
                for(prop in this.colHash) {
                    values[prop]=rowEl.getAttribute(prop);
                }
                this.rowHash[rowid]=new xslgeRowT(values,rowid,this);
                this.rowHash[rowid].rowNum=this.rowArr.length;
                this.rowArr.push(rowid);
                //alert(this.rowHash[rowid]);
            }
        )
    );
    this.topRow=0;
    this.windowSize=Math.round(Number(this.defDoc.documentElement.getAttribute("window-size")));
    var i,tmp;
    for(i=0;i<this.windowSize;i++) {
        this.rowHash[this.rowArr[i]].show();
    }
    JSUtil.addObserver(this.formnode,'onkeydown',this.onkeydownFunc);
    this.editMode=false;//when true does typing stuff
    this.focusedNow=false;//set true when focused, then this.focusRow is set to a row and this.focusCol is set with a colName.
}

xslgeTableT.prototype.exposeRow=function(rowid){
    //newHtml.setAttribute("rowref",rowid);
    this.tbodyEl.appendChild(this.rowHash[rowid].html);
    this.rowArr.push(rowid);   
}
    
    
xslgeTableT.prototype.makeButtons=function(){
    this.button=new Object();//hash for button objs.
    XMLUtil.forIn(this.htmlTable.getElementsByTagName("div"),
        JSUtil.methodRef(this,function(divEl){
            if(divEl.getAttributeNode("button")) {
                this.button[divEl.getAttribute("button")]=divEl;
                divEl.className="xslge_Button";
            }
        })
    );//should set pageup and pagedown.
    JSUtil.addObserver(this.button.pageup,"onclick",JSUtil.methodRef(this,this.pageup));
    JSUtil.addObserver(this.button.pagedown,"onclick",JSUtil.methodRef(this,this.pagedown));
}

xslgeTableT.prototype.makeSorters=function(){//implements sorting buttons
    var thetr;
    XMLUtil.forIn(this.htmlTable.getElementsByTagName("tr"),
        function(trEl) {
            if(trEl.className=="XSLGE_colhead") thetr=trEl;
        }
    );
    XMLUtil.forIn(thetr.getElementsByTagName("th"),
        JSUtil.methodRef(this,function(thEl) {
            this.colHash[thEl.getAttribute("colref")].htmlColHead=thEl;
        })
    );
    for(colName in this.colHash) {
        JSUtil.addObserver(this.colHash[colName].htmlColHead,"onclick",this.sort_onclickFunc);
    
    }
}

xslgeTableT.prototype.sort_onclickFunc=function(e) {
    alert("onsortCalled");
    var obj=this,rowid,colName,tableid,row,table;
    while(obj.nodeName.toLowerCase()!='th') obj=obj.parentNode;
    colName=obj.getAttribute('colref');
    while(obj.nodeName.toLowerCase()!='form') obj=obj.parentNode;
    tableid=obj.id;
    tableid=tableid.substring(0,tableid.length-5);
    table=GenEdit3.tables[tableid];
    table.sortBy(colName);
}


xslgeTableT.prototype.sortBy=function(colName) {
    var change=new Object(),i,end=Math.min(this.topRow+this.windowSize,this.rowArr.length);
    for(i=this.topRow;i<end;i++) {
        change[this.rowArr[i]]=1;//hide
    }
    this.rowArr.sort(this.getComparer(colName));
    for(i=0;this.rowArr[i]!=this.focusRow.id;i++);
    this.topRow=Math.max(0,Math.min(i,this.rowArr.length-this.windowSize));
    end=Math.min(this.topRow+this.windowSize,this.rowArr.length);
    for(i=this.topRow;i<end;i++) {
        if(change[this.rowArr[i]]==null) change[this.rowArr[i]]=2;//show
        else change[this.rowArr[i]]=0;//do nothing
    }
    for(index in change) {
        if(change[index]==1) this.rowHash[index].hide();
        else if(change[index]==2) this.rowHash[index].show();
    }
    XMLUtil.removeAllChildNodes(this.tbodyEl);
    for(i=0;i<this.rowArr.length;i++) {
        this.tbodyEl.appendChild(this.rowHash[this.rowArr[i]].html);
        this.rowHash[this.rowArr[i]].rowNum=i;
    }
}

xslgeTableT.prototype.getComparer=function(colName) {
    var rowHash=this.rowHash,comparer=this.colHash[colName].comparer;
    return function(a,b) {//a,b are indexes into rowHash
        return comparer(rowHash[a].values[colName],rowHash[b].values[colName]);
    }
}

xslgeTableT.prototype.pagedown=function() {
    this.pageTo(Math.min(this.topRow+this.windowSize,this.rowArr.length-this.windowSize));
    this.rowHash[this.rowArr[this.topRow]].fieldHash[this.colArr[0]].focus();
}

xslgeTableT.prototype.pageup=function() {
    this.pageTo(Math.max(this.topRow-this.windowSize,0));
    this.rowHash[this.rowArr[this.topRow]].fieldHash[this.colArr[0]].focus();
}


//OLD
/*xslgeTableT.prototype.pageTo=function(rowNum) {
    var i,diff=Math.abs(rowNum-this.topRow);
    if(diff>=this.windowSize) {
        for(i=0;i<this.windowSize;i++) {
            this.rowHash[this.rowArr[this.topRow+i]].hide();
            this.rowHash[this.rowArr[this.rowNum+i]].show();
        }
    } else {
        for(i=0;i<diff;i++){
            if(rowNum<this.topRow) {
                this.rowHash[this.rowArr[rowNum+i]].show();
                this.rowHash[this.rowArr[rowNum+this.windowSize+i]].hide();
            } else {
                this.rowHash[this.rowArr[this.topRow+i]].hide();
                this.rowHash[this.rowArr[this.topRow+this.windowSize+i]].show();
            }
        }    
    }
    this.topRow=rowNum;
}
/*/xslgeTableT.prototype.pageTo=function(rowNum) {//allows "illegal" pages (to pages out of range).
    rowNum=Math.max(0,rowNum);
    var begin,end,change=new Object(),i;
    begin=this.topRow;
    end=Math.min(this.topRow+this.windowSize,this.rowArr.length);
    for(i=begin;i<end;i++) {
        change[this.rowArr[i]]=1;//hide
    }
    begin=rowNum;
    end=Math.min(rowNum+this.windowSize,this.rowArr.length);
    for(i=begin;i<end;i++) {
        if(change[this.rowArr[i]]==null) change[this.rowArr[i]]=2;//show
        else change[this.rowArr[i]]=0;//hide+show==no change;
    }
    for(index in change) {
        if(change[index]==1) this.rowHash[index].hide();
        else if(change[index]==2) this.rowHash[index].show();
    }
    this.topRow=rowNum;
}/**/

xslgeTableT.prototype.setEditMode=function(mode) {
    if(mode==this.editMode) return;
    this.editMode=mode;
    if(mode) {
        this.focusRow.fieldHash[this.focusCol].className="xslgeEditMode";
    } else {
        this.focusRow.fieldHash[this.focusCol].className="xslgeBrowseMode";
    }
}

xslgeTableT.prototype.onkeydownFunc=function(e) {//to be defined on "form"
    var tableid=this.id,table,TfocusRow,TtopRow;
    tableid=tableid.substring(0,tableid.length-5);
    table=GenEdit3.tables[tableid];
    if (!table.focusedNow) return;
    TfocusRow=table.focusRow;
    var key;//contains keycode
    if (window.event) key=window.event.keyCode;
    else key=e.which;
    //alert(key);
    if(!table.editMode) {
        if (e&&e.cancelable) e.preventDefault();
        switch (key) {
            case 38://up
                var TRrowNum=TfocusRow.rowNum;
                if(TRrowNum==0) return false;
                if(table.topRow==TfocusRow.rowNum) {
                    table.pageTo(table.topRow-1);
                }
                table.rowHash[table.rowArr[TRrowNum-1]].fieldHash[table.focusCol].focus();
                break;
            case 40://down
                var TRrowNum=TfocusRow.rowNum;
                if(TRrowNum==table.rowArr.length-1) return false;
                if(table.topRow+table.windowSize-1==TRrowNum) {
                    table.pageTo(table.topRow+1);
                }
                table.rowHash[table.rowArr[TRrowNum+1]].fieldHash[table.focusCol].focus();
                break;
            case 37://left
                if(table.colHash[table.focusCol].colNum==0) return false;
                TfocusRow.fieldHash[table.colArr[
                    table.colHash[table.focusCol].colNum-1
                    ]].focus();
                break;
            case 39://right
                if(table.colHash[table.focusCol].colNum+1==table.colArr.length) return false;
                TfocusRow.fieldHash[table.colArr[
                    table.colHash[table.focusCol].colNum+1
                    ]].focus();
                break;
            case 9://tab
                TfocusRow.fieldHash[table.colArr[
                    (table.colHash[table.focusCol].colNum+1)%table.colArr.length
                    ]].focus();
                break;
            case 33://pgup
                TfocusRow.fieldHash[table.focusCol].blur();
                table.pageup();
                break;
            case 34://pgdown
                TfocusRow.fieldHash[table.focusCol].blur();
                table.pagedown();
                break;
            default:
                if( (key<=90&&key>=65) ||//a-z
                    (key<=57&&key>=48) ||//0-9
                    (key>=96&&key<=111) ||//numpad with numlock on, whats 109?
                    (key>=186&&key<=192) ||//tilde,comman,period,colon,minus,equals
                    (key>=219&&key<=222) ||//square brackets, backslash, quote
                    (key==13||key==8||key==46||key==32)//enter,delete,backspace,space
                  ) {//turn on edit mode.
                    table.setEditMode(true);
                    return true;
                }
        }
        //if(e.cancelable) alert("yes");
        if(e&&e.cancelable) e.preventDefault();
        return false;
    } else {
        if(key==13) {//enter
            table.setEditMode(false);
            //e.preventDefault();
            return false;
        } else if (key==27) {//escape
            TfocusRow.values[table.focusCol]=TfocusRow.oldValues[table.focusCol];
            TfocusRow.fieldHash[table.focusCol].value=TfocusRow.oldValues[table.focusCol];
            table.setEditMode(false);
            //e.preventDefault();
            return false;
        }
        return true;
    }
}

function xslgeRowT(values,rowid,tableRef) {//Requires an otherwise fully set up table.
    this.tableRef=tableRef;
    this.oldValues=values;
    this.values=new Object();
    this.colClass=new Object();
    for (col in values) {
        this.values[col]=values[col];
        this.colClass=0;//0-unchanged,1-changed,2-focused
    }
    this.tdNodeHash=new Object();//will keep the references to the td-nodes by column name.
    this.fieldHash=new Object();//will keep the refernces to the parent of the valName by column name.
    this.id=rowid;
    this.status=2;//0: Showing, 1:Not Showing, been shown, 2:Never Shown before
    this.html=this.html.cloneNode(true);
    this.html.setAttribute("rowref",rowid);
}    
xslgeRowT.prototype.html=document.createElement("tr");
var tdtemp=document.createElement("td");
xslgeRowT.prototype.html.appendChild(tdtemp);
tdtemp.appendChild(document.createTextNode("Unloaded"));
tdtemp=null;
xslgeRowT.prototype.html.className="xslgeHiddenRow";

xslgeRowT.prototype.hide=function() {
    if(this.status!=0) return;
    this.status=1;
    this.html.className="xslgeHiddenRow";
}

xslgeRowT.prototype.show=function(){
    if(this.status==2) this.setupNow();
    this.status=0;
    this.html.className="xslgeShownRow";
}

xslgeRowT.prototype.setupNow=function() {
    if(this.status!=2) return;
    this.html=this.tableRef.rowTemplate.cloneNode(true);
    XMLUtil.forIn(this.html.getElementsByTagName("td"),
        JSUtil.methodRef(this,
            function(tdEl){
                var colRef=tdEl.getAttribute("colref"),tempnode;
                this.tdNodeHash[colRef]=tdEl;
                XMLUtil.removeAllChildNodes(tdEl);
                if(this.tableRef.colHash[colRef].editable) {
                    tempnode=document.createElement("input");
                    tempnode.setAttribute("size",this.tableRef.colHash[colRef].inputSize);
                    tempnode.setAttribute("value",this.values[colRef]);
                    tdEl.appendChild(tempnode);
                    this.fieldHash[colRef]=tempnode;
                } else {
                    this.fieldHash[colRef]=tdEl;
                    tempnode=document.createTextNode(this.values[colRef]);
                    tdEl.appendChild(tempnode);
                    this.fieldHash[colRef]=tempnode;
                }
            }
        )
    );
    var colName;
    for(colName in this.tableRef.colHash) {
        //alert(this.tableRef.colHash[colName].editable);
        this.classCheck(colName);
        if (this.tableRef.colHash[colName].editable) {//need to assign onchange etc. handlers
            JSUtil.addObserver(this.fieldHash[colName],'onchange',this.onchangeFunc);
            JSUtil.addObserver(this.fieldHash[colName],'onfocus',this.onfocusFunc);
            JSUtil.addObserver(this.fieldHash[colName],'onblur',this.onblurFunc);
        }
    }
    this.html.setAttribute("rowref",this.id);
    this.tableRef.exposeRow(this.id);
}

xslgeRowT.prototype.classCheck=function(colName) {
    this.setClass(colName,(this.values[colName]==this.oldValues[colName]?0:1));        
}

xslgeRowT.prototype.setClass=function(colName,stat) {
    if(stat!=this.colClass[colName]) {
        this.colClass[colName]=stat;
        this.tdNodeHash[colName].className=(stat==0)?("xslgeUnchangedField"):
                                          ((stat==1)?("xslgeChangedField"):
                                                     ("xslgeFocusedField"));
    }
}

xslgeRowT.prototype.onchangeFunc=function(e) {
    var obj=this,rowid,colName,tableid,row,table;
    while(obj.nodeName.toLowerCase()!='td') obj=obj.parentNode;
    colName=obj.getAttribute('colref');
    while(obj.nodeName.toLowerCase()!='tr') obj=obj.parentNode;
    rowid=obj.getAttribute('rowref');
    while(obj.nodeName.toLowerCase()!='form') obj=obj.parentNode;
    tableid=obj.id;
    tableid=tableid.substring(0,tableid.length-5);
    table=GenEdit3.tables[tableid];
    row=table.rowHash[rowid];
    row.values[colName]=row.fieldHash[colName].value;
    row.classCheck(colName);
    //alert((row.values[colName]==row.oldValues[colName]?0:1));
}

xslgeRowT.prototype.onfocusFunc=function(e) {
    //alert("onfocusCalled");
    var obj=this,rowid,colName,tableid,row,table;
    while(obj.nodeName.toLowerCase()!='td') obj=obj.parentNode;
    colName=obj.getAttribute('colref');
    while(obj.nodeName.toLowerCase()!='tr') obj=obj.parentNode;
    rowid=obj.getAttribute('rowref');
    while(obj.nodeName.toLowerCase()!='form') obj=obj.parentNode;
    tableid=obj.id;
    tableid=tableid.substring(0,tableid.length-5);
    table=GenEdit3.tables[tableid];
    row=table.rowHash[rowid];
    row.setClass(colName,2);
    table.focusedNow=true;
    table.focusRow=row;
    table.focusCol=colName;
}



xslgeRowT.prototype.onblurFunc=function(e) {
    var obj=this,rowid,colName,tableid,row,table;
    while(obj.nodeName.toLowerCase()!='td') obj=obj.parentNode;
    colName=obj.getAttribute('colref');
    while(obj.nodeName.toLowerCase()!='tr') obj=obj.parentNode;
    rowid=obj.getAttribute('rowref');
    while(obj.nodeName.toLowerCase()!='form') obj=obj.parentNode;
    tableid=obj.id;
    tableid=tableid.substring(0,tableid.length-5);
    table=GenEdit3.tables[tableid];
    row=table.rowHash[rowid];
    row.classCheck(colName);
    table.focusedNow=false;
    table.setEditMode(false);
}

function xslgeColT(colObj) {
    this.defObj=colObj;
    this.type=colObj.getAttribute("type");
    this.text=colObj.getAttribute("text");
    this.flags=colObj.getAttribute("flags");
    this.editable=GenEdit3.matchflag("editable",this.flags);
    this.isID=GenEdit3.matchflag("id",this.flags);
    if(this.editable) this.inputSize=colObj.getAttribute("input-size");
}

xslgeColT.prototype.otherComparer=function(x,y){
    x=""+x;
    y=""+y;
    if (x<y) return 1;
    else if(y<x) return -1;
    else return 0;
}

xslgeColT.prototype.comparer=xslgeColT.prototype.otherComparer;
