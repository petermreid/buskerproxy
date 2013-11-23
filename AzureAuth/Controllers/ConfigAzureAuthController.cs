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
        // GET,POST config?connectionString=blah
        //config?connectionString=DefaultEndpointsProtocol=http;AccountName=backtester;AccountKey=
        [HttpGet]
        [HttpPost]
        public HttpResponseMessage Register([FromUri]string connectionString)
        {
            string host = Request.RequestUri.Host;

            RoutingConfig.Config.RegisterRoute(host,
                new RoutingConfig.RoutingInfo
                {
                    connectionString = connectionString,
                    routeTo = host,
                    type = "AzureAuth"
                });
            string content = "AzureAuth:" + host + " registered";
            return new HttpResponseMessage(HttpStatusCode.OK) 
                { Content = new StringContent(content, Encoding.UTF8, "text/plain") };
        }

        // DELETE config
        [HttpDelete]
        public HttpResponseMessage Unregister()
        {
            string host = Request.RequestUri.Host;
            RoutingConfig.Config.UnregisterRoute(host);
            string content = "AzureAuth:" + host + "unregistered";
            return new HttpResponseMessage(HttpStatusCode.OK)
                {Content=new StringContent(content, Encoding.UTF8, "text/plain")};
        }
    }
}