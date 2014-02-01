using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uhuru.Openshift.MsSQLSysGenerator
{
    public static class RegUtil
    {
        //public static void ExportSqlKey(string outputPath, string regPath )
        //{
        //    Output.WriteInfo("Exporting MSSQL Server Registry Key");
        //    Process proc = new Process();
        //    try
        //    {
        //        proc.StartInfo.FileName = "regedit.exe";
        //        proc.StartInfo.UseShellExecute = false;
        //        proc = Process.Start("regedit.exe", "/e \"" + outputPath + "\" \"" + regPath + "\"");
        //        if (proc != null)
        //        {
        //            proc.WaitForExit();
        //        }
        //        Output.WriteSuccess("Done!");
        //    }
        //    catch (Exception ex)
        //    {
        //        Output.WriteError(ex.ToString());
        //        throw ex;
        //    }
        //    finally
        //    {
        //        if (proc != null)
        //        {
        //            proc.Dispose();
        //        }
        //    }

        //}
    }
}
