var lastquery = "";
var lasttop = "20";
function queryUrl(topNr, query) {
    return "list.xml?top=" + encodeURIComponent(topNr) +
                    "&view=xslt&remote=allow&q=" + encodeURIComponent(query);
}
var extm3ulink = "list.m3u?extm3u=true&remote=allow&q=";
var extm3u8link = "list.m3u8?extm3u=true&remote=allow&q=";
function updateResults() {
    var newquery;
    newquery = document.getElementById("searchquery").value;
    newquery = newquery.replace(/^\s+|\s+$/g, "");
    newquery = newquery.toLowerCase();
    var topNr = document.getElementById("shownumber").value;
    if (newquery == lastquery && topNr == lasttop) return;
    lastquery = newquery;
    lasttop = topNr;
    window.frames["resultsview"].location.href = queryUrl(lasttop, lastquery);
    var m3uEl = document.getElementById("m3u");
    m3uEl.href = extm3ulink + encodeURIComponent(lastquery);
    var m3u8El = document.getElementById("m3u8");
    m3u8El.href = extm3u8link + encodeURIComponent(lastquery);
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
    var idxDoc = window.document;
    var audioContainer = idxDoc.getElementById("audio-container");

    var type = guessMime(getExtension(songurl));
    var audioEl;
    if (htmlAudioSupported(type)) {
        audioEl = idxDoc.createElement("audio");
        audioEl.setAttribute("autoplay", "autoplay");
        audioEl.setAttribute("controls", "controls");
        audioEl.setAttribute("src", songurl);
    } else if (type == "audio/mpeg") {
        var audioEl = idxDoc.createElement("object");
        var movieUrl = "xspf_player_slim.swf?autoplay=true&song_url=" + songurl + "&song_title=" + encodeURIComponent(songlabel);
        audioEl.setAttribute("type", "application/x-shockwave-flash");
        audioEl.setAttribute("data", movieUrl);
        audioEl.setAttribute("width", "100%");
        audioEl.setAttribute("style", "width:100%");

        audioEl.setAttribute("height", "16");
    }

    while (audioContainer.childNodes.length > 0)
        audioContainer.removeChild(audioContainer.childNodes[0]);
    audioContainer.appendChild(idxDoc.createTextNode(songlabel));
    if (audioEl)
        audioContainer.appendChild(audioEl);
    else {
        var bEl = idxDoc.createElement("b");
        bEl.appendChild(idxDoc.createTextNode("UNSUPPORTED"));
        audioContainer.appendChild(bEl);
    }
}

