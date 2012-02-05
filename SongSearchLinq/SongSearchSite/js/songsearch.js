var UpdateSongSearchResults = {};

$(document).ready(function ($) {
    var lastquery = "";
    var lasttop = "";
    var lastQHash = "";
    var lastOrdering = $("#orderingEl").val();
    var searchqueryEl = $("#searchquery")[0];

    function encodeQuery(str) { return encodeURIComponent(str.replace(/^\s+|\s+(?=\s|$)/g, "").toLowerCase()); }

    function updateResults() {
        var qHash = window.location.hash, qInput = searchqueryEl.value;
        qHash = qHash.match(/^\#\<.+\>$/) ? '' : qHash.substring(1);
        try {
            qHash = decodeURIComponent(qHash);
        } catch (e) { }
        var isQInputChanged = qInput != lastquery;
        var isQHashChanged = lastQHash != qHash;
        var newquery = !isQInputChanged && isQHashChanged ? qHash : qInput;
        lastQHash = qHash;
        var topNr = $("#shownumber").attr("value");
        var newOrdering = $("#orderingEl").val();
        if (newquery == lastquery && topNr == lasttop && newOrdering == lastOrdering) return;
        lasttop = topNr;
        lastOrdering = newOrdering;
        if (newquery != lastquery) {
            lastquery = newquery;
            if (!isQInputChanged)
                searchqueryEl["value"] = newquery;
            if (!isQHashChanged)
                window.setTimeout(function () { if (newquery == lastquery) updateHash(); }, 10000);
            for (var i = 0; i < queryTargets.length; i++)
                queryTargets[i](newquery, topNr);
        }
        $("#searchForm").submit();
    }

    UpdateSongSearchResults = updateResults;

    function QueryTarget(el, attrName, prefix, avoidEncode) {
        return function (search) { el[attrName] = (prefix || "") + (avoidEncode ? search : encodeQuery(search)); };
    }

    function updateHash() {
        window.location.hash = "#" + lastquery;
        window.songsearchSetPlaylistHashUri();
    }

    var queryTargets = [];
    $("a.matchLink").each(function (idx, aEl) { queryTargets.push(QueryTarget(aEl, "href", aEl.href, false)); });
    $("#searchForm input").keyup(updateResults).change(updateResults).blur(updateHash);
    $("#nodupCheckbox").change(function () { $("#searchForm").submit(); });
    $(window).bind("hashchange", updateResults);
    updateResults();
    if (!document.createElement("input").autofocus) $("[autofocus='autofocus']").first().each(function (i) { this.focus(); });
});