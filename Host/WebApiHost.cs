using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.ServiceModel;
using System.Web.Http;
using System.Web.Http.SelfHost;

namespace BuskerProxy.Host
{
    public static class WebApiHost
    {
        private static List<IDisposable> apps = new List<IDisposable>();

        public static HttpServer Listen(string baseAddress)
        {
            // Need lots of outgoing connections and hang on to them
            ServicePointManager.DefaultConnectionLimit = 20;
            ServicePointManager.MaxServicePointIdleTime = 10000;
            //send packets as soon as you get them
            ServicePointManager.UseNagleAlgorithm = false;
            //send both header and body together
            ServicePointManager.Expect100Continue = false;

            try
            {
                var config = new HttpSelfHostConfiguration(baseAddress)
                {
                    HostNameComparisonMode = HostNameComparisonMode.StrongWildcard,
                    IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always,
                    TransferMode = System.ServiceModel.TransferMode.Streamed
                };

                WebApiConfig.Register(config);
                HttpSelfHostServer server = new HttpSelfHostServer(config);
                server.OpenAsync().Wait();

                apps.Add(server);
                Trace.TraceInformation("Listening on:" + config.BaseAddress);
                return server;
            }
            catch (Exception ex)
            {
                Trace.TraceInformation(ex.Message + ':' + ex.InnerException.Message);
                return null;
            }

        }

        public static void UnListen()
        {
            foreach (var app in apps)
            {
                app.Dispose();
            }
        }
    }
}
