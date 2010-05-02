$(document).ready(function ($) {
    var lastquery = "";
    var lasttop = "";

    function encodeQuery(str) { return encodeURIComponent(str.replace(/^\s+|\s+$/g, "").toLowerCase()); }

    function updateResults() {
        var qHash = window.location.hash.substring(1), qInput = document.getElementById("searchquery").value;
        try {
            qHash = decodeURIComponent(qHash);
        } catch (e) { }
        var newquery = qInput == lastquery ? qHash : qInput;
        var topNr = $("#shownumber").attr("value");
        if (newquery == lastquery && topNr == lasttop) return;
        lastquery = newquery;
        lasttop = topNr;
        for (var i = 0; i < queryTargets.length; i++)
            queryTargets[i].update(newquery, topNr);
        $("#searchForm").submit();
    }

    function QueryTarget(el, attrName, pattern, avoidEncode) {
        this.el = el;
        this.attrName = attrName;
        this.pattern = pattern ? pattern : el[attrName] + "{0}";
        this.avoidEncode = avoidEncode;
        this.update = function QueryTarget_update() {
            var newval = this.pattern;
            for (var i = 0; i < arguments.length; i++)
                newval =
                newval.replace(
                    "{" + i + "}",
                    this.avoidEncode ? arguments[i] : encodeQuery(arguments[i])
                );
            if (this.el[this.attrName] != newval) this.el[this.attrName] = newval;
        };
    }


    var searchqueryEl = $("#searchquery")[0];
    var queryTargets = [
        new QueryTarget(searchqueryEl, "value", "{0}", true),
        new QueryTarget(window.location, "hash", "{0}", true),
    ];
    $("a.matchLink").each(function (idx, aEl) { queryTargets.push(new QueryTarget(aEl, "href")); });
    $("#searchForm input").keyup(updateResults);
    $(window).bind("hashchange", updateResults);
    updateResults();
    if (!document.createElement("input").autofocus) $("[autofocus='autofocus']").first().each(function (i) { this.focus(); });

});