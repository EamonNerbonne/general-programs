$(document).ready(function ($) {
    var lastquery = "";
    var lasttop = "";
    var lastQHash = "";
    var lastOrdering = $("#orderingEl").val();
    var searchqueryEl = $("#searchquery")[0];

    function encodeQuery(str) { return encodeURIComponent(str.replace(/^\s+|\s+(?=\s|$)/g, "").toLowerCase()); }

    function updateResults() {
        var qHash = window.location.hash.substring(1), qInput = searchqueryEl.value;
        try {
            qHash = decodeURIComponent(qHash);
        } catch (e) { }
        var newquery = qInput == lastquery && lastQHash != qHash ? qHash : qInput;
        lastQHash = qHash;
        var topNr = $("#shownumber").attr("value");
        var newOrdering = $("#orderingEl").val();
        if (newquery == lastquery && topNr == lasttop && newOrdering == lastOrdering) return;
        lasttop = topNr;
        lastOrdering = newOrdering;
        if (newquery != lastquery) {
            lastquery = newquery;
            for (var i = 0; i < queryTargets.length; i++)
                queryTargets[i](newquery, topNr);
        }
        $("#searchForm").submit();
    }
    window.updateResultsGlobal = updateResults;

    function QueryTarget(el, attrName, prefix, avoidEncode) {
        return function (search) { el[attrName] = (prefix || "") + (avoidEncode ? search : encodeQuery(search)); };
    }

    function updateHash() { window.location.hash = "#" + lastquery; }

    var queryTargets = [QueryTarget(searchqueryEl, "value", null, true),
        function (newsearch) { window.setTimeout(function () { if (newsearch == lastquery) updateHash(); }, 10000); }
    ];
    $("a.matchLink").each(function (idx, aEl) { queryTargets.push(QueryTarget(aEl, "href", aEl.href, false)); });
    $("#searchForm input").keyup(updateResults).blur(updateHash);
    $(window).bind("hashchange", updateResults);
    updateResults();
    if (!document.createElement("input").autofocus) $("[autofocus='autofocus']").first().each(function (i) { this.focus(); });
});