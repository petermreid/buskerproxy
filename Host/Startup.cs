using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;
using System.Web.Http;
using BuskerProxy.Handlers;
using System.Net.Http;
using Microsoft.AspNet.SignalR;

[assembly: OwinStartup(typeof(BuskerProxy.Host.Startup))]

namespace BuskerProxy.Host
{
    public class Startup
    {
        // This code configures Web API. The Startup class is specified as a type
        // parameter in the WebApp.Start method.
        public void Configuration(IAppBuilder appBuilder)
        {
            // Configure Web API for self-host. 
            HttpConfiguration httpconfig = new HttpConfiguration();
            RegisterRoutes(httpconfig);
            appBuilder.UseWebApi(httpconfig);

            HubConfiguration hubconfig = new HubConfiguration();
            appBuilder.MapSignalR();
        }

        private void RegisterRoutes(HttpConfiguration config)
        {
            config.Routes.MapHttpRoute(
                name: "BuskerProxy",
                routeTemplate: "{*path}",
                defaults: new { path = RouteParameter.Optional },
                constraints: new{isLocal=new HostConstraint{ Host ="buskerproxy"}},
                handler: new StaticFileHandler()
            );

            config.Routes.MapHttpRoute(
                    name: "ConfigAzureAuth",
                    routeTemplate: "config/azureauth",
                    defaults: new { controller = "ConfigAzureAuth" }
                );

            config.Routes.MapHttpRoute(
                    name: "Proxy",
                    routeTemplate: "{*path}",
                    handler: HttpClientFactory.CreatePipeline
                        (
                            innerHandler: new HttpClientHandler(), // will never get here if proxy is doing its job
                            handlers: new DelegatingHandler[] 
                            { 
                                new PortStripHandler(),
                                new LoggingHandler(),
                                new AzureAuthHandler(),
                                new ProxyHandler() 
                            }
                        ),
                    defaults: new { path = RouteParameter.Optional },
                    constraints: null
                );
        }
    }
}
