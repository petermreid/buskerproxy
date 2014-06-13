using BuskerProxy.Extensions;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace BuskerProxy.Handlers
{
    // ProxyHandler.cs
    public class ProxyHandler : DelegatingHandler
    {
        protected override async System.Threading.Tasks.Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            //have to explicitly null it to avoid protocol violation
            if (request.Method == HttpMethod.Get || request.Method == HttpMethod.Trace) request.Content = null;

            HttpClient client = new HttpClient();
            try
            {
                AddProxyRequestHeader(request);
                Trace.TraceInformation("Request To:{0}", request.RequestUri.ToString()); 
                var response = await client.SendAsync(request,HttpCompletionOption.ResponseHeadersRead);

                //the WebAPI stack unchunks the response, 
                //so its unchunked when we pass it back to the requester
                response.Headers.TransferEncodingChunked = false;
                AddProxyResponseHeader(response);
                //same again clear out due to protocol violation
                if (request.Method == HttpMethod.Head)
                    response.Content = null;
                return response;
            }
            catch (Exception ex)
            {
                var response = request.CreateErrorResponse(HttpStatusCode.InternalServerError,ex);
                string message = ex.Message;
                if (ex.InnerException != null)
                    message += ':' + ex.InnerException.Message;
                response.Content = new StringContent(message);
                Trace.TraceError("Error:{0}", message);
                return response;
            }
        }

        private static void AddProxyResponseHeader(HttpResponseMessage response)
        {
            //do the proxy via header thang
            response.Headers.Via.Add(new ViaHeaderValue("1.1","BuskerProxy","http"));
        }

        private void AddProxyRequestHeader(HttpRequestMessage request)
        {
            //do the proxy forward header thang
            request.Headers.Add("X-Forwarded-For", request.GetClientIp());
        }
    }
}
