var HtmlFactory21 = (function (doc) {
	"use strict";
	var isArray = Array.isArray || function isArray(vArg) { return Object.prototype.toString.call(vArg) === "[object Array]"; };
	var slice = [].slice;
	function appendArrayInto(node, arr,i) {
		var len = arr.length;
		for (; i < len; i++) {
			var argVal = arr[i];
			if (argVal instanceof Node)
				node.appendChild(argVal);
			else if (isArray(argVal))
				appendArrayInto(node, argVal);
			else if (argVal !== null && argVal !== undefined)
				node.appendChild(doc.createTextNode(argVal));
		}
	}
	function appendArrayIntoEmpty(node, arr, i) {
		var len = arguments.length;
		for (; i < len; i++) {
			var argVal = arguments[i];
			if (argVal instanceof Node)
				node.appendChild(argVal);
			else if (isArray(argVal))
				appendArrayInto(node, argVal);
			else if (argVal !== null && argVal !== undefined)
				node.textContent = argVal;
			appendArrayInto(node, arr, i + 1);
		}
	}

	Node.prototype.add = function () {
		appendArrayInto(this, arr, 0);
		return this;
	}

	Element.prototype.attr = function (attrs) {
		for (var key in attrs)
			if (key.charCodeAt(0) === 111 && key.charCodeAt(1) === 110 && typeof val === "function")
				this.addEventListener(key.substr(2), val); //starts with on
			else
				this.setAttribute(key, attrs[key]);
		return this;
	}

	function mkFrag() {
		var node = doc.createDocumentFragment();
		appendArrayInto(node, arguments);
		return node;
	}


	function mkElem(name) {
		var factory = function () {
			var node = doc.createElement(name);
			var len = arguments.length;
			if (len > 0) {
				var argVal = arguments[1];
				if (argVal instanceof Node)
					node.appendChild(argVal);
				else if (isArray(argVal))
					appendArrayInto(node, argVal);
				else if (argVal !== null && argVal !== undefined)
					node.textContent = argVal;

				for (var i = 1; i < len; i++) {
					argVal = arguments[i];
					if (argVal instanceof Node)
						node.appendChild(argVal);
					else if (isArray(argVal))
						appendArrayInto(node, argVal);
					else if (argVal !== null && argVal !== undefined)
						node.appendChild(doc.createTextNode(argVal));
				}
			}
			return node;
		};
		factory.attr = function (attrs) {
			var el = doc.createElement(name);
			if (attrs !== null && attrs !== undefined) el.attr(attrs);

		};
	}

	var elNames = "a,abbr,address,article,aside,audio,b,blockquote,body,br,button,cite,code,del,details,dfn,div,em,fieldset,figcaption,figure,footer,form,h1,h2,h3,h4,h5,h6,head,header,hgroup,hr,html,i,img,input,ins,kbd,label,legend,li,link,mark,meta,meter,nav,noscript,ol,optgroup,option,p,pre,q,samp,script,section,select,small,source,span,strong,style,sub,summary,sup,table,tbody,td,textarea,tfoot,th,thead,time,title,tr,ul,var,video,area,base,bdo,canvas,caption,col,colgroup,command,datalist,dd,dl,dt,embed,iframe,keygen,map,menu,object,output,param,progress,rp,rt,ruby".split(",");
	var retval = {
		$frag: mkFrag,
		$elem: mkElem
	};
	for (var elI = 0; elI < elNames.length; elI++)
		retval[elNames[elI]] = mkElem(elNames[elI]);
	return retval;
})(document);
