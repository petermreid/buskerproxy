using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace BuskerProxy.Secure
{

    public class SecureProxyListener
    {
        private static IPAddress _secureProxyAddress = IPAddress.Loopback;
        private int _secureProxyPort;
        private static IPAddress _insecureProxyAddress = IPAddress.Loopback;
        private static int _insecureProxyPort;
        private static X509Certificate2 _certificate;

        public SecureProxyListener(IPAddress secureProxyAddress, int secureProxyPort, IPAddress insecureProxyAddress, int insecureProxyPort)
        {
            _secureProxyAddress = secureProxyAddress;
            _secureProxyPort = secureProxyPort;
            _insecureProxyAddress = insecureProxyAddress;
            _insecureProxyPort = insecureProxyPort;
            _certificate = new X509Certificate2("buskerproxy.pfx", "");
        }

        ///<summary>
        /// Start listening for connection
        /// </summary>
        public async void Start()
        {
            TcpListener listener = new TcpListener(_secureProxyAddress, _secureProxyPort);

            listener.Start();
            LogMessage("Server is running");
            LogMessage("Listening on port " + _secureProxyPort);

            try
            {
                while (true)
                {
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    while (!ThreadPool.QueueUserWorkItem(new WaitCallback(ProcessClient), client)) ;
                }
            }
            catch (ThreadAbortException) { }
            catch (SocketException) { }
        }

        private static void ProcessClient(Object obj)
        {
            TcpClient tcpClient = (TcpClient)obj;
            TcpClient tcpProxy = null;
            try
            {
                string clientInfo = tcpClient.Client.RemoteEndPoint.ToString();
                LogMessage(string.Format("Got connection request from {0}", clientInfo));
                //make a forward connection to proxy
                tcpProxy = new TcpClient();
                tcpProxy.Connect(_insecureProxyAddress, _insecureProxyPort);
                ForwardClientToProxy(tcpClient, tcpProxy);
            }
            catch (SocketException exp)
            {
                LogMessage("Unable to forward requests to proxy, Check proxy is running");
            }
            catch (Exception exp)
            {
                LogMessage(exp.ToString());
            }
            finally
            {
                if (tcpClient != null)
                    tcpClient.Close();
                if (tcpProxy != null)
                    tcpProxy.Close();
            }
        }

        private static void ForwardClientToProxy(TcpClient tcpClient, TcpClient tcpProxy)
        {
            string httpCmd;
            string method;
            string version;
            string url;
            string sslTunnelDomain = null;

            Stream clientStream = tcpClient.GetStream();
            Stream proxyStream = tcpProxy.GetStream();

            httpCmd = clientStream.ReadLine();
            ParseHttpCommand(httpCmd, out method, out url, out version);
            if (method == "CONNECT")
            {
                //client wants to ssl tunnel through the proxy
                clientStream = GetSslTunnelStream(clientStream, version);
                if (clientStream == null)
                    return;
                httpCmd = clientStream.ReadLine();
                sslTunnelDomain = url;
            }
            if (!String.IsNullOrEmpty(sslTunnelDomain))
            {
                //modify the path in the http request to include the domain
                ParseHttpCommand(httpCmd, out method, out url, out version);
                //modify the forward address so it has complete URL 
                httpCmd = method + ' ' + "https://" + sslTunnelDomain + url + ' ' + version;
            }

            LogMessage(string.Format(httpCmd));
            proxyStream.WriteLine(httpCmd);
            proxyStream.Flush();
            CopyHttpStream(clientStream, proxyStream);
            CopyHttpStream(proxyStream, clientStream);

            clientStream.Close();
            proxyStream.Close();
        }

        private static Stream GetSslTunnelStream(Stream stream, string version = "HTTP/1.1")
        {
            SslStream sslStream = null;
            //Browser wants to create a secure tunnel
            //read and ignore headers
            while (!String.IsNullOrEmpty(stream.ReadLine())) ;
            //tell the client that a tunnel has been established              
            LogMessage(string.Format("Doing CONNECT"));
            var connectStreamWriter = new BinaryWriter(stream);
            connectStreamWriter.WriteLine(version + " 200 Connection established");
            connectStreamWriter.WriteLine(String.Format("Timestamp: {0}", DateTime.Now.ToString()));
            connectStreamWriter.WriteLine("Proxy-agent: buskerproxy");
            connectStreamWriter.WriteLine();
            connectStreamWriter.Flush();

            //open a decrypting stream    
            sslStream = new SslStream(stream, false);
            try
            {
                sslStream.AuthenticateAsServer(_certificate, false, SslProtocols.Tls | SslProtocols.Ssl3 | SslProtocols.Ssl2, true);
            }
            catch (Exception ex)
            {
                stream.Close();
                sslStream.Close();
                return null;
            }
            return sslStream;
        }

        private static void CopyHttpStream(Stream fromStream, Stream toStream, string httpCmd = "", Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;
            using (var fromStreamReader = new BinaryReader(fromStream, encoding, true))
            using (var toStreamWriter = new BinaryWriter(toStream, encoding, true))
            {

                string line;
                int contentLength = 0;

                //copy the headers
                while (!String.IsNullOrEmpty(line = fromStreamReader.ReadLine()))
                {
                    if (line.StartsWith("Content-Length:", true, CultureInfo.CurrentCulture))
                        contentLength = int.Parse(line.Replace("Content-Length:", ""));
                    toStreamWriter.WriteLine(line);
                }
                toStreamWriter.WriteLine();
                if (contentLength > 0)
                    toStreamWriter.Write(fromStreamReader.ReadBytes(contentLength));
                toStreamWriter.Flush();
            }
        }

        private static void ParseHttpCommand(string httpCmd, out string method, out string requestPath, out string version)
        {
            char[] splitChar = { ' ' };
            method = "";
            requestPath = null;
            version = "";
            //read the first line
            //break up the line into three components
            String[] splitBuffer = httpCmd.Split(splitChar, 3);
            if (splitBuffer.Length == 3)
            {
                method = splitBuffer[0];
                requestPath = splitBuffer[1];
                version = splitBuffer[2];
            }
        }

        private static void LogMessage(string message, [CallerMemberName]string callername = "")
        {
            System.Console.WriteLine("[{0}] - Thread-{1}- {2}",
                    callername, Thread.CurrentThread.ManagedThreadId, message);
        }
    }
}

