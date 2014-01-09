<%@ Page Title="Home Page" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="SampleApp._Default" %>

<asp:Content runat="server" ID="FeaturedContent" ContentPlaceHolderID="FeaturedContent">
    <section class="featured">
        <div class="content-wrapper">
            <hgroup class="title">
                <h1>Welcome</h1>
            </hgroup>
            <p>
                This is a sample .NET application. It uses a MySql Database to store page visits and displays the latest 10 visits.</p>
        </div>
    </section>
</asp:Content>
<asp:Content runat="server" ID="BodyContent" ContentPlaceHolderID="MainContent">
    <h3>App info:</h3>
    <ul class="round">
        <li>
            <h5>Application host:
                <asp:Label ID="Label1" runat="server"></asp:Label>
            </h5>            
        </li>
        <li>
            <h5>Application port: <asp:Label ID="Label2" runat="server"></asp:Label>
            </h5>            
        </li>
        <li>
            <h5>Application UUID: <asp:Label ID="Label3" runat="server"></asp:Label>
            </h5>            
        </li>
    </ul>
    <h3>Latest visitors:</h3>
    <asp:GridView ID="GridView1" runat="server" CssClass="table"></asp:GridView>
</asp:Content>
