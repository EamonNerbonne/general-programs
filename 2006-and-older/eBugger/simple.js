  function flipClass(htmlEl, className) {
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

function testClass(htmlEl,className) {
    var str=htmlEl.className,strArr;
    if(!str) return false;
    strArr=str.split(" ");
    for(var sClass in strArr) {
      if(strArr[sClass]==className) return true;
    }
    return false;
  }


function theadonclick(e) {
  flipClass(this.parentNode.parentNode.parentNode.parentNode,"collapsed");
  if(e && e.stopPropagation) e.stopPropagation();
  if(window.event) window.event.cancelBubble=true;
}
function initTree(){
    var theadEl = document.documentElement.getElementsByTagName("thead");
    for(var i=0;i<theadEl.length;i++){
     var thEl = theadEl.item(i).getElementsByTagName("th");
     for(var ii=0;ii<thEl.length;ii++) {
		var thE=thEl.item(ii);
         if(testClass(thE.parentNode.parentNode.parentNode.parentNode,"tree")  && testClass(thE,"symcol") )
           thE.onclick=theadonclick;
         if(testClass(thE.parentNode.parentNode.parentNode.parentNode,"tree")  && !testClass(thE,"symcol") )
           thE.onclick=colonclick;
     }
     var tdEl = theadEl.item(i).getElementsByTagName("td");
     for(var ii=0;ii<tdEl.length;ii++) {
		var tdE=tdEl.item(ii);
         if(testClass(tdE.parentNode.parentNode.parentNode.parentNode,"tree") )
           tdE.onclick=colonclick;
     }
     
   }
}

function colonclick(e) {
	var cnt=0;
	var curel=this;
	while(curel !== null) {
        	curel=curel.previousSibling; cnt++;
	}
	cnt--;
	//cnt now col number;
	curel=this.parentNode.parentNode.nextSibling;
	while(curel.nodeType != 1) curel=curel.nextSibling;
	sortTable(curel,cnt,false);
}
function cmpF(a,b) {
  return a[0]>b[0]?1:(a[0]<b[0]?-1:0);
}

function sortTable(tblEl, col, rev) {
  var exDisplay = tblEl.style.display;
  tblEl.style.display = "none";

  var i;
  var rowArray = [];
  var elCount = tblEl.rows.length;
  for (i = 0; i< elCount; i ++) {
    var row = tblEl.rows[0];
    var cellvalue = (col > 1) ? parseFloat(row.cells[col].firstChild.nodeValue) : row.cells[col].firstChild.firstChild.nodeValue;
    rowArray.push([cellvalue,row]);
    tblEl.removeChild(row);
  }

  rowArray.sort(cmpF);

  for(i = 0; i<elCount;i++) {
	var newrow = rowArray[i][1];
	if( i % 2 ^ (testClass(newrow,"odd")?0:1) == 1) flipClass(newrow, "odd");
	tblEl.appendChild(newrow);
  }

  tblEl.style.display = exDisplay;

  return false;
}
