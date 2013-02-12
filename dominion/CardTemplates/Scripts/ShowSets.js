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
	DominionSets.forEach(function (set) {
		set.Cards.forEach(function (card) {
			card.Set = set;
			var priceMatch = card.Price.match(/^(\d+)(P?)$/);
			card.CoinPrice = priceMatch && priceMatch[1];
			card.PotionPrice = priceMatch && priceMatch[2].replace('P','▲');
			card.OtherPrice = !priceMatch && card.Price || "";
		});
		set.haveSet = ko.observable(true);
	});
	var viewModel = {
		Cards: DominionSets
					.bind(function (a) { return a.Cards; })
					.sort(function (a, b) { return a.Name.English.localeCompare(b.Name.English); }),
		Sets: DominionSets
	};


	ko.applyBindings(viewModel);
})();