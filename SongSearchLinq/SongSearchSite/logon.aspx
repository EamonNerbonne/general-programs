<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="logon.aspx.cs" Inherits="SongSearchSite.logon" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <link rel="shortcut icon" type="image/ico" href="img/emnicon.ico" />
    <title>Log on to songsearch</title>
    <style type="text/css">
    html,body,form 
    {
        width: 100%; height: 100%; margin: 0; padding: 0; 
    }
    table 
    {
        border:none;margin:0 auto; position:relative; top:50%; margin-top:-3.5em;
    }
    </style>
</head>
<body style="font-family: Sans-Serif; ">
    <form id="form1" runat="server" >
    <table >
    <tr><td><label for="txtUserName">Username:</label></td><td><asp:textbox id="txtUserName" runat="server" /></td></tr>
    <tr><td><label for="txtPassword">Password:</label></td><td><asp:textbox id="txtPassword" runat="server" TextMode="Password" /></td></tr>
    <tr><td colspan="2"><asp:Label id="lblMessage"  runat="server"/></td></tr>
    <tr><td colspan="2" align="right"><asp:button id="btnSubmit" OnClick="Submit_OnClick"
                      Text="Login" runat="server" /></td></tr>
                      </table>
</form>    
</body>
</html>
