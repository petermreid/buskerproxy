using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Text;

namespace BuskerProxy.Controllers
{
    public class ConfigAzureAuthController : ApiController
    {
        // GET,POST config/azureauth?name=blah&host=blah&connectionString=blah
        //config/azureauth?name=backtester&host=backtester.table.core.windows.net&connectionString=DefaultEndpointsProtocol=http;AccountName=backtester;AccountKey=
        [HttpGet]
        [HttpPost]
        public HttpResponseMessage Register([FromUri]string name, [FromUri]string host, [FromUri]string connectionString)
        {
            RoutingConfig.Config.RegisterRoute(name,
                new RoutingConfig.RoutingInfo
                {
                    connectionString = connectionString,
                    routeTo = host,
                    type = "AzureAuth"
                });
            string content = "AzureAuth:" + name + " registered";
            return new HttpResponseMessage(HttpStatusCode.OK) 
                { Content = new StringContent(content, Encoding.UTF8, "text/plain") };
        }

        // DELETE config/azureauth?name=blah
        [HttpDelete]
        public HttpResponseMessage Unregister([FromUri]string name)
        {
            RoutingConfig.Config.UnregisterRoute(name);
            string content = "AzureAuth:" + name + "unregistered";
            return new HttpResponseMessage(HttpStatusCode.OK)
                {Content=new StringContent(content, Encoding.UTF8, "text/plain")};
        }
    }
}