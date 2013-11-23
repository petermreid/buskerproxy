using System;
using System.Net.Http;
using Microsoft.AspNet.SignalR;

namespace BuskerProxy.Extensions
{
    public static class SignalRRequestExtensions
    {
        public static string GetClientIp(this IRequest request)
        {
            object ipaddress; 

            //to be replaced by OwinConstants
            if(request.Environment.TryGetValue("server.RemoteIpAddress", out ipaddress))
            {
                return (string)ipaddress;
            }
            else
            {
                throw new Exception("Could not get client IP");
            }
        }
    }
}