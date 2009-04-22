function forIn(items,handler) {
  var cnt=items.length,i;
  for(i=0;i<cnt;i++)handler(items.item(i));
}

function loadXMLDoc(load_url,load_handler)
{
    var xmlDoc;
	if (document.implementation && document.implementation.createDocument) {
	    //alert("implementation.createDocument");
		xmlDoc = document.implementation.createDocument("", "", null);
	    xmlDoc.onload=load_handler;
	    xmlDoc.load(load_url);
	}else if (window.ActiveXObject)	{//annoying IE xmldom limitation, workaround using ActiveX
	    //alert("ActiveX workaround");
		xmlDoc = new ActiveXObject("MSXML2.DOMDocument.3.0");
		xmlDoc.async=false;
		xmlDoc.preserveWhiteSpace=true;//unfortunate, Mozilla always does this, so to make things
		                               //more consistent we do it here too.
        xmlDoc.load(load_url);
        load_handler.call(xmlDoc);
 	}else{//uhuh
		alert('Your browser can\'t handle this script');
	}
}

var idnum=0;
function getnewid() {
	idnum++;
	return "genedit"+idnum;
}

var xmlfile;
function init() {
  forIn(document.getElementsByTagName("emn:genedit"),ge_init);
  document.onclick=catchIt;
}


function ge_init(ge_node) {
  var idstr=getnewid();
  var formnode;
  formnode=document.createElement("form");
  formnode.id=idstr;
  formnode.setAttribute("onsubmit","saveEdit(); return false");
  ge_node.parentNode.insertBefore(formnode,ge_node);
  formnode.appendChild(ge_node);//move node into form to make it easier to find.
  loadXMLDoc(ge_node.getAttribute("src"),new Function("constructTable.call(this,\""+idstr+"\");"));
}

function constructTable(idstr) {
    var tablenode,trnode,fieldnode,i,ii,tbody;
    //alert(this.xml);
    var cols=this.documentElement.attributes.length,rowset=this.getElementsByTagName("row");
    var rows=rowset.length;
    tablenode=document.createElement("table");
    tablenode.className="genedit";
    tbody=document.createElement("tbody");
    tablenode.appendChild(tbody);
    trnode=document.createElement("tr");
	for(i=0;i<cols;i++) {
		fieldnode=document.createElement("th");
		fieldnode.appendChild(document.createTextNode(this.documentElement.getAttribute("d"+(i+1))));
		trnode.appendChild(fieldnode);
	}
    tbody.appendChild(trnode);
	for(ii=0;ii<rows;ii++) {
		trnode=document.createElement("tr");
		for(i=0;i<cols;i++) {
			fieldnode=document.createElement("td");
			fieldnode.appendChild(document.createTextNode(this.getElementsByTagName("row").item(ii).getAttribute("d"+(i+1))));
			trnode.appendChild(fieldnode);
		}
		tbody.appendChild(trnode);
	}
	var formset=document.getElementsByTagName("form");
	for(i=0;i<formset.length;i++) {
		if(formset.item(i).getAttribute("id")==idstr) break;
	}
	formset.item(i).appendChild(tablenode);
	tablenode.setAttribute("class","genedit");
}

var editing  = false;

function removeAllChildNodes(node) {
    while (node.hasChildNodes()) {
        node.removeChild(node.firstChild);    
    }
}

function catchIt(e)
{
	if (!document.getElementById || !document.createElement) return;//unsupported

	if (window.Event) var obj = e.target;//Mozilla supports spec
	else var obj = window.event.srcElement;//Explorer workaround

	if (editing) {//save old edit
		if ( obj.id != 'tabedit1field') saveEdit();
		else return;//already editing right field!
	}

	while (obj.nodeType != 1) obj = obj.parentNode;	//bubble up to next element (not targetting text directly)
	if (obj.tagName.toLowerCase() != 'td') return; //we only edit in tables
		
	var anc,count;
	anc = obj;
	count=0;
	while (anc.tagName.toLowerCase() != 'form' && anc.tagName.toLowerCase() != 'body' ) {
		anc=anc.parentNode;
		count++;
	}
	if (anc.firstChild.tagName.toLowerCase() != 'emn:genedit') return;//didn't click in form
	//   form/table/TBODY/tr/td/p/g/a/b/c  i.e. go 4 steps back down for the actual td
	//take note of weird tbody tag (invisible?)
	if (count!=4) return;//buggy html
	//if (count < 4) return;//buggy HTML
	//count-=4;
	//while(count>0) {
	//	obj=obj.parentNode;
	//	count--;
	//}//now at td
	
	var text = obj.firstChild.nodeValue;
	var infield = document.createElement('input');
	infield.setAttribute('type','text');
	infield.setAttribute('size',Math.max(text.length+5,20));
	infield.id='tabedit1field';
	removeAllChildNodes(obj);
	obj.appendChild(infield);
	infield.value = text;
	infield.focus();
	//infield.onblur=saveEdit;
	window.onblur=saveEdit;
	editing = true;
}

function saveEdit()
{
	if (!editing) return;
	var inp=document.getElementById('tabedit1field')
	var text = inp.value;
	var tdel = inp.parentNode;
    removeAllChildNodes(tdel);
    tdel.appendChild(document.createTextNode(text));
	editing = false;
}
