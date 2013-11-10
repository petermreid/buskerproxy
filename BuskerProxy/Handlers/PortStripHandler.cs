using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Threading.Tasks;
using System.Text;
using System.Web;
using System.Diagnostics;

namespace BuskerProxy.Handlers
{
    // ProxyHandler.cs
    public class PortStripHandler : DelegatingHandler
    {
        protected override async System.Threading.Tasks.Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            UriBuilder forwardUri = new UriBuilder(request.RequestUri);

            forwardUri.Port = 80;
            request.RequestUri = forwardUri.Uri;

            return await base.SendAsync(request, cancellationToken);
        }
    }
}