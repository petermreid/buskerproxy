using BuskerProxy.Handlers;
using Microsoft.Owin;
using Microsoft.Owin.Hosting;
using Owin;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Dispatcher;

[assembly: OwinStartup(typeof(BuskerProxy.Host.Proxy))]

namespace BuskerProxy.Host
{
    public class Proxy
    {
        static List<IDisposable> apps = new List<IDisposable>();
        
        public static void Start(string proxyAddress)
        {
            try
            {
                // Start OWIN proxy host 
                apps.Add(WebApp.Start<Proxy>(proxyAddress));
                Trace.TraceInformation("Listening on:" + proxyAddress);
                Trace.WriteLine("Set your IE proxy to:" + proxyAddress);
            }
            catch (Exception ex)
            {
                string message = ex.Message;
                if(ex.InnerException!=null)
                    message+=":"+ex.InnerException.Message;
                Trace.TraceInformation(message );
            }
        }

        public static void Stop()
        {
            foreach (var app in apps)
            {
                if (app != null)
                    app.Dispose();
            }
        }


        // This code configures Web API. The Startup class is specified as a type
        // parameter in the WebApp.Start method.
        public void Configuration(IAppBuilder appBuilder)
        {
            appBuilder.MapSignalR();
            
            // Configure Web API for self-host. 
            HttpConfiguration httpconfig = new HttpConfiguration();
            RegisterRoutes(httpconfig);
            appBuilder.UseWebApi(httpconfig);
        }

        private void RegisterRoutes(HttpConfiguration config)
        {
            //anything with buskerproxy in the name send to the static file handler
            //http://buskerproxy.cloudapp.net/logging.html
            config.Routes.MapHttpRoute(
                name: "Busker",
                routeTemplate: "{*path}",
                defaults: new { path = RouteParameter.Optional },
                constraints: new { isLocal = new HostConstraint { Host = "buskerproxy" } },
                handler: new StaticFileHandler() 
            );

            //now plug in some AzureAuth config handling using a Controller
            //http://backtester.table.core.windows.net/config?connectionstring=DefaultEndpointsProtocol=https;AccountName=backtester;AccountKey=
            config.Routes.MapHttpRoute(
                    name: "ConfigAzureAuth",
                    routeTemplate: "config",
                    defaults: new { controller = "ConfigAzureAuth" },
                    constraints: new { isLocal = new HostConstraint { Host = "table.core.windows.net" } }
                );

            config.MapHttpAttributeRoutes();

            //now plug in the image handler 
            //http://www.buskerimage.com/flip
            config.Routes.MapHttpRoute(
                    name: "BuskerImageFlip",
                    routeTemplate: "flip",
                    defaults: new { controller = "Image" },
                    constraints: new { isLocal = new HostConstraint { Host = "buskerimage" } }
                );

            //now plug in the image handler 
            //http://www.buskerimage.com/bw
            config.Routes.MapHttpRoute(
                name: "BuskerImageBW",
                routeTemplate: "bw",
                defaults: new { controller = "Image" },
                constraints: new { isLocal = new HostConstraint { Host = "buskerimage" } }
            );

            //anything that needs to fall through needs to go in the pipeline
            config.Routes.MapHttpRoute(
            name: "Proxy",
            routeTemplate: "{*path}",
            handler: HttpClientFactory.CreatePipeline
                (
                    innerHandler: new HttpClientHandler(), // will never get here if proxy is doing its job
                    handlers: new DelegatingHandler[] 
                    { 
                        new PortStripHandler(),
                        new AzureAuthHandler(),
                        new LoggingHandler(),
                        new ImageHandler(),
                        new ProxyHandler() 
                    }
                ),
            defaults: new { path = RouteParameter.Optional },
            constraints: null);
        }
    }
}
