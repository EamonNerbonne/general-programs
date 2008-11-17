<%@ Page Language="C#" AutoEventWireup="true"  CodeFile="Default.aspx.cs" Inherits="_Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div style="float:right; width:15em">Or enter a number of tracks as `Artist - Title':<br />
        <asp:TextBox ID="TextBox1" runat="server" Width="100%" Height="15em"></asp:TextBox></div>
    <div>Upload a playlist in .m3u format:
        <asp:FileUpload ID="FileUpload1" runat="server" />
    </div>
    <asp:PlaceHolder ID="PlaceHolder1" runat="server"></asp:PlaceHolder>
    </form>
</body>
</html>
