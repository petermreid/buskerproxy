using Microsoft.WindowsAzure;
using RoutingConfig;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;

namespace BuskerProxy.Handlers
{
    // AzureAuthHandler.cs
    public class AzureAuthHandler : DelegatingHandler
    {
        const string NextPartitionKeyParam = "NextPartitionKey";
        const string NextRowKeyParam = "NextRowKey";
        const string NextPartitionKeyHeader = "x-ms-continuation-NextPartitionKey";
        const string NextRowKeyHeader = "x-ms-continuation-NextRowKey";
        const string TransferEncodingHeader = "Transfer-Encoding";
        const string AuthorizationHeader = "Authorization";
        const string DateHeader = "x-ms-date";
        const string FeedEndElement = "</feed>";

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

        private static void GenerateAzureAuthHeaders(StorageCredentials storageCredentials, Uri requestUri, out string dateHeader, out string authorizationHeader)
        {
            dateHeader = DateTime.UtcNow.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
            string accountName = storageCredentials.AccountName ;
            var resource = requestUri.PathAndQuery;
            if (resource.Contains("?"))
            {
                resource = resource.Substring(0, resource.IndexOf("?"));
            }

            string stringToSign = string.Format("{0}\n/{1}{2}",
                    dateHeader,
                    accountName,
                    resource
                );

            string signedSignature = storageCredentials.ComputeHmac(stringToSign);
            authorizationHeader = string.Format("{0} {1}:{2}", "SharedKeyLite", accountName, signedSignature);
        }
        
        private void BeforeSendRequest(HttpRequestMessage request)
        {
            string connection = Config.GetConnection(request.RequestUri.Host);
            if (connection != null)
            {
                //and we dont yet hav an AuthorizationHeader
                if (!request.Headers.Contains(AuthorizationHeader))
                {
                    StorageCredentials storageCredentials = CloudStorageAccount.Parse(connection).Credentials;
                    string dateHeader;
                    string authorizationHeader;
                    GenerateAzureAuthHeaders(storageCredentials, request.RequestUri, out dateHeader, out authorizationHeader);
                    request.Headers.Add(DateHeader, dateHeader);
                    request.Headers.Add(AuthorizationHeader, authorizationHeader);
                }
            }
        } 

        public void AfterReceiveResponse(HttpResponseMessage response)
        {
            string connection=null;
            var request = response.RequestMessage;
            if(request!=null)
                connection = Config.GetConnection(request.RequestUri.Host);
            if (connection != null)
            {
                Uri requestUri = response.RequestMessage.RequestUri;
                //now insert the extra <link element>
                var queryStringParams = HttpUtility.ParseQueryString(requestUri.Query);
                //remove the old ones
                queryStringParams.Remove(NextPartitionKeyParam);
                queryStringParams.Remove(NextRowKeyParam);
                //add the new partition and row keys to the queryString
                bool generateLinkElement = false;
                IEnumerable<string> values;
                if (response.Headers.TryGetValues(NextPartitionKeyHeader, out values) == true)
                {
                    var nextPartitionKey = values.FirstOrDefault();
                    queryStringParams.Add(NextPartitionKeyParam, nextPartitionKey);
                    generateLinkElement = true;
                }
                if (response.Headers.TryGetValues(NextRowKeyHeader, out values) == true)
                {
                    var nextRowKey = values.FirstOrDefault();
                    queryStringParams.Add(NextRowKeyParam, nextRowKey);
                    generateLinkElement = true;
                }

                string linkElement = "";
                if (generateLinkElement == true)
                {
                    //create the next href url
                    string newUri = requestUri.GetLeftPart(UriPartial.Path) + "?" + queryStringParams.ToString();
                    //and add to link element
                    linkElement = "<link rel=\"next\" href=\"" + WebUtility.HtmlEncode(newUri) + "\"/>";
                    response.Content = ReplaceInContent(response.Content, FeedEndElement, linkElement + "\n" + FeedEndElement);
                    //optional remove the paging headers as that info is now in the link element
                    response.Headers.Remove(NextPartitionKeyHeader);
                    response.Headers.Remove(NextRowKeyHeader);
                }
            }
        }

        private static HttpContent ReplaceInContent(HttpContent originalContent, string from, string to)
        {
            string originalString = originalContent.ReadAsStringAsync().Result;
            string newString = originalString.Replace(from, to);
            return new AtomContent(newString);
        }
    }   
}
