function onloadHand(){  
  var ctrldown=false;
  var shiftdown=false;
  var editing=false;
  document.addEventListener("keydown",function(e){
    switch(e.which) {
      case 16:shiftdown=true;break;
      case 17:ctrldown=true;break;
      case 192:if(!ctrldown) break;
        editing=!editing;
        document.designMode=editing?'on':'off';
        break;
      case 66: 
        if(ctrldown) {
          document.execCommand("bold",false,null);
          e.preventDefault();
          e.stopPropagation();
        }
        break;
      case 73: 
        if(ctrldown) {
          document.execCommand("italic",false,null);
          e.preventDefault();
          e.stopPropagation();
        }
        break;
    }
  },true);
  document.addEventListener("keyup",function(e){
    switch(e.which) {
      case 16:shiftdown=false;break;
      case 17:ctrldown=false;break;
    }
  },true);
 }
