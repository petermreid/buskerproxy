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
    public class FlipperController : ApiController
    {
        // GET,POST 
        [HttpGet]
        [HttpPost]
        public HttpResponseMessage Flip()
        {
            string content;
            if(FlipperHandler.Flip(Request.GetClientIp()))
                content = "Flipped";
            else
                content = "Normal";
            return new HttpResponseMessage(HttpStatusCode.OK) 
                { Content = new StringContent(content, Encoding.UTF8, "text/plain") };
        }
    }
}