var HtmlFactory6 = (function (D) {
	"use strict";
	function unfoldArgumentInto(el, arr) {//can't check for window.Node: not available in IE8.
		var len = arr.length;
		for (var i = 0; i < len; i++) {
			var argVal = arr[i];
			if (argVal != null) {
				if (argVal.nodeType)
					el.appendChild(argVal);
				else if (Array.isArray(argVal))
					unfoldArgumentInto(el, argVal);
				else
					el.appendChild(D.createTextNode(argVal));
			}
		}
		return el;
	}

	function E(name, attrs, arr) {
		var el = D.createElement(name);
		if (attrs)
			for (var key in attrs)
				el.setAttribute(key, attrs[key]);
		var len = arr.length;
		for (var i = 0; i < len; i++) {
			var argVal = arr[i];
			if (argVal != null) {
				if (argVal.nodeType)
					el.appendChild(argVal);
				else if (Array.isArray(argVal))
					unfoldArgumentInto(el, argVal);
				else
					el.appendChild(D.createTextNode(argVal));
			}
		}
		return el;
	}
	return E;
})(document);
