var HtmlFactory4 = (function (D) {
	"use strict";
	var isArray = Array.isArray || function (vArg) { return Object.prototype.toString.call(vArg) === "[object Array]"; };

	function unfoldArgumentInto(el, arr) {//can't check for window.Node: not available in IE8.
		var len = arr.length;
		for (var i = 0; i < len; i++) {
			var argVal = arr[i];
			if (argVal != null) {
				if (argVal.nodeType)
					el.appendChild(argVal);
				else if (isArray(argVal))
					unfoldArgumentInto(el, argVal);
				else
					el.appendChild(D.createTextNode(argVal));
			}
		}
		return el;
	}


	//Creates an element; returns a function for filling it.
	function forElem(elemName) {
		//Adds JS property/value pairs as attributes.  (onXXX properties that are functions are instead added as event handlers.)
		//returns a content-addition function
		function addAttrThenContent(attrContent) {
			//Adds all arguments to the element's content.
			return function (content) {
				var el = D.createElement(elemName);
				if (typeof attrContent === "object")
					for (var prop in attrContent) {
						var propVal = attrContent[prop];
						if (propVal !== undefined) {
							if (typeof propVal === "function" && prop.substr(0, 2) === 'on') {
								el.addEventListener(prop.substr(2), propVal, false);
							} else {
								el.setAttribute(prop, propVal);
							}
						}
					}

				return unfoldArgumentInto(el, content);
			};
		}

		function justAddContent(content) { return unfoldArgumentInto(D.createElement(elemName), content); }
		justAddContent.attrs = addAttrThenContent;
		return justAddContent;
	}

	var elNames = "a,abbr,address,article,aside,audio,b,blockquote,body,br,button,cite,code,del,details,dfn,div,em,fieldset,figcaption,figure,footer,form,h1,h2,h3,h4,h5,h6,head,header,hgroup,hr,html,i,img,input,ins,kbd,label,legend,li,link,mark,meta,meter,nav,noscript,ol,optgroup,option,p,pre,q,samp,script,section,select,small,source,span,strong,style,sub,summary,sup,table,tbody,td,textarea,tfoot,th,thead,time,title,tr,ul,var,video,area,base,bdo,canvas,caption,col,colgroup,command,datalist,dd,dl,dt,embed,iframe,keygen,map,menu,object,output,param,progress,rp,rt,ruby".split(",");
	var retval = {
		//Adds all arguments to a document fragment.
		$fragment: function (content) { return unfoldArgumentInto.apply(D.createDocumentFragment(), content); },
		$elem: forElem
	};
	for (var elI = 0; elI < elNames.length; elI++)
		retval[elNames[elI]] = forElem(elNames[elI]);
	//creates a document fragment; appends all arguments as content.
	return retval;
})(document);
