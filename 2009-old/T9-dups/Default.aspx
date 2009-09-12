<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Default.aspx.cs" Inherits="_Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
	<title></title>
</head>
<body>
	<form id="form1" runat="server">
	<div>
		Word to search for...<asp:TextBox runat="server" ID="SourceWord"></asp:TextBox> <button>(Using Enter is faster)</button>
		<p><asp:Label ID="rawt9code" runat="server" /></p>
		<table>
			<tr>
				<th>
					Nederlands
				</th>
				<th>
					English-words
				</th>
				<th>
					English-POS
				</th>
			</tr>
			<tr>
				<td>
					<asp:ListView runat="server" ID="ned1" ItemPlaceholderID="itemPlaceholderNed1">
						<LayoutTemplate>
							<ul runat="server">
								<li runat="server" id="itemPlaceholderNed1"></li>
							</ul>
						</LayoutTemplate>
						<ItemTemplate>
							<li>
								<asp:Label ID="NameLabel" runat="server" Text='<%#Container.DataItem %>' />
							</li>
						</ItemTemplate>
					</asp:ListView>
				</td>
				<td>
					<asp:ListView runat="server" ID="eng1" ItemPlaceholderID="itemPlaceholderNed1">
						<LayoutTemplate>
							<ul id="Ul1" runat="server">
								<li runat="server" id="itemPlaceholderNed1"></li>
							</ul>
						</LayoutTemplate>
						<ItemTemplate>
							<li>
								<asp:Label ID="NameLabel" runat="server" Text='<%#Container.DataItem %>' />
							</li>
						</ItemTemplate>
					</asp:ListView>
				</td>
				<td>
					<asp:ListView runat="server" ID="eng2" ItemPlaceholderID="itemPlaceholderNed1">
						<LayoutTemplate>
							<ul id="Ul2" runat="server">
								<li runat="server" id="itemPlaceholderNed1"></li>
							</ul>
						</LayoutTemplate>
						<ItemTemplate>
							<li>
								<asp:Label ID="NameLabel" runat="server" Text='<%#Container.DataItem %>' />
							</li>
						</ItemTemplate>
					</asp:ListView>
				</td>
			</tr>
		</table>
	</div>
	</form>
</body>
</html>
