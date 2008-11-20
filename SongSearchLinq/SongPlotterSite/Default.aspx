<%@ Page Language="C#" AutoEventWireup="true"  CodeFile="Default.aspx.cs" Inherits="_Default" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    <style type="text/css">
    body,html {
    width:100%;
    height:100%;
    position:relative;
    padding:0;
    margin:0;
        	font-family: "Calibri", "Verdana", "Sans-Serif";

    }
    div#m3uupload    {
    	top:0.2em;
    	left:0.5em;
    	width:30em;
    	z-index:0;
    	position:absolute;
    }
    div#textbox     {
    	top:0.2em;
    	left:31em;
    	width:30em;
    	z-index:0;
    	position:absolute;
    }
    #submitbutton 
    {
    	top:0.2em;
    	left:62em;
    	width:6em;
    	z-index:0;
    	position:absolute;
    }
    div#unknown     {
    	top:50%;
    	left:50%;
    	margin:-15em;
    	width:30em;
    	height:30em;
    	z-index:2;
    	position:absolute;
    	background:red;
    }
    div#textbox:hover,     div#m3uupload:hover    {
    	z-index:2;
    	background:yellow;
    }
    div#map    {
    	z-index:1;
    	position:absolute; top:2.5em; bottom:0; left:0; right:0; background:black;
    	overflow:hidden;
    	padding:5em;
    }
    .textboxFix 
    {
    	width:99%;
    	height:90%;
    	position:absolute;
    	bottom:0;
    }
    .song 
    {
    position:absolute;
    width:20em;
    height:2em;
    margin:-1em -10em;	
    color:White;
    text-align:center;
    }
    .song table
    {
    margin:auto;
    background-image:url("trans.png");	
    border:1px solid white;
    }
    .song td 
    {
    	font-style:italic;
    }
    
    
    </style>
</head>
<body>
    <form id="form1" runat="server">
    <div id="m3uupload">Upload a playlist in .m3u format:
        <asp:FileUpload ID="FileUpload1" runat="server"  /></div>
    <div id="textbox" onmouseover="document.getElementById('TextBox1').focus()">Or enter a number of tracks as `Artist - Title':<br />
        <asp:TextBox ID="TextBox1" TextMode="MultiLine"  AutoPostBack="false"  runat="server" Width="100%" Height="35em"/></div>
    <div id="submitbutton"><asp:Button ID="Button1" runat="server" Text="Plot those songs" /></div>
    <div id="hideOrDelete">onclick: <a href="javascript:hideMode()" id="hideModaA">hide</a> / <a href="javascript:removeMode()" id="removeModeA">remove</a> / </div>
    <div id="unknown" <%=unknownSongs==null? "style='display:none;'":""%>><div style="float:right"><a href="#" onclick="document.getElementById('unknown').style.display='none';return false">close</a></div><center>I've never heard of: </center><br />
        <asp:TextBox TextMode="MultiLine"  AutoPostBack="false" ID="UnknownBox2" runat="server" CssClass="textboxFix"></asp:TextBox></div>
    <div id="map"><div style="position:absolute; top:5em;bottom:5em;left:5em;right:5em;" ><%PrintSongs(); %></div></div>
    </form>
</body>
</html>
