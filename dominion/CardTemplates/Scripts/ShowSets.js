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

var cards = DominionSets.bind(function (a) {
	return a.Cards.map(function (c) {
		c.SetName = a.Name;
		return c;
	});
});
cards.sort(function (a, b) { return a.Name.English.localeCompare(b.Name.English); });

ko.applyBindings({ cards: cards });
