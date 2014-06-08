using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using BuskerProxy.Handlers;
using BuskerProxy.Extensions;
using System.Diagnostics;

namespace BuskerProxy.Hubs
{
    public class LoggingHub : Hub
    {
        public void Start()
        {
            Trace.WriteLine("connection", Context.ConnectionId); 
           //when a user clicks on Start in the browser, add this client ip address to the list registered for logging       
           LoggingHandler.Register(Context.Request.GetClientIp(), Context.ConnectionId);
        }
        public void Stop()
        {
            //when a user clicks on Stop in the browser, remove this client ip address from the list registered for logging
            LoggingHandler.Unregister(Context.Request.GetClientIp());
        }
    }
}