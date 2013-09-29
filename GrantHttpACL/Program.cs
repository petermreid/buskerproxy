using Microsoft.WindowsAzure.ServiceRuntime;
using System.Diagnostics; 

namespace GrantHttpACL
{
    class Program
    {
        static void Main(string[] args)
        {
            TextWriterTraceListener writer = new TextWriterTraceListener("C:\\Logs\\GrantHttpACL.log", "myListener");
            Trace.Listeners.Add(writer);
            Trace.AutoFlush = true;

            if (RoleEnvironment.IsAvailable)
            {
                foreach (var ep in RoleEnvironment.CurrentRoleInstance.InstanceEndpoints)
                {
                    var endpoint = ep.Value;
                    if (endpoint.Protocol == "http")
                    {
                        var ipe = ep.Value.IPEndpoint;
                        string port = ipe.Port.ToString();
                        string cmd = string.Format("http add urlacl url=http://+:{0} user=everyone listen=yes delegate=yes", port);
                        Process.Start("netsh", cmd);
                        Trace.WriteLine(System.DateTime.Now.ToString() + ':' + "netsh " + cmd);
                    }
                }
            }
        }
    }

}
