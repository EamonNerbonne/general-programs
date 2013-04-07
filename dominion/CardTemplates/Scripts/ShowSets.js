/// <reference path="knockout-2.2.1.debug.js" />
/// <reference path="DominionSets.js" />


Array.prototype.bind = function (mapper) {
	var ret = [], tmp;
	for (var i = 0; i < this.length; i++) {
		tmp = mapper(this[i]);
		for (var j = 0; j < tmp.length; j++)
			ret.push(tmp[j]);
	}
	return ret;
};

(function () {
	var cardClasses = {
		a: "action",
		v: "victory",
		t: "treasure",
		r: "reaction",
		k: "attack",
		d: "duration",
		D: "defense",
		C: "curse",
		R: "ruins",
		S: "shelter",
		c: "core",
	};
	var langs = ["English", "German", "Dutch", "French"];
	var lang = ko.observable("English");

	DominionSets.forEach(function (Set) {
		Set.translatedName = ko.computed(function () { return Set.Name[lang()] || "??" + Set.Name.English; });
		Set.Cards.forEach(function (card) {
			card.translatedName = ko.computed(function () { return card.Name[lang()] || "??" + card.Name.English; });
			card.Set = Set;
			var priceMatch = card.Price.match(/^(\d+)(P?)$/);
			card.CoinPrice = priceMatch && priceMatch[1];
			card.PotionPrice = priceMatch && priceMatch[2].replace('P', '▲') || "";
			card.OtherPrice = !priceMatch && card.Price || "";
			card.CssClasses = card.Type.split("").map(function (letter) {
				return cardClasses[letter] + "-card";
			}).join(" ");
		});
		Set.haveSet = ko.observable(true);
	});

	var byName = function (a, b) { return a.translatedName().localeCompare(b.translatedName()); };
	var bySet = function (a, b) { return a.Set.translatedName().localeCompare(b.Set.translatedName()); };
	var byNaN = function (a, b) { return Number(a.CoinPrice === null) - Number(b.CoinPrice === null); };
	var byCost = function (a, b) { return parseFloat(a.CoinPrice) - parseFloat(b.CoinPrice); };
	var byPotions = function (a, b) { return a.PotionPrice.length - b.PotionPrice.length; };
	var byCore = function (a, b) { return b.Type.indexOf("c") - a.Type.indexOf("c"); };

	var byPotionCoins = cmp(byNaN, byPotions, byCost);
	var byCoinPotions = cmp(byNaN, byCost, byPotions);

	function cmp(fs) {
		return Array.prototype.reduce.call(arguments, function (f, g) {
			return function (a, b) { return f(a, b) || g(a, b); };
		});
	}

	var sortOrders = [
		{ txt: "Core cards, Name", cmp: cmp(byCore, byName) },
		{ txt: "Core cards, Potions, Coins, Name", cmp: cmp(byCore, byPotionCoins, byName) },
		{ txt: "Core cards, Coins, Potions, Name", cmp: cmp(byCore, byCoinPotions, byName) },
		{ txt: "Core cards, Set, Name", cmp: cmp(byCore, bySet, byName) },
		{ txt: "Name", cmp: cmp(byName) },
		{ txt: "Potions, Coins, Name", cmp: cmp(byPotionCoins, byName) },
		{ txt: "Coins, Potions, Name", cmp: cmp(byCoinPotions, byName) },
		{ txt: "Set, Name", cmp: cmp(bySet, byName) }
	];
	var order = ko.observable(sortOrders[0]);

	var viewModel = {
		Languages: langs,
		Language: lang,
		Orders: sortOrders,
		Order: order,
		CardWidth: ko.observable(90),
		CardHeight: ko.observable(68),
		Cards: ko.computed(function () {
			var cmp = order().cmp;
			return DominionSets
				.bind(function (a) { return a.Cards; })
				.sort(cmp);
		}),
		Sets: DominionSets
	};

	ko.computed(function () {
		var width = Number(viewModel.CardWidth()) || 90;
		var height = Number(viewModel.CardHeight()) || 68;
		var style = '.card { width:' + width + 'mm; height:' + height + 'mm; }';
		var el = document.getElementById('CardStyleElem');
		while(el.lastChild)
			el.removeChild(el.lastChild);
		el.appendChild(document.createTextNode(style));
	}).extend({ throttle: 500 });

	ko.applyBindings(viewModel);
})();