using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Text;
using BuskerProxy.Handlers;
using BuskerProxy.Extensions;

namespace BuskerProxy.Controllers
{
    public class ImageController : ApiController
    {
        [HttpGet]
        [Route("flip")]
        public HttpResponseMessage Flip()
        {
            string content;
            if(ImageHandler.Flip(Request.GetClientIp()))
                content = "Flipped On";
            else
                content = "Flipped Off";
            return new HttpResponseMessage(HttpStatusCode.OK) 
                { Content = new StringContent(content, Encoding.UTF8, "text/plain") };
        }

        [HttpGet]
        [Route("bw")]
        public HttpResponseMessage BW()
        {
            string content;
            if (ImageHandler.BW(Request.GetClientIp()))
                content = "Black & White On";
            else
                content = "Black & White Off";
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(content, Encoding.UTF8, "text/plain") };
        }
    }
}