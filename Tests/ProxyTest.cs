using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http;

namespace BuskerProxy.Tests
{
    [TestClass]
    public class ProxyTest
    {
        [TestMethod]
        public void TestGet()
        {
            HttpClient client = new HttpClient();
            try
            {
                var response = client.GetAsync("http://www.google.com.au/").Result;
                response.EnsureSuccessStatusCode();
                Assert.IsNotNull(response.Content);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);               
            }
        }
    }
}
