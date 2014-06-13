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
                string key = GenerateCacheKey(request);
                HttpResponseMessage response=null;
                if (CanUseCache(request))
                {
                    response = GetFromCache(key);
                    if (response != null)
                    {
                       return response;
                    }
                }
                response = await base.SendAsync(request, cancellationToken);
                if (IsCacheable(response))
                {
                    PutInCache(key, response);
                }
                return response;
            }
            catch (Exception ex)
            {
                var response = request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
                return response;
            }
        }

        private HttpResponseMessage GetFromCache(string key)
        {
            return null;
        }
        
        private void PutInCache(string key, HttpResponseMessage response)
        {
        }

        private string GenerateCacheKey(HttpRequestMessage request)
        {
            //IEnumerable<string> varyHeaders;
            //if (!VaryHeaderStore.TryGetValue(uri, out varyHeaders))
            //{
            //    varyHeaders = DefaultVaryHeaders;
            //}
            //var cacheKey = new CacheKey(uri,
            //    request.Headers.Where(x => varyHeaders.Any(y => y.Equals(x.Key,
            //        StringComparison.CurrentCultureIgnoreCase)))
            //        .SelectMany(z => z.Value)
            //    );
            return "";
        }

        private bool IsCacheable(HttpResponseMessage response)
        {
            return false;
        }

        private bool CanUseCache(HttpRequestMessage request)
        {
            if (request.Method==HttpMethod.Get || request.Method==HttpMethod.Put)
            {
                // client can tell CachingHandler not to do caching for a particular request
                if (request.Headers.CacheControl != null)
                {
                    if (request.Headers.CacheControl.NoStore)
                        return false;
                }
                return true;
            }
            return false;
        }



        /// <summary>
        /// Returns whether resource is fresh or if stale, it is acceptable to be stale
        /// null --> dont know, cannot be determined
        /// true --> yes, is OK if stale
        /// false --> no, it is not OK to be stale 
        /// </summary>
        /// <param name="cachedResponse"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        private bool? IsFreshOrStaleAcceptable(HttpResponseMessage cachedResponse, HttpRequestMessage request)
        {

            TimeSpan staleness = TimeSpan.Zero; // negative = fresh, positive = stale

            if (cachedResponse == null)
                throw new ArgumentNullException("cachedResponse");

            if (request == null)
                throw new ArgumentNullException("request");

            if (cachedResponse.Content == null)
                return null;

            DateTimeOffset? responseDate = cachedResponse.Headers.Date ?? cachedResponse.Content.Headers.LastModified; // Date should have a value
            if (responseDate == null)
                return null;

            if (cachedResponse.Headers.CacheControl == null)
                return null;

            // calculating staleness
            // according to http://www.w3.org/Protocols/rfc2616/rfc2616-sec14.html#sec14.9.3 max-age overrides expires header
            if (cachedResponse.Content.Headers.Expires != null)
            {
                staleness = DateTimeOffset.Now.Subtract(cachedResponse.Content.Headers.Expires.Value);
            }

            if (cachedResponse.Headers.CacheControl.MaxAge.HasValue) // Note: this is MaxAge for response
            {
                staleness = DateTimeOffset.Now.Subtract(responseDate.Value.Add(cachedResponse.Headers.CacheControl.MaxAge.Value));
            }

            if (request.Headers.CacheControl == null)
                return staleness < TimeSpan.Zero;

            if (request.Headers.CacheControl.MinFresh.HasValue)
                return -staleness > request.Headers.CacheControl.MinFresh.Value; // staleness is negative if still fresh

            if (request.Headers.CacheControl.MaxStale) // stale acceptable
                return true;

            if (request.Headers.CacheControl.MaxStaleLimit.HasValue)
                return staleness < request.Headers.CacheControl.MaxStaleLimit.Value;

            if (request.Headers.CacheControl.MaxAge.HasValue)
                return responseDate.Value.Add(request.Headers.CacheControl.MaxAge.Value) > DateTimeOffset.Now;

            return false;
        }
    }
}
