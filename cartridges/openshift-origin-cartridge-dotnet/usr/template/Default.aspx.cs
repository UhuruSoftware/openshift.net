using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace smallWeb
{
    public partial class Default : System.Web.UI.Page
    {
        protected Label Label1;
        protected void Page_Load(object sender, EventArgs e)
        {
            Label1.Text = "Hello World!";
        }
    }
}