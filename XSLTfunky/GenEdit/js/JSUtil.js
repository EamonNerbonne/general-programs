//JSUtil.addObserver(object, eventname, handler); adds an event handler to an event, allows stacking.
//e.g....addObserver(window,  'onload', [a function]);

//JSUtil.getObservers(object, eventname, handler); returns an updateable array of all registered handlers.
/*
var JSKEY_TAB=9;
var JSKEY_ENTER=13;
var JSKEY_SHIFT=16;
var JSKEY_CTRL=17;
var JSKEY_ALT=18;
var JSKEY_LEFT=37;
var JSKEY_UP=38;
var JSKEY_RIGHT=39;
var JSKEY_DOWN=40;
var JSKEY_0=48;
var JSKEY_1=49;
var JSKEY_2=50;
var JSKEY_3=51;
var JSKEY_4=52;
var JSKEY_5=53;
var JSKEY_6=54;
var JSKEY_7=55;
var JSKEY_8=56;
var JSKEY_9=57;
var JSKEY_A=65;
var JSKEY_B=66;
var JSKEY_C=67;
var JSKEY_D=68;
var JSKEY_E=69;
var JSKEY_F=70;
var JSKEY_G=71;
var JSKEY_H=72;
var JSKEY_I=73;
var JSKEY_J=74;
var JSKEY_K=75;
var JSKEY_L=76;
var JSKEY_M=77;
var JSKEY_N=78;
var JSKEY_O=79;
var JSKEY_P=80;
var JSKEY_Q=81;
var JSKEY_R=82;
var JSKEY_S=83;
var JSKEY_T=84;
var JSKEY_U=85;
var JSKEY_V=86;
var JSKEY_W=87;
var JSKEY_X=88;
var JSKEY_Y=89;
var JSKEY_Z=90;
var JSKEY_NUMLOCK=144;
var JSKEY_CAPSLOCK=20;
var JSKEY_WINSTART=91
var JSKEY_WINCONTEXT=93;
var JSKEY_INSERT=45;
var JSKEY_HOME=36;
var JSKEY_PGUP=33;
var JSKEY_PGDN=34;
var JSKEY_DEL=46;
var JSKEY_END=35;
var JSKEY_BKSP=8;
var JSKEY_COMMA=188;
var JSKEY_PERIOD=190;
var JSKEY_SLASH=191;
var JSKEY_SEMICOLON=186;
var JSKEY_QUOTE=222;
var JSKEY_SQUAREOPEN=219;
var JSKEY_SQUARECLOSE=221;
var JSKEY_MINUS=189;
var JSKEY_EQUALS=187;
var JSKEY_BACKSLASH=220;
var JSKEY_ESCAPE=27;
var JSKEY_F1=112;
var JSKEY_F2=113;
var JSKEY_F3=114;
var JSKEY_F4=115;
var JSKEY_F5=116;
var JSKEY_F6=117;
var JSKEY_F7=118;
var JSKEY_F8=119;
var JSKEY_F9=120;
var JSKEY_F10=121;
var JSKEY_F11=122;
var JSKEY_F12=123;
var JSKEY_SCROLLLOCK=145;
var JSKEY_BREAK=19;
var JSKEY_NUMPAD_SLASH=111;
var JSKEY_NUMPAD_STAR=106;
var JSKEY_NUMPAD_MINUS=109;
var JSKEY_NUMPAD_PLUS=107;
var JSKEY_NUMOFF_5=12;//only when numlock off!!7
var JSKEY_NUMON_DOT=110;//Only when numlock on!!
var JSKEY_NUMON_0=96;
var JSKEY_NUMON_1=97;
var JSKEY_NUMON_2=98;
var JSKEY_NUMON_3=99;
var JSKEY_NUMON_4=100;
var JSKEY_NUMON_5=101;
var JSKEY_NUMON_6=102;
var JSKEY_NUMON_7=103;
var JSKEY_NUMON_8=104;
var JSKEY_NUMON_9=105;
var JSKEY_SPACE=32;
//var JSKEY_NUMPAD_
//var JSKEY_NUMPAD_
//var JSKEY_NUMPAD_
//var JSKEY_NUMPAD_
*/

//This file contains a container object.  All functionality can be reached through it,
//e.g. JSUtil.myfunc();

var JSUtil=new (function(){
    this._eventHandler=new Object();//INTERNAL:will hash handlers by name. (one entry per name!)
    this._HandlerArrArr=new Array();//INTERNAL:array; for each handler a new handlers array is held
    this.keys=new Array(256);//Array which holds which key is currently down.
    for(var i=0;i<256;i++) {
        this.keys[i]=false;
    }
    //this.log="";
});

JSUtil.verify=function(){
    function test(hi,bye) {
    }
    if(!test.call||!test.apply) throw "Requires Function calling/applying support";
}


if(window.addEventListener)
    JSUtil.addObserver=function(theobj, eventname, handler) {
        theobj.addEventListener(eventname.substr(2), handler, false);
    }
else {
    JSUtil.addObserver=function(theobj, eventname, handler) {
        var thearr=JSUtil._getObservers(theobj,eventname);
        thearr.push(handler);
    }

    //INTERNAL
    JSUtil._getObservers=function(theobj, eventname) {
        if(theobj["JSUtil->"+eventname]==null) {
            var temp=new Array();
            theobj["JSUtil->"+eventname]=JSUtil._HandlerArrArr.length;
            JSUtil._HandlerArrArr.push(temp);
            if(theobj[eventname]!=null) temp.push(theobj[eventname]);
            theobj[eventname]=JSUtil._lookuphandler(eventname);
        }
        return JSUtil._HandlerArrArr[theobj["JSUtil->"+eventname]];
    }
    
    //INTERNAL
    JSUtil._lookuphandler=function(name) {
        if (JSUtil._eventHandler[name]===undefined) 
            JSUtil._inithandler(name);
        return JSUtil._eventHandler[name];
    }
    //INTERNAL
    JSUtil._inithandler=function(eventname) {//INTERNAL
        JSUtil._eventHandler[eventname]=function(){
            var i,afunc,retval=true,funcarr,temp;
            funcarr=JSUtil._HandlerArrArr[this["JSUtil->"+eventname]];
            for(i=0;i<funcarr.length;i++) {
                afunc=(funcarr[i]);
                temp=afunc.apply(this,arguments);
                retval=(temp===undefined||temp!==false)&&retval;
            }
            return retval;
        }
    }//INTERNAL
}
JSUtil.forEach=function (items,handler) {
    var i;
    for(i=0;i<items.length;i++) handler(items[i]);
}

JSUtil.showTextInWindow=function(text) {
    var winref=window.open();
    winref.document.write(text);//use view source then.
}

JSUtil.methodRef=function(object,func) {
    return function(){func.apply(object,arguments);}
}//Beware! never assign such functions to DOM objects, may cause mem-leaks in IE + Mozilla

//Now for the "keys" observer:
/*JSUtil.addObserver(document,'onkeydown',function(e){
    var key;
    if (window.event) key=window.event.keyCode;
    else key=e.which;
    //if(!(JSUtil.keys[key])) {
    JSUtil.keys[key]=true;
//        JSUtil.log+=key+"d, "
    //}
});

JSUtil.addObserver(document,'onkeyup',function(e){
    var key;
    if (window.event) key=window.event.keyCode;
    else key=e.which;
    JSUtil.keys[key]=false;
    //JSUtil.log+=key+"u, "
});*/
