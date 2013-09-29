using BuskerProxy.Host;
using System;
using System.Diagnostics;


namespace ConsoleHost
{
    class Program
    {
        static void Main(string[] args)
        {
            WebApiHost.Listen(@"http://127.0.0.1:8080/");
            Trace.WriteLine("Set your IE proxy to Address:127.0.0.1 Port:8080");
            Console.ReadLine();
            WebApiHost.UnListen();
        }

    }
}


