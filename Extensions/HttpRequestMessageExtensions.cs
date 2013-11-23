using System;
using System.Net.Http;
using System.ServiceModel.Channels;

namespace BuskerProxy.Extensions
{
    public static class HttpRequestMessageExtensions
    {
        public static string GetClientIp(this HttpRequestMessage request)
        {
            if (request.Properties.ContainsKey("MS_HttpContext"))
            {
                return ((dynamic)request.Properties["MS_HttpContext"]).Request.UserHostAddress as string;
            }
            else if (request.Properties.ContainsKey("MS_OwinContext"))
            {
                return ((dynamic)request.Properties["MS_OwinContext"]).Request.RemoteIpAddress as string;
            }
            else if (request.Properties.ContainsKey(RemoteEndpointMessageProperty.Name))
            {
                RemoteEndpointMessageProperty prop;
                prop = (RemoteEndpointMessageProperty) request.Properties[RemoteEndpointMessageProperty.Name];
                return prop.Address;
            }
            else
            {
                throw new Exception("Could not get client IP");
            }
        }
    }
}