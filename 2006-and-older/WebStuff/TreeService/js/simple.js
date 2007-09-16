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
		
		
   function initTree(){
    var divEl = document.documentElement.getElementsByTagName("div");
    for(var i=0;i<divEl.length;i++){
     var divE=divEl.item(i);
     if(testClass(divE,"treehead")){
      divE.onclick=function(e) {
       flipClass(this.parentNode,"collapsed");
       if(e && e.stopPropagation) e.stopPropagation();
       if(window.event) window.event.cancelBubble=true;
      }
     }
    }
   }
