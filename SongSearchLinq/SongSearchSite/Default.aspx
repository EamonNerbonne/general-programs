<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="SongSearchSite._Default"
    EnableViewState="false" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Song Search Linq</title>
    <link rel="Stylesheet" type="text/css" href="songsearch.css" />
    <link rel="icon" type="image/png" href="emnicon.png">
    <script type="text/javascript" src="songsearch.js" defer="defer"></script>
</head>
<body onload="document.getElementById('searchquery').focus(); updateResults()" >
    <form id="form1" runat="server" onsubmit="return false">
    <table id="pagetable">
        <tr>
            <td >
                <div class="searchSect">
                    <h3>SongSearch - a site for searching &amp; playing distributed song db&#39;s.</h3>
                    Search:
                    <input id="searchquery" onkeyup="updateResults()" type="text" name="q" />
                    with at most
                    <input id="shownumber" onkeyup="updateResults()" type="text" value="20" name="top" />
                    results.<br />
                    <a id="m3u" href="list.m3u?extm3u=true&amp;remote=allow&amp;q=" target="_blank">playlist
                        of all matches</a> <a id="m3u8" href="list.m3u8?extm3u=true&amp;remote=allow&amp;q="
                            target="_blank">playlist of all matches (UTF-8, winamp unsupported)!</a>
                </div>
                <div id="audio-container">
                    <i>Click on a song to play it here... (HTML 5 required)</i>
                </div>
            </td>
        </tr>
        <tr>
            <td style="height: 100%; width: 100%; border: none; padding: 0;">
                <iframe frameborder="0" style="height: 100%; width: 100%; border: none;" id="resultsview"
                    src="list.xml?top=20&amp;view=xslt&amp;q=" name="resultsview"></iframe>
            </td>
        </tr>
    </table>
    </form>
</body>
</html>
