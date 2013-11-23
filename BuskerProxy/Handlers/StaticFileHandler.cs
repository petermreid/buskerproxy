using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace BuskerProxy.Handlers
{
    //to be replaced when Microsoft.Owin.StaticFiles is available
    public class StaticFileHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string baseFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var uripath = request.RequestUri.AbsolutePath.Substring(1);
            Trace.TraceInformation("Request To:{0}", request.RequestUri.ToString()); 

            return Task<HttpResponseMessage>.Factory.StartNew(() =>
            {
                var filepath = uripath.Replace('/','\\');              
                if (File.Exists(filepath))
                {
                    var response = request.CreateResponse();
                    response.Content = new StreamContent(new FileStream(filepath, FileMode.Open));
                    response.Content.Headers.ContentType = GuessMediaTypeFromExtension(filepath);
                    return response;
                }
                else
                {
                    return request.CreateErrorResponse(HttpStatusCode.NotFound, "File not found");
                }
            });
        }

        private MediaTypeHeaderValue GuessMediaTypeFromExtension(string path)
        {
            var ext = Path.GetExtension(path);

            switch (ext)
            {
                case ".htm":
                case ".html":
                    return new MediaTypeHeaderValue(MediaTypeNames.Text.Html);

                case ".js":
                    return new MediaTypeHeaderValue("text/javascript");

                default:
                    return new MediaTypeHeaderValue(MediaTypeNames.Text.Plain);
            }
        }
    }
}