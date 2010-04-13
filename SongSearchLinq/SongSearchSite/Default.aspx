<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="SongSearchSite._Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Song Search Linq</title>
    <style type="text/css">
        #Text1
        {
            width: 310px;
        }
        #searchquery
        {
            width: 186px;
        }
        body, html, #form1
        {
            margin: 0;
            padding: 0;
            height: 100%;
        }
        body
        {
            font-family: Segoe UI, Calibri, Helvetica, Arial, Sans-Serif;
            font-size: 12pt;
        }
        table#pagetable
        {
            border-collapse: collapse;
            border: none;
            margin: 0;
            padding: 0;
            width: 100%;
            height: 100%;
        }
        iframe
        {
            border: 0;
        }
        .searchSect
        {
            background: #c4ddd5;
            padding: 0.5em;
        }
    </style>
    <script type="text/javascript">
        var lastquery = "";
        var lasttop = "20";
        function queryUrl(topNr, query) {
            return "list.xml?top=" + encodeURIComponent(topNr) +
                    "&view=xslt&remote=allow&q=" + encodeURIComponent(query);
        }
        // var queryUrlOLD="list.xml?top=1000&view=xslt&remote=allow&q=";
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
            if(extension)
            	switch(extension.toLowerCase()) {
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
                var audioStub = new Audio("");
                if(!(audioStub.canPlayType))
                    return false;
                var canPlay = audioStub.canPlayType(type);
                return canPlay && audioStub.canPlayType(type) != "no";
            } catch (e) {
                return false;
            }
        }

        function BatPop(songlabel, songfilename, songurl, imgObj) {
            var idxDoc = window.idxStatus.document;
            var audioContainer = idxDoc.getElementById("audio-container");

            var type = guessMime(getExtension(songurl));
            var audioEl;
            var isFlash=false;
            if(htmlAudioSupported(type)) {
                audioEl = idxDoc.createElement("audio");
                audioEl.setAttribute("autoplay", "autoplay");
                audioEl.setAttribute("controls", "controls");
                audioEl.setAttribute("src", songurl);
            } else if (type == "audio/mpeg") {
                var audioEl = idxDoc.createElement("object");
                audioEl.setAttribute("type", "application/x-shockwave-flash");
                audioEl.setAttribute("data", "player_mp3_maxi.swf");
                audioEl.setAttribute("width", "200");
                audioEl.id = "flashPlayer";
                isFlash = true;
                audioEl.setAttribute("height", "30");
                var param1El = idxDoc.createElement("param");
                param1El.setAttribute("name", "bgcolor");
                param1El.setAttribute("value", "#ffffff");
                audioEl.appendChild(param1El);
                var param2El = idxDoc.createElement("param");
                param2El.setAttribute("name", "FlashVars");
                param2El.setAttribute("value", "mp3=" + songurl + "&height=30&autoplay=1&autoload=0&showstop=1&showvolume=1&showloading=always&sliderheight=11&volumeheight=7");
                audioEl.appendChild(param2El);
            }

            while (audioContainer.childNodes.length > 0)
                audioContainer.removeChild(audioContainer.childNodes[0]);
            if(audioEl)
                audioContainer.appendChild(audioEl);
            else {
                var bEl = idxDoc.createElement("b");
                bEl.appendChild(idxDoc.createTextNode(UNSUPPORTED));
                audioContainer.appendChild(bEl);
            }
            audioContainer.appendChild(idxDoc.createElement("br"));
            audioContainer.appendChild(idxDoc.createTextNode(songlabel));
            audioContainer.appendChild(idxDoc.createElement("br"));
            audioContainer.appendChild(idxDoc.createTextNode(songfilename));
            if (isFlash) {
                window.idxStatus.setTimeout(function () {
                    window.idxStatus.setTimeout(function () {
                        idxDoc.getElementById("flashPlayer").SetVariable("player:jsPlay", "play");
                    }, 1);
                }, 1);
            }
        }
    </script>
</head>
<body>
    <form id="form1" runat="server" onsubmit="return false">
    <table id="pagetable">
        <tr style="height: 8em">
            <td class="searchSect">
                SongSearch - a site for searching &amp; playing distributed song db&#39;s.<br />
                Search:
                <input id="searchquery" onkeyup="updateResults()" type="text" name="q" />
                with at most
                <input id="shownumber" onkeyup="updateResults()" type="text" value="20" name="top" />
                results.<br />
                <a id="m3u" href="list.m3u?extm3u=true&amp;remote=allow&amp;q=" target="_blank">playlist
                    of all matches</a> <a id="m3u8" href="list.m3u8?extm3u=true&amp;remote=allow&amp;q="
                        target="_blank">playlist of all matches (UTF-8, winamp unsupported)!</a><br />
                Autostart:
                <input type="checkbox" name="autostart" id="autostart" checked="checked" />
            </td>
            <td>
                <iframe src="IndexStatus.aspx" frameborder="0" scrolling="no" style="width: 20em;
                    height: 8em;" id="idxStatus" name="idxStatus"></iframe>
            </td>
        </tr>
        <tr>
            <td style="height: 100%; width: 100%; border: none; padding: 0;" colspan="2">
                <iframe frameborder="0" style="height: 100%; width: 100%; border: none;" id="resultsview"
                    src="list.xml?top=20&amp;view=xslt&amp;q=" name="resultsview"></iframe>
            </td>
        </tr>
    </table>
    </form>
</body>
</html>
