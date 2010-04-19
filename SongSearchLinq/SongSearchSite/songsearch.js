var lastquery = "";
var lasttop = "";

function encodeQuery(str) {
    return encodeURIComponent(str.replace(/^\s+|\s+$/g, "").toLowerCase());
}

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

var queryTargets = [];

function init() {
    var searchqueryEl = $("#searchquery")[0];
    queryTargets.push(new QueryTarget(searchqueryEl, "value", "{0}", true));
    queryTargets.push(new QueryTarget(window.location, "hash", "{0}", true));
    $("a.matchLink").each(function (aEl) { queryTargets.push(new QueryTarget(aEl, "href")); });



    $("#searchForm input").keyup(updateResults);
    window.onhashchange = updateResults;

    updateResults();
    searchqueryEl.focus();
}

function guessMime(extension) {
    if (extension)
        switch (extension.toLowerCase()) {
        case ".mp3": return "audio/mpeg";
        case ".wma": return "audio/x-ms-wma";
        case ".wav": return "audio/wav";
        case ".ogg": return "application/ogg";
        case ".mpc":
        case ".mpp":
        case ".mp+": return "audio/x-musepack";
    }
    return null;
}

function getExtension(url) {
    dotIdx = url.lastIndexOf(".");
    if (dotIdx == -1) return null;
    else return url.substring(dotIdx);
}

function htmlAudioSupported(type) {
    try {
        if (!Audio)
            return false;
        var audioStub = new Audio("");
        if (!(audioStub.canPlayType))
            return false;
        var canPlay = audioStub.canPlayType(type);
        return canPlay && audioStub.canPlayType(type) != "no";
    } catch (e) {
        return false;
    }
}

function PlaySong(songlabel, songfilename, songurl, imgObj) {
    var type = guessMime(getExtension(songurl));
    var audioEl;
    if (htmlAudioSupported(type)) {
        audioEl = $("<audio autoplay controls>").attr("src", songurl);
    } else if (type == "audio/mpeg") {
        var movieUrl = "xspf_player_slim.swf?autoplay=true&song_url=" + songurl + "&song_title=" + encodeURIComponent(songlabel);
        audioEl = 
            $("<object type='application/x-shockwave-flash' width='400' height='16'><param name='movie'/></object>")
            .attr("data", movieUrl)
            .find("param").attr("value", movieUrl).end()
            ;
    } else {
        audioEl = $("<b>UNSUPPORTED</b>");
    }
    $("#audio-container").text(songlabel).append(audioEl);
}

$(document).ready(init);
