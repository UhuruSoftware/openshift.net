<%@ Page Language="C#" %>  
  
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">  
  
<script runat="server">
    protected void Button1_Click(object sender, System.EventArgs e) {  
        string envVars = string.Empty;
        
        IDictionary	environmentVariables = Environment.GetEnvironmentVariables();
        foreach (DictionaryEntry de in environmentVariables)
        {
            envVars = string.Format("{0}<br />{1} = {2}", envVars, de.Key, de.Value);
        }
        
        Label1.Text = envVars;
    }  
    
    protected void Button2_Click(object sender, System.EventArgs e) {  
        throw new Exception("Exception thrown on the server-side.");
    }  
    
</script>  

<html xmlns="http://www.w3.org/1999/xhtml">
<head>
  <meta charset="utf-8">
  <meta http-equiv="X-UA-Compatible" content="IE=edge,chrome=1">
  <title>Welcome to OpenShift with .Net Support</title>
  <style>
  html {
  background: black;
  }
  body {
    background: #333;
    background: -webkit-linear-gradient(top, black, #666);
    background: -o-linear-gradient(top, black, #666);
    background: -moz-linear-gradient(top, black, #666);
    background: linear-gradient(top, black, #666);
    color: white;
    font-family: "Helvetica Neue",Helvetica,"Liberation Sans",Arial,sans-serif;
    width: 40em;
    margin: 0 auto;
    padding: 3em;
  }
  a {
    color: white;
  }

  h1 {
    text-transform: capitalize;
    -moz-text-shadow: -1px -1px 0 black;
    -webkit-text-shadow: 2px 2px 2px black;
    text-shadow: -1px -1px 0 black;
    box-shadow: 1px 2px 2px rgba(0, 0, 0, 0.5);
    background: #CC0000;
    width: 22.5em;
    margin: 1em -2em;
    padding: .3em 0 .3em 1.5em;
    position: relative;
  }
  h1:before {
    content: '';
    width: 0;
    height: 0;
    border: .5em solid #91010B;
    border-left-color: transparent;
    border-bottom-color: transparent;
    position: absolute;
    bottom: -1em;
    left: 0;
    z-index: -1000;
  }
  h1:after {
    content: '';
    width: 0;
    height: 0;
    border: .5em solid #91010B;
    border-right-color: transparent;
    border-bottom-color: transparent;
    position: absolute;
    bottom: -1em;
    right: 0;
    z-index: -1000;
  }
  h2 {
    margin: 2em 0 .5em;
    border-bottom: 1px solid #999;
  }

  pre {
    background: black;
    padding: 1em 0 0;
    -webkit-border-radius: 1em;
    -moz-border-radius: 1em;
    border-radius: 1em;
    color: #9cf;
  }

  ul {
    margin: 0;
    padding: 0;
  }
  li {
    list-style-type: none;
    padding: .5em 0;
  }

  .brand {
    display: block;
    text-decoration: none;
  }
  .brand .brand-image {
    float: left;
    border:none;
  }
  .brand .brand-text {
    float: left;
    font-size: 24px;
    line-height: 24px;
    padding: 4px 0;
    color: white;
    text-transform: uppercase;
  }
  .brand:hover,
  .brand:active {
    text-decoration: underline;
  }

  .brand:before,
  .brand:after {
    content: ' ';
    display: table;
  }
  .brand:after {
    clear: both;
  }
  </style>
</head>
<body>
    <form id="form1" runat="server">  
      <a href="http://openshift.com" class="brand">
        <img class="brand-image"
          alt="OpenShift logo"
          src="data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAADAAAAAgCAYAAABU1PscAAAAAXNSR0IArs4c6QAAAAZiS0dEAAAAAAAA+UO7fwAAAAlwSFlzAAARHgAAER4B27UUrQAABUhJREFUWMPFWFlsVGUU/s5/70zbaSltA7RQpJ2lC9CFkQkWIgSJxkAhRA0JCYFq4hPG6JsoGKNCtPigxqhvGlPAuGIaE4igNaElbIW2yNL2tkOtTYGWCqWF2e79fCh7p1Bmpnge/3vuOef7z/nPJiTxMHS6pMRuu6YqFNTTAJYSyAU4GZB0AH2AGCANAfc5Qrba6T3HrmECScYLwCioSIcV2AjidQDZ45Q/LJRaWrLV03X89P8GwHB5XwG4DcDkGPWEBKimNrzN094efGQAzjm9GWHFr4R4LiHKgFaSL3r8zYcmHEBbkW+KFo7UEyhKsNeHlMgyV8eJo4kQpqId9ub6HCoc+XWcxl8lcBTATwDax8GfZtHa054/f/bNg8ZcnyOhHjBc834E8MJ9/vML8aYZQX1hd1PP3WFXkhMRfYkIlpOoGomc0WRRTnch+XAQWG2KTNJNLbuy68C/cQMwXOWrAKkdgz8A8kMdg9X5fn/gQcI7POXLaMk3AGbe/P8SbF0D1KcGRGXpIJJpIQkWBHhnsf/Ie3GF0DmnMxmQT8bg7RellXr8ze+Ox3gAcBvNf+iUUhH5FODLSvScAerDGpiVxTAyGUYKzICA34nCwbhDyHB7N4L8PAofhVzh9jfvjffR/ZZTnupIsR8G0C9EjW7Tfnii/dBgrPL0u83kmjHy33Z3Z/zG97uKi7xpWA8GHZpE1mcZRne8MvXblfbxqQAWR+Fp+mdW5hZPjAqu5JVlhrTwOgrXi2ABbjjchF4FYGvi0qhprgagjYod4OeldXWRWBUEtdBjEH4mwIJ7vF2V4Dqgot0+NEFdPAqmdZ5tAXA8Slx6LrpKsxMHQJge5ft1v0oe2OOu+PZ39+LCOFqImqiXo8JzAeBkXlnmnoKK9LgACJl2R9gELsHW1saUwKCpnbIoa8UMTokVgGXJmSjHkfNWUlWDy9d6USVdyoiEF8b1iElxQKHuPG1D/bCtVEBhCiykMQQFgCK2mN2sSx+tkdcbhGq7wKSkK9RnmsCG2xVSLsflAR1S6eloWhawtF8yGJGskSJDBdQR8pIjZMXcfFmm1gOg2lRaSRdT1AD1PBPQbCAyxcRMifCpc41HEtILNbh9s8SSvYTUmBp2LDGOdCOB1OD0XbeByWliwY5bugc9nU2T4wqhCx7PNAV9bSGwARp3TzVaP0j09GQUzJubLUgefY3SEHMh63MVr4FIlYL+7C1AlCwAmxM+/plYy6hhgN2xp1HBawAr72krnH3uoicTaXyHx7uIwKZoT0QhUhszAAI7x7ivL0a60/jp77yyTFrWt6N6rxE99c7OkxdiBhC2y/cAorXHpama/aNG8dkOO32b6p3zTzXmeysfPu4LkkKafA3IrGjfCfPtuGfiPlfx+xBsuWtwpa3zIuy2YaoZ5o0eSQc5TVnb53aeeAuk9eBtRvkqUH0MoTsqA7nL429eFzeA3lyfQ08eaiNgCrjTYNozQ1S+WyUfQCosTLqZ+oiDUNwhggPujpZTuCMXGwUV6cJgKYnNIJffR3df2NLLZ5871puQrUR//pzpU7rOnAfJP53eDELrsoPpk4RIGRn5xqIBAAdBOCAoBjBjPJsJUdZSt9HSOGFrld5cn2M4KbwfkIUJzqYhQlYWdJ7YN2FrFQCY3nPsmk61AuSuRNYyUdaiRBk/7tViR37Zcir1JYC8WNshgjWWPfhq0dmzVx/5bhQAWnLKU1Md8gZHOsjxAgmD2GEKq4s6m1sxASQPu16HiBh53goqPg9ac0TEcwNQEOBlQAZEcMgC94dDZt2c7r8GMIH0H43ZRDC51RVCAAAAAElFTkSuQmCC">
        <img class="brand-image" height="36px"
          alt="Uhuru logo"
          src="data:image/png;base64,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">
        <div class="brand-text">&nbsp;<strong>Open</strong>Shift with .NET</div>
      </a>
      
      <h1>
        Welcome to OpenShift with .Net support
      </h1>
      <p>
        This is a standalone ASP.NET aspx file - use the button below to do a postback and display environment variables.

        <br />

        <asp:Button ID="Button1" runat="server" Text="Display Environment Variables" OnClick="Button1_Click" />  
        <asp:Button ID="Button2" runat="server" Text="Throw an Exception" OnClick="Button2_Click" />  

        <br />
        
        <asp:Label ID="Label1" runat="server" Font-Size="Smaller" ForeColor="Crimson"></asp:Label>  
      </p>
      <p>
        You can commit your changes to Windows applications using git, just like you do it for Linux apps:
      </p>
      <pre>
        git commit -a -m 'Some commit message'
        git push
      </pre>
      <p>
        Then reload this page.
      </p>

      <h2>
        What's next?
      </h2>
      <ul>
        <li>
          Why not visit us at <a href="http://www.openshift.com">http://www.openshift.com</a>, or
        </li>
        <li>
          You could get help in the <a href="http://www.openshift.com/forums/openshift">OpenShift forums</a>, or
        </li>
        <li>
          You're welcome to come chat with us in our IRC channel at #openshift on freenode.net
        </li>
      </ul>
    </form>
</body>
</html>
