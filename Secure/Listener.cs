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
using System.Diagnostics;

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
            Trace.WriteLine("Secure server is running");
            Trace.WriteLine("Set your IE proxy to:" + _secureProxyAddress + ':' + _secureProxyPort);

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
            string firstLine;
            string method;
            string version;
            string url;
            string sslTunnelDomain = null;

            Stream clientStream = tcpClient.GetStream();
            Stream proxyStream = tcpProxy.GetStream();

            firstLine = clientStream.ReadLine();
            ParseHttpCommand(firstLine, out method, out url, out version);
            if (method == "CONNECT")
            {
                //client wants to ssl tunnel through the proxy
                clientStream = GetSslTunnelStream(clientStream, version);
                if (clientStream == null)
                    return;
                firstLine = clientStream.ReadLine();
                sslTunnelDomain = url;
            }

            //request
            CopyHttpStream(clientStream, proxyStream, firstLine, sslTunnelDomain);
            //response 
            CopyHttpStream(proxyStream, clientStream);
            firstLine = "";
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

        private static bool CopyHttpStream(Stream fromStream, Stream toStream, string firstLine = "", string sslTunnelDomain="")
        {
            bool keepAlive = false; 
            var encoding = Encoding.UTF8;
            string method;
            string version;
            string url;

            using (var fromStreamReader = new BinaryReader(fromStream, encoding, true))
            using (var toStreamWriter = new BinaryWriter(toStream, encoding, true))
            {
                if (String.IsNullOrEmpty(firstLine))
                    firstLine = fromStream.ReadLine();

                if (!String.IsNullOrEmpty(sslTunnelDomain))
                {
                    //modify the path in the http request to include the domain
                    ParseHttpCommand(firstLine, out method, out url, out version);
                    //modify the forward address so it has complete URL 
                    firstLine = method + ' ' + "https://" + sslTunnelDomain + url + ' ' + version;
                    firstLine += "\r\n";
                    firstLine += "X-Forward-Secure: true";
                }
                LogMessage(string.Format(firstLine));
                toStream.WriteLine(firstLine);
                toStream.Flush();

                string line;
                int contentLength = 0;
                bool chunked = false;

                //copy the headers
                while (!String.IsNullOrEmpty(line = fromStreamReader.ReadLine()))
                {
                    if (line.StartsWith("Content-Length:", true, CultureInfo.CurrentCulture))
                        contentLength = int.Parse(line.Replace("Content-Length:", ""));
                    if (line.StartsWith("Transfer-Encoding: chunked", true, CultureInfo.CurrentCulture))
                        chunked = true;
                    if (line.StartsWith("Proxy-Connection: Keep-Alive", true, CultureInfo.CurrentCulture))
                        keepAlive = true;
                    toStreamWriter.WriteLine(line);
                }
                toStreamWriter.WriteLine();
                if (contentLength > 0)
                    toStreamWriter.Write(fromStreamReader.ReadBytes(contentLength));

                if(chunked)
                {
                    while (!String.IsNullOrEmpty(line = fromStreamReader.ReadLine()))
                    {
                        contentLength = int.Parse(line, System.Globalization.NumberStyles.HexNumber);
                        toStreamWriter.Write(fromStreamReader.ReadBytes(contentLength));
                        fromStreamReader.ReadLine();
                    }
                }
                toStreamWriter.Flush();
            }
            return keepAlive;
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
            Trace.WriteLine(String.Format("[{0}] - Thread-{1}- {2}",
                    callername, Thread.CurrentThread.ManagedThreadId, message));
        }
    }
}

