<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Default.aspx.cs" Inherits="_Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Spell List Maker</title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <div style="float: right; width: 30%; padding: 0.5em; background: #eee; border: 1px solid #bbb;
            margin: 0.5em;">
            <h2>Spell List Maker</h2>
            <p>
                <em>Takes a simple spell list and generates a webpages with the full descriptions of
                    those (SRD) spells.</em></p>
            <p>
                This is a printable spell-list generator.&nbsp; It's quite simple, You enter a list
                of spells separated by semicolons, ending in a period. You can name your spell list
                by sticking some name in front of it, ending in a colon (i.e. "1st Level: Detect
                Evil; Bless."). Spacing and newlines are irrelevant. You can have multiple lists
                too, just put more than one in there.</p>
            <h3>
                Complete Spell-lists:</h3>
            <ul>
                <li>
                    <asp:LinkButton ID="WizardLink" runat="server" onclick="NormalCasterLink_Click">Wizard</asp:LinkButton>
				</li>
				<li>
                    <asp:LinkButton ID="DruidLink" runat="server" onclick="NormalCasterLink_Click">Druid</asp:LinkButton>
				</li>
				<li>
                    <asp:LinkButton ID="ClericLink" runat="server" onclick="NormalCasterLink_Click">Cleric</asp:LinkButton>
				</li>
				<li>
                    <asp:LinkButton ID="BardLink" runat="server" onclick="NormalCasterLink_Click">Bard</asp:LinkButton>
				</li>
				<li>
                    <asp:LinkButton ID="PaladinLink" runat="server" onclick="NormalCasterLink_Click">Paladin</asp:LinkButton>
				</li>
				<li>
                    <asp:LinkButton ID="RangerLink" runat="server" onclick="NormalCasterLink_Click">Ranger</asp:LinkButton>
				</li>
				<li>
                    <asp:LinkButton ID="AssassinLink" runat="server" onclick="NormalCasterLink_Click">Assassin</asp:LinkButton>
				</li>
				<li>
                    <asp:LinkButton ID="BlackguardLink" runat="server" onclick="NormalCasterLink_Click">Blackguard</asp:LinkButton>
				</li>
				<li>
                    <asp:LinkButton ID="DomainsLink" runat="server" onclick="DomainsLink_Click">Cleric Domains</asp:LinkButton>
				</li>
            </ul>
            <h3>
                For example:</h3>
            <ul>
                <li>
                    <asp:LinkButton ID="sylvia4" runat="server" OnClick="sylvia4_Click">Sylvia Nerlatel 4th Level Beguiler Spelllist</asp:LinkButton></li>
                <li>
                    <asp:LinkButton ID="sylvia6" runat="server" OnClick="sylvia6_Click">Sylvia Nerlatel 6th Level Beguiler Spelllist</asp:LinkButton></li>
            </ul>
        </div>
        
    	<asp:TextBox ID="LoginBox" runat="server"></asp:TextBox>
		<asp:Button ID="LoginButton" runat="server" Text="Login"></asp:Button>
        
    	<asp:Literal ID="Literal1" runat="server" Text="Not required."></asp:Literal>
        
    &nbsp;(and not yet implemented... the rest still works!)</div>
    <asp:DropDownList ID="DropDownList1" runat="server" AutoPostBack="True" 
	DataSourceID="SqlDataSource1" DataTextField="name" DataValueField="name" 
	Enabled="False">
	</asp:DropDownList>
	<asp:SqlDataSource ID="SqlDataSource1" runat="server" 
	ConnectionString="<%$ ConnectionStrings:StoredSpellListDBconnstr %>" 
	SelectCommand="SELECT DISTINCT [name] FROM [spelllist] WHERE ([username] = @username)">
		<selectparameters>
			<asp:controlparameter ControlID="LoginBox" Name="username" PropertyName="Text" 
			Type="String" />
		</selectparameters>
	</asp:SqlDataSource>
    <p>
        <asp:TextBox ID="SpellBox" runat="server" OnTextChanged="SpellBox_TextChanged"
            Width="65%" TextMode="MultiLine" Text="Wizard 1st: Time Stop" Style="height: 40em"></asp:TextBox><br />
        <asp:Button ID="GenerateButton" runat="server" OnClick="GenerateButton_Click" Text="Generate Now!"
            PostBackUrl="~/Validate.aspx" />
    </p>
    <p>
        <br />
        These are the spell's I know of:
        <asp:Label ID="SpellListLabel" runat="server" Text="Empty"></asp:Label><br />
    </p>
    </form>
</body>
</html>
