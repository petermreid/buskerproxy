using BuskerProxy.Host;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading;

namespace WorkerRole
{
    public class WorkerRole : RoleEntryPoint
    {
        public override void Run()
        {
            // This is a sample worker implementation. Replace with your logic.
            Trace.TraceInformation("WorkerRole entry point called");

            while (true)
            {
                Thread.Sleep(10000);
                //Trace.TraceInformation("Working");
            }
        }

        public override bool OnStart()
        {
            DiagnosticMonitorConfiguration dmc = DiagnosticMonitor.GetDefaultInitialConfiguration(); 
            dmc.Logs.ScheduledTransferPeriod = TimeSpan.FromMinutes(1); 
            dmc.Logs.ScheduledTransferLogLevelFilter = LogLevel.Verbose;

            DiagnosticMonitor.Start("Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString", dmc); 


            foreach (var rie in RoleEnvironment.CurrentRoleInstance.InstanceEndpoints)
            {
                var endpoint = rie.Value;
                if (endpoint.Protocol == "http")
                {
                    string baseUri = string.Format("{0}://{1}",
                        endpoint.Protocol, endpoint.IPEndpoint);

                    Trace.TraceInformation(String.Format("Starting OWIN at {0}", baseUri));
                    WebApiHost.Listen(baseUri);
                }
            }
            return base.OnStart();
        }

        public override void OnStop()
        {
            WebApiHost.UnListen();
        }
    }
}
