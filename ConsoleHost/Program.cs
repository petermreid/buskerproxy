using BuskerProxy.Host;
using System;
using System.Net;

namespace ConsoleHost
{
    class Program
    {
        static string proxyAddress = @"http://*:8080/"; 

        static void Main(string[] args)
        {
            // Need lots of outgoing connections and hang on to them
            ServicePointManager.DefaultConnectionLimit = 20;
            ServicePointManager.MaxServicePointIdleTime = 10000;
            //send packets as soon as you get them
            ServicePointManager.UseNagleAlgorithm = false;
            //send both header and body together
            ServicePointManager.Expect100Continue = false;

            Proxy.Start(proxyAddress);
            Console.ReadLine();
            Proxy.Stop();
        }

    }
}


