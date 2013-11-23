using System.Web;
using System.Web.Http.Routing;
using System.Collections.Generic;
using System.Net.Http;

namespace BuskerProxy.Handlers
{
    /// <summary>
    /// Restricts WebAPI route to only affect certain hosts
    /// </summary>
    public class HostConstraint : IHttpRouteConstraint
    {
        public string Host{get;set;}

        public bool Match(HttpRequestMessage request, IHttpRoute route, string parameterName, IDictionary<string, object> values, HttpRouteDirection routeDirection) 
        {
            return request.RequestUri.Host.Contains(Host);
        }
    }
}
