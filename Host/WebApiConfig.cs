using BuskerProxy.Handlers;
using System.Net.Http;
using System.Web.Http;

namespace BuskerProxy.Host
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
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
