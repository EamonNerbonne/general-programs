<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Validate.aspx.cs" Inherits="Validate" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>Spell List Maker - Validating</title>
<script language="javascript" type="text/javascript">
<!--

function DIV1_onclick() {

}

// -->
</script>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        OK, so you want to make the following spell list eh (italicized spells are unknown):<br />
        <div id="SpellListHTML" runat="server"></div>
        <br />
        <br />
        <asp:Button ID="NoGood" runat="server" PostBackUrl="~/Default.aspx" Text="Go Back and Try again..." />
        <asp:Button ID="YesGreat" runat="server" Text="Yeah, Let's Try This..." PostBackUrl="~/SpellList.ashx" /><br />
        <br />
        <div style="display:none;">
        <asp:TextBox ID="SpellBox" runat="server" TextMode="MultiLine"></asp:TextBox></div>
    </form>
</body>
</html>
