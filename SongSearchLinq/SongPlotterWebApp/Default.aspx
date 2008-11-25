<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="SongPlotterWebApp._Default" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head>
    <title>Song Plotter</title>
    <link rel="Stylesheet" href="songplot.css" type="text/css" />
</head>
<body>
    <form id="form1" runat="server">
    <div id="m3uupload">Upload a playlist in .m3u format:
        <asp:FileUpload ID="FileUpload1" runat="server"  /></div>
    <div id="textbox" onmouseover="document.getElementById('TextBox1').focus()">Or enter a&nbsp; number of tracks as `Artist - Title':<br />
        <asp:TextBox ID="TextBox1" TextMode="MultiLine"  AutoPostBack="false"  runat="server" Width="100%" Height="35em"/></div>
    <div id="submitbutton"><asp:Button ID="PlotButton" runat="server" Text="Plot those songs" /></div>
    <div id="unknown" <%=unknownSongs==null? "style='display:none;'":""%>><div style="float:right"><a href="#" onclick="document.getElementById('unknown').style.display='none';return false">close</a></div><center>I've never heard of: </center><br />
        <asp:TextBox TextMode="MultiLine"  AutoPostBack="false" ID="UnknownBox2" runat="server" CssClass="textboxFix"></asp:TextBox></div>
    <div id="map"><div style="position:absolute; top:5em;bottom:5em;left:5em;right:5em;" ><%PrintSongs(); %></div></div>
    </form>
</body>
</html>
