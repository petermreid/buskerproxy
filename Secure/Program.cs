using System;
using System.Net;

namespace BuskerProxy.Secure
{
    class Program
    {    
        static void Main(string[] args)
        {
            SecureProxyListener secureProxyListener = new SecureProxyListener(IPAddress.Loopback,8081,IPAddress.Loopback,8080);
            secureProxyListener.Start();
            Console.ReadLine();
        }
    }
}


