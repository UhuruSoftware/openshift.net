
namespace Uhuru.Openshift.Cmdlets
{
    public class ReturnStatus
    {
        public int ExitCode { get; set; }
        public object Output { get; set; }

        public ReturnStatus() 
        {
            this.ExitCode = 0;
            this.Output = string.Empty;
        }        

        public ReturnStatus(object output, int exitCode)
        {
            this.ExitCode = exitCode;
            this.Output = output;
        }
    }
}
