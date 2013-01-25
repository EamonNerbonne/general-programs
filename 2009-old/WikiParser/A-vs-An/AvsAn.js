(function(){
	"use strict";
	var prefixTrie = function () {
		var trieRoot = {};
		//prefixes = [];
		var prefLines = document.getElementById("articles").firstChild.nodeValue.replace(/\s([^\[]*)\[(an?):(\d+):(\d+)\]/g, function (m, prefix, article, anCount, aCount) {
			var node = trieRoot;
			while (prefix) {
				var letter = prefix[0];
				var next = node[letter];
				if (!next)
					node[letter] = next = {};
				node = next;
				prefix = prefix.substring(1);
			}
			trie.data = { article: article, anCount: parseInt(anCount), aCount: parseInt(aCount) };
		});
		return trieRoot;
	}();

	function findTrieNode(word) {
		var suffix = word, node = prefixTrie, data;
		while (true) {
			data = node.data || data;
			if (!suffix) break;
			node = node[suffix[0]];
			if (!node) break;
			suffix = suffix.substring(1);
		}
		return { prefix: word.substring(0, word.length - suffix.length), data: data };
	}
	var articleEl = document.getElementById("article");
	var articleDetailEl = document.getElementById("articleDetail");
	var inputEl = document.getElementById("searchquery");

	inputEl.onkeyup = inputEl.oninput = function () {
		var input = document.getElementById("searchquery").value.replace(/^\s+|\s+$/g, "") + " ";
		var node = findTrieNode(input);
		articleEl.replaceChild(document.createTextNode(node.data.article), articleEl.firstChild);
		articleDetailEl.replaceChild(document.createTextNode(node.prefix + "[" + node.data.article + ":" + node.data.anCount + ":" + node.data.aCount + "]"), articleDetailEl.firstChild);
	}
})();