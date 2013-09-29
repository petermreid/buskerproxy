using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Xml.Linq;

namespace BuskerProxy.Handlers
{
    //represents content loaded from a file as Xml
    public class AtomContent : StringContent
    {
        public AtomContent(string content)
            : base(content)
        {
            SetAtomContentType();
        }

        private void SetAtomContentType()
        {
            this.Headers.ContentType = new MediaTypeHeaderValue("application/atom+xml");
        }
    }
}
