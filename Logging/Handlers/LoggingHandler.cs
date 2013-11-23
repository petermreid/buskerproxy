using BuskerProxy.Hubs;
using Microsoft.AspNet.SignalR;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using BuskerProxy.Extensions;

namespace BuskerProxy.Handlers
{
    // ProxyHandler.cs
    public class LoggingHandler : DelegatingHandler
    {
        private static Dictionary<string, string> connections = new Dictionary<string, string>();
        
        public static void Register(string ipaddress, string connection)
        {
            Unregister(ipaddress);
            connections.Add(ipaddress, connection);
        }

        public static void Unregister(string ipaddress)
        {
            connections.Remove(ipaddress);
        }

        protected override async System.Threading.Tasks.Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            //give it a unique Id so we can match up request and response later
            request.Properties["Id"] = System.Guid.NewGuid().ToString();
            await LogRequest(request);
            Stopwatch stopWatch = Stopwatch.StartNew();
            var response = await base.SendAsync(request, cancellationToken);
            stopWatch.Stop();
            await LogResponse(request, response, stopWatch);
            return response;         
        }

        private static async System.Threading.Tasks.Task LogRequest(HttpRequestMessage request)
        {
            string connection;
            if (connections.TryGetValue(request.GetClientIp(), out connection))
            {
                var loggingHubContext = GlobalHost.ConnectionManager.GetHubContext<LoggingHub>();
                await loggingHubContext.Clients.Client(connection).LogUrl(request.RequestUri.ToString());
            }
        }

        private static async System.Threading.Tasks.Task LogResponse(HttpRequestMessage request, HttpResponseMessage response, Stopwatch stopwatch)
        {
            string connection;
            if (connections.TryGetValue(request.GetClientIp(), out connection))
            {
                var loggingHubContext = GlobalHost.ConnectionManager.GetHubContext<LoggingHub>();
                await loggingHubContext.Clients.Client(connection).LogUrl(request.RequestUri.ToString());
            }
        }
    }
}