<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="AcloSync._Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>AcloSync-er</title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
    
        <asp:Panel ID="GoogleAuthPanel" runat="server">
            Google Account Details<br />
            Username: <asp:TextBox ID="googleUsername" runat="server"></asp:TextBox><br />
            Password: <asp:TextBox ID="googlePassword" runat="server" TextMode="Password"></asp:TextBox><br />
            <asp:Button ID="SendGoogleAuthButton" runat="server" Text="Log in" 
                onclick="SendGoogleAuthButton_Click1" />
        </asp:Panel>
    
    </div>
    <asp:Panel ID="CalendarSelectionPanel" runat="server">
        Select Your Calendars:<br />
        <asp:CheckBoxList ID="CalenderCheckBoxList" runat="server">
        </asp:CheckBoxList>
        <br />
        <asp:CheckBox ID="ConsiderAllDayEventsCheckBox" runat="server" Text="Consider all-day events" 
            TextAlign="Left" />
    </asp:Panel>
    <asp:Panel ID="AcloPreferencesPanel" runat="server">
        Select your ACLO-preferences:<br />
        <table style="width:100%;">
            <tr>
                <td>
                    <asp:CheckBoxList ID="AcloActivitiesCheckBoxList" runat="server">
                    </asp:CheckBoxList>
                </td>
                <td>
                  <!--  Send me to the gym
                    <asp:TextBox ID="GymFrequencyTextBox" runat="server">3</asp:TextBox>
                    &nbsp;times a week.--></td>
            </tr>
        </table>
    </asp:Panel>
    <asp:Panel ID="OutputCalendarPanel" runat="server">
        Add my gym timetable to a google calender named:
        <asp:TextBox ID="CalendarNameTextBox" runat="server">AcloSync</asp:TextBox>
        <br />
        <asp:CheckBox ID="DeleteFirstCheckBox" runat="server" 
            Text="Empty the calendar first." Checked="True" />
        <br />
        <asp:Button ID="GrilledRoosterButton" runat="server" style="font-size: large" 
            Text="Let the Rooster crow!" onclick="GrilledRoosterButton_Click" />
    </asp:Panel>
    </form>
</body>
</html>
