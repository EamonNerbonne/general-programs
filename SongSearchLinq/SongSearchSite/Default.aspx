<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="SongSearchSite._Default"
    EnableViewState="false" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Song Search Linq</title>
    <link rel="shortcut icon" type="image/png" href="emnicon.png" />
    <link rel="Stylesheet" type="text/css" href="songsearch.css" />
</head>
<body>
    <div class="searchAndPlayerRow">
        <div class="searchSect">
            <h3>
                SongSearch - a site for searching &amp; playing distributed song db&#39;s.</h3>
            <form method="get" action="list.xml" id="searchForm" target="resultsview">
            <input type="hidden" name="view" value="xslt" />
            Search:
            <input id="searchquery" type="text" name="q" autocomplete="off"/>
            with at most
            <input id="shownumber" type="text" value="50" name="top" />
            results.<br />
            </form>
            matches: <a class="matchLink" href="playlist.m3u?q=" target="_blank">m3u</a> <a class="matchLink"
                href="playlist.m3u8?q=" target="_blank">m3u8</a>
        </div>
        <div style="position: absolute; left: 36em; right: 0; top: 0; bottom: 0; ">
            <div id="audio-container" >
                <i>Click on a song to play it here... (HTML 5 required)</i>
            </div>
        </div>
    </div>
    <div style="position: absolute; top:6em;left:0;right:0;bottom:0;  border: none; padding: 0;">
        <iframe frameborder="0" style="height: 100%; width: 100%; border: none;" id="resultsview"
            src="Loading.html" name="resultsview"></iframe>
    </div>
</body>
<script type="text/javascript" src="jquery-1.4.2.min.js"></script>
<script type="text/javascript" src="jquery.jplayer-1.1.0.min.js"></script>
<script type="text/javascript" src="songsearch.js?" defer="defer"></script>
</html>
