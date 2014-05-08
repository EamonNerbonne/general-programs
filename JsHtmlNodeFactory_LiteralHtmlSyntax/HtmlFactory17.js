var HtmlFactory17 = (function (D) {
	"use strict";
	var isArray = Array.isArray || function isArray(vArg) { return Object.prototype.toString.call(vArg) === "[object Array]"; };

	function unfoldArgumentInto(el, arr) {//can't check for window.Node: not available in IE8.
		var len = arr.length;
		for (var i = 0; i < len; i++) {
			var argVal = arr[i];
			if (argVal instanceof Node)
				el.appendChild(argVal);
			else if (isArray(argVal))
				unfoldArgumentInto(el, argVal);
			else if (argVal !== null && argVal !== undefined)
				el.appendChild(D.createTextNode(argVal));
		}
	}

	function mkElem(name) {
		return function (attrs) {
			var el = D.createElement(name);
			if (attrs)
				for (var key in attrs)
					el.setAttribute(key, attrs[key]);
			var len = arguments.length;
			for (var i = 1; i < len; i++) {
				var argVal = arguments[i];
				if (argVal instanceof Node)
					el.appendChild(argVal);
				else if (isArray(argVal))
					unfoldArgumentInto(el, argVal);
				else if (argVal !== null && argVal !== undefined)
					el.appendChild(D.createTextNode(argVal));
			}
			return el;
		}
	}

	var elNames = "a,abbr,address,article,aside,audio,b,blockquote,body,br,button,cite,code,del,details,dfn,div,em,fieldset,figcaption,figure,footer,form,h1,h2,h3,h4,h5,h6,head,header,hgroup,hr,html,i,img,input,ins,kbd,label,legend,li,link,mark,meta,meter,nav,noscript,ol,optgroup,option,p,pre,q,samp,script,section,select,small,source,span,strong,style,sub,summary,sup,table,tbody,td,textarea,tfoot,th,thead,time,title,tr,ul,var,video,area,base,bdo,canvas,caption,col,colgroup,command,datalist,dd,dl,dt,embed,iframe,keygen,map,menu,object,output,param,progress,rp,rt,ruby".split(",");
	var retval = {};
	for (var elI = 0; elI < elNames.length; elI++)
		retval[elNames[elI]] = mkElem(elNames[elI]);
	return retval;
})(document);
