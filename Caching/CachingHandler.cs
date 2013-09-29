using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;


namespace BuskerProxy.Handlers
{
    // CachingHandler.cs
    public class CachingHandler : DelegatingHandler
    {

        protected override async System.Threading.Tasks.Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            try
            {
                BeforeSendRequest(request);
                HttpResponseMessage response = await base.SendAsync(request, cancellationToken);
                AfterReceiveResponse(response);
                return response;
            }
            catch (Exception ex)
            {
                var response = request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
                return response;
            }
        }

        private void BeforeSendRequest(HttpRequestMessage request)
        {

               
        }

        public void AfterReceiveResponse(HttpResponseMessage response)
        {
        }
    }
}
