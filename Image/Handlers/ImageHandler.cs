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
    // ImageHandler.cs
    public class ImageHandler : DelegatingHandler
    {
        private static HashSet<string> flipConnections = new HashSet<string>();

        public static bool Flip(string ipaddress)
        {
            if (flipConnections.Contains(ipaddress))
            {
                flipConnections.Remove(ipaddress);
                return false;
            }
            else
            {
                flipConnections.Add(ipaddress);
                return true;
            }
        }

        private static HashSet<string> bwConnections = new HashSet<string>();

        public static bool BW(string ipaddress)
        {
            if (bwConnections.Contains(ipaddress))
            {
                bwConnections.Remove(ipaddress);
                return false;
            }
            else
            {
                bwConnections.Add(ipaddress);
                return true;
            }
        }


        protected override async System.Threading.Tasks.Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            try
            {
                HttpResponseMessage response = await base.SendAsync(request, cancellationToken);
                if (flipConnections.Contains(request.GetClientIp()) || bwConnections.Contains(request.GetClientIp()))
                {
                    System.Drawing.Imaging.ImageFormat fmt;
                    MediaTypeHeaderValue mediatype = response.Content.Headers.ContentType;
                    if (response.IsSuccessStatusCode && ImageFormat(mediatype, out fmt))
                    {
                        var streamin = await response.Content.ReadAsStreamAsync();

                        using (Bitmap bmp = new Bitmap(Image.FromStream(streamin)))
                        {
                            if(flipConnections.Contains(request.GetClientIp()))
                                bmp.RotateFlip(RotateFlipType.RotateNoneFlipXY);
                            if(bwConnections.Contains(request.GetClientIp()))
                                MakeBW(bmp);
                            var msout = new MemoryStream();
                            bmp.Save(msout, fmt);
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

        private static void MakeBW(Bitmap bmp)
        {
            for (int x = 0; x < bmp.Width; x++)
            {
                for (int y = 0; y < bmp.Height; y++)
                {
                    Color pxl = bmp.GetPixel(x, y);
                    int avg = (pxl.R + pxl.G + pxl.B) / 3;
                    bmp.SetPixel(x, y, Color.FromArgb(avg, avg, avg));
                }
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
