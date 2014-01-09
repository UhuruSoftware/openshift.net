using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Configuration;
using System.Web.Script.Serialization;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SampleApp
{
    public partial class _Default : Page
    {
        string createTableCommand = @"CREATE TABLE IF NOT EXISTS `{0}` (
  `ID` INT NOT NULL AUTO_INCREMENT,
  `DATE` DATETIME,
  `IP` VARCHAR(50),
  PRIMARY KEY  (`ID`)
);";

        protected void Page_Load(object sender, EventArgs e)
        {
            Label1.Text = Environment.GetEnvironmentVariable("OPENSHIFT_APP_DNS");
            Label2.Text = Environment.GetEnvironmentVariable("OPENSHIFT_DOTNET_PORT");
            Label3.Text = Environment.GetEnvironmentVariable("OPENSHIFT_APP_UUID");

            string server = Environment.GetEnvironmentVariable("OPENSHIFT_MYSQL_DB_HOST");
            string port = Environment.GetEnvironmentVariable("OPENSHIFT_MYSQL_DB_PORT");
            string username = Environment.GetEnvironmentVariable("OPENSHIFT_MYSQL_DB_USERNAME");
            string password = Environment.GetEnvironmentVariable("OPENSHIFT_MYSQL_DB_PASSWORD");
            string database = Environment.GetEnvironmentVariable("OPENSHIFT_APP_NAME");

            string connString = string.Format("Server={0};Port={1};Database={2};Uid={3};Pwd={4};AllowZeroDateTime=true",
                server, port, database, username, password);
            string tableName = "visitors";

            HttpRequest currentRequest = HttpContext.Current.Request;
            string ipAddress = currentRequest.ServerVariables["HTTP_X_FORWARDED_FOR"];

            if (ipAddress == null || ipAddress.ToLower() == "unknown")
                ipAddress = currentRequest.ServerVariables["REMOTE_ADDR"];

            MySqlConnection conn; conn = new MySqlConnection(connString);
            conn.Open();
            try
            {
                MySqlCommand command = conn.CreateCommand();
                command.CommandText = string.Format(createTableCommand, tableName);
                command.ExecuteNonQuery();
                command.CommandText = string.Format("insert into {0} (date, ip) values ('{1}', '{2}')", tableName, DateTime.Now, ipAddress);
                command.ExecuteNonQuery();
                command.CommandText = string.Format("select date as 'Date', ip as 'Visitor IP' from {0} order by id desc LIMIT 10", tableName);
                MySqlDataReader reader = command.ExecuteReader();
                GridView1.DataSource = reader;
                GridView1.DataBind();
            }
            catch (Exception ex)
            {
                Response.Write("oops, something went terribly wrong:" + ex.ToString());
            }
            finally
            {
                conn.Close();
            }
        }
    }
}