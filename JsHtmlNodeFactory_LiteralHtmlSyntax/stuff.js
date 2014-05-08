/// <reference path="HtmlFactory.js" />
K = HtmlFactory;
var c = 0;
function mkArr(depth, length) {
	var retval = [];
	c++;
	for (var i = 0; i < length; i++) {
		var kid_length = (c * 1234567 + depth * 137 + length * 1337 + i) % (length + depth) % depth;
		retval.push(mkArr(depth - 1, kid_length));
	}
	return { depth: depth, c: c, kids: retval };
}
var structure = mkArr(8, 13);

function mkDom(obj) {
	return K.span.attrs({ title: obj.depth })(obj.c, " [", obj.kids.map(mkDom), "] ");
}
mkDom(structure);



//see: http://jsperf.com/array-type-checking/8
//see: http://jsperf.com/js-coerce-null/10
//see: http://jsperf.com/instanceof-vs-typeof-nodetype/2
