using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Web.Http;
using Microsoft.Owin.Hosting;

namespace BuskerProxy.Host
{
    public static class WebApiHost
    {
        private static List<IDisposable> apps = new List<IDisposable>();

        public static void Listen(string baseAddress)
        {
            //// Need lots of outgoing connections and hang on to them
            //ServicePointManager.DefaultConnectionLimit = 20;
            //ServicePointManager.MaxServicePointIdleTime = 10000;
            ////send packets as soon as you get them
            //ServicePointManager.UseNagleAlgorithm = false;
            ////send both header and body together
            //ServicePointManager.Expect100Continue = false;

            try
            {
                //var config = new HttpSelfHostConfiguration(baseAddress)
                //{
                //    HostNameComparisonMode = HostNameComparisonMode.StrongWildcard,
                //    IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always,
                //    TransferMode = System.ServiceModel.TransferMode.Streamed
                //};

                // Start OWIN host 
                StartOptions options = new StartOptions(baseAddress);
                IDisposable app = WebApp.Start<Startup>(options);
                Trace.TraceInformation("Listening on:" + baseAddress); 
                apps.Add(app);
            }
            catch (Exception ex)
            {
                Trace.TraceInformation(ex.Message + ':' + ex.InnerException.Message);
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
