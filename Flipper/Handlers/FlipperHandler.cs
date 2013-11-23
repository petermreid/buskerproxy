using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using BuskerProxy.Extensions;
using System.IO;
using System.Net.Http.Headers;
using System.Drawing;

namespace BuskerProxy.Handlers
{
    // FlipperHandler.cs
    public class FlipperHandler : DelegatingHandler
    {
        private static HashSet<string> connections = new HashSet<string>();

        public static bool Flip(string ipaddress)
        {
            if (connections.Contains(ipaddress))
            {
                connections.Remove(ipaddress);
                return false;
            }
            else
            {
                connections.Add(ipaddress);
                return true;
            }
        }

        protected override async System.Threading.Tasks.Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            try
            {
                HttpResponseMessage response = await base.SendAsync(request, cancellationToken);
                if (connections.Contains(request.GetClientIp()))
                {
                    System.Drawing.Imaging.ImageFormat fmt;
                    MediaTypeHeaderValue mediatype = response.Content.Headers.ContentType;
                    if (response.IsSuccessStatusCode && ImageFormat(mediatype, out fmt))
                    {
                        var streamin = await response.Content.ReadAsStreamAsync();

                        using (Image image = Image.FromStream(streamin))
                        {
                            image.RotateFlip(RotateFlipType.RotateNoneFlipXY);
                            var msout = new MemoryStream();
                            image.Save(msout, fmt);
                            msout.Position=0;
                            response.Content = new StreamContent(msout);
                            response.Content.Headers.ContentType = mediatype;
                        }
                    }
                }

                return response;
            }
            catch (Exception ex)
            {
                var response = request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
                return response;
            }
        }

        private bool ImageFormat(MediaTypeHeaderValue mediatype, out System.Drawing.Imaging.ImageFormat fmt)
        {
            switch (mediatype.MediaType)
            {
                case "image/jpeg":
                case "image/jpg":
                    fmt= System.Drawing.Imaging.ImageFormat.Jpeg;
                    return true;
                case "image/png":
                    fmt = System.Drawing.Imaging.ImageFormat.Png;
                    return true;
                case "image/gif":
                    fmt = System.Drawing.Imaging.ImageFormat.Gif;
                    return true;
                case "image/tiff":
                    fmt = System.Drawing.Imaging.ImageFormat.Tiff;
                    return true;
                default:
                    fmt = null;
                    return false;
            }
        }

    }   
}
