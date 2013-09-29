using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http;

namespace BuskerProxy.Tests
{
    [TestClass]
    class AzureAuthTest
    {
        [TestMethod]
        public void TestQuote()
        {
            HttpClient client = new HttpClient();
            try
            {
                var response = client.GetAsync("https://backtester.table.core.windows.net/Quote()?$top=200").Result;
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
