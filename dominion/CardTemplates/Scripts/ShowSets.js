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
		v: "victory",
		t: "treasure",
		a: "action",
		r: "reaction",
		k: "attack",
		d: "duration",
		D: "defense",
		C: "curse",
		R: "ruins",
		S: "shelter",
	};

	DominionSets.forEach(function (Set) {
		Set.Cards.forEach(function (card) {
			card.Set = Set;
			var priceMatch = card.Price.match(/^(\d+)(P?)$/);
			card.CoinPrice = priceMatch && priceMatch[1];
			card.PotionPrice = priceMatch && priceMatch[2].replace('P', '▲');
			card.OtherPrice = !priceMatch && card.Price || "";
			card.CssClasses = card.Type.split("").map(function(letter) {
				return cardClasses[letter] + "-card";
			}).join(" ");
		});
		Set.haveSet = ko.observable(true);
	});
	var viewModel = {
		Cards: DominionSets
					.bind(function (a) { return a.Cards; })
					.sort(function (a, b) { return a.Name.English.localeCompare(b.Name.English); }),
		Sets: DominionSets
	};


	ko.applyBindings(viewModel);
})();