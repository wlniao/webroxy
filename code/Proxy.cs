using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
namespace TcpRouter
{
    /// <summary>
    /// 代理客户端
    /// </summary>
    public class Proxy
    {
        private static string _ProxyHost = null;
        public static string ProxyHost
        {
            get
            {
                if (_ProxyHost == null)
                {
                    _ProxyHost = Wlniao.Config.GetSetting("ProxyHost");
                    if (string.IsNullOrEmpty(_ProxyHost))
                    {
                        _ProxyHost = "wln.io";
                    }
                }
                return _ProxyHost;
            }
        }
        public static string ThisIP = Wlniao.OpenApi.Tool.GetIP();
        /// <summary>
        /// Web服务代理端口（默认为环境变量中的ListenPort）
        /// </summary>
        public static int WebPort = Wlniao.XCore.ListenPort;
        /// <summary>
        /// 监听的端口
        /// </summary>
        private Int32 listenPort = 0;
        /// <summary>
        /// 代理端Socket
        /// </summary>
        private Socket proxySocket;

        /// <summary>
        /// 启动监听
        /// </summary>
        /// <param name="Port"></param>
        public void Listen(Int32 Port = 80)
        {
            try
            {
                listenPort = Port;
                proxySocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                proxySocket.Bind(new IPEndPoint(IPAddress.Any, Port));  //监听所有网络接口
                proxySocket.Listen(30);//设定最多30个排队连接请求
                new Thread(NewConnect).Start();
                if (listenPort == WebPort)
                {
                    Console.WriteLine("Listen Port:{0} (Proxy:{1})", Port.ToString(), ProxyHost);
                }
                else
                {
                    Console.WriteLine("Listen Port:{0}", Port.ToString());
                }
            }
            catch
            {
                Console.WriteLine("\r\n\r\nError Message：port\"{0}\" is use by other app", Port.ToString());
                Console.Write("\r\n\r\nPress any key to exit!");
                Console.Read();
            }
        }

        /// <summary>  
        /// 新连接事件处理  
        /// </summary>  
        private void NewConnect()
        {
            while (true)
            {
                Socket clientSocket = proxySocket.Accept(); //挂起并继续等待下一个链接
                new Thread(ReceiveMessage).Start(clientSocket);
            }
        }

        /// <summary>  
        /// 接收消息  
        /// </summary>  
        /// <param name="clientSocket"></param>  
        private void ReceiveMessage(object socket)
        {
            try
            {
                if (listenPort == WebPort)
                {
                    #region Web代理模式 通过Host识别
                    //通过clientSocket接收数据  
                    var request = new byte[1024 * 64];
                    var clientSocket = (Socket)socket;
                    int receiveNumber = clientSocket.Receive(request);
                    if (receiveNumber > 0)
                    {
                        var tempRequest = new List<byte>();
                        var endPoint = DataApi.ProxyWebEndPoint;
                        var tempHost = "";      //当前请求使用的Host
                        var trueHost = "";      //真实服务端回源Host
                        #region HTTP协议处理
                        var requestStr = Encoding.ASCII.GetString(request, 0, receiveNumber);
                        var firstBlankLine = requestStr.IndexOf("\r\n\r\n");    //第一个空行的位置，通知服务器以下不再有请求头。
                        var requestHeader = requestStr.Substring(0, firstBlankLine);
                        var kvList = new List<String>();
                        var kvStr = requestHeader.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        var forwarded = ((IPEndPoint)clientSocket.RemoteEndPoint).Address.ToString();
                        foreach (var _kvStr in kvStr)
                        {
                            var kv = _kvStr.Split(':');
                            var key = kv[0].ToLower();
                            if (key == "host")
                            {
                                //处理请求的主机头
                                tempHost = kv[1].Trim();
                                endPoint = HostCache.FoundByHost(tempHost, out trueHost);
                                kvList.Add("Host: " + trueHost);
                                continue;
                            }
                            else if (key == "origin")
                            {
                                kvList.Add("Origin: " + "http://" + trueHost);
                                continue;
                            }
                            else if (key == "x-forwarded-for")
                            {
                                //处理请求端真实的IP
                                forwarded += "," + kv[1].TrimStart();
                                continue;
                            }
                            else if (key == "referer" && trueHost != tempHost)
                            {
                                var path = _kvStr.Substring(_kvStr.IndexOf("http://") + 7);
                                path = path.Substring(path.IndexOf('/'));
                                kvList.Add("Referer: " + "http://" + trueHost + path);
                                continue;
                            }
                            kvList.Add(_kvStr);
                        }
                        kvList.Add("X-Forwarded-For: " + forwarded);
                        requestHeader = "";
                        foreach (var kv in kvList)
                        {
                            requestHeader += kv + "\r\n";
                        }
                        var contentData = new byte[receiveNumber - firstBlankLine];
                        Buffer.BlockCopy(request, firstBlankLine, contentData, 0, contentData.Length);
                        tempRequest.AddRange(Encoding.ASCII.GetBytes(requestHeader));
                        tempRequest.AddRange(contentData);
                        tempRequest.AddRange(Encoding.ASCII.GetBytes("\r\n\r\n"));
                        request = tempRequest.ToArray();
                        #endregion

                        #region HTTP数据转发
                        var hostSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        hostSocket.Connect(endPoint);
                        if (hostSocket.Send(request, request.Length, SocketFlags.None) > 0)
                        {
                            hostSocket.ReceiveTimeout = 500;
                            while (true)
                            {
                                try
                                {
                                    var response = new byte[1024 * 64];
                                    var length = hostSocket.Receive(response, response.Length, SocketFlags.None);
                                    if (length > 0)
                                    {
                                        clientSocket.Send(response, length, SocketFlags.None);
                                        hostSocket.ReceiveTimeout += 100;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                catch { break; }
                            }
                        }
                        hostSocket.Shutdown(SocketShutdown.Both);
                        #endregion
                    }
                    clientSocket.Shutdown(SocketShutdown.Both);
                    #endregion
                }
                else
                {
                    #region TCP代理
                    var request = new byte[1024 * 64];
                    var response = new byte[1024 * 64];
                    var hostSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    hostSocket.Connect(HostCache.FoundByPort(listenPort));
                    var clientSocket = (Socket)socket;
                    try
                    {
                        clientSocket.ReceiveTimeout = 10;
                        var receiveNumberFirst = clientSocket.Receive(request);
                        if (receiveNumberFirst > 0)
                        {
                            #region 被动模式
                            clientSocket.ReceiveTimeout = 10000;
                            //从客户端接收到数据时，转发给服务端 
                            hostSocket.Send(request, receiveNumberFirst, SocketFlags.None);
                            while (clientSocket.Connected)
                            {
                                try
                                {
                                    //从客户端接收数据 
                                    var receiveNumberServer = hostSocket.Receive(response);
                                    if (receiveNumberServer > 0)
                                    {
                                        //从服务端接收到数据时，转发给客户端  
                                        clientSocket.Send(response, receiveNumberServer, SocketFlags.None);
                                    }
                                    //从客户端接收数据 
                                    var receiveNumberClient = clientSocket.Receive(request);
                                    if (receiveNumberClient > 0)
                                    {
                                        //从客户端接收到数据时，转发给服务端 
                                        hostSocket.Send(request, receiveNumberClient, SocketFlags.None);
                                    }
                                }
                                catch { }
                            }
                            #endregion 
                        }
                    }
                    catch
                    {
                        #region 主动模式
                        clientSocket.ReceiveTimeout = 10000;
                        while (clientSocket.Connected)
                        {
                            //从服务端接收数据 
                            var receiveNumberServer = hostSocket.Receive(response);
                            if (receiveNumberServer > 0)
                            {
                                //从服务端接收到数据时，转发给客户端  
                                clientSocket.Send(response, receiveNumberServer, SocketFlags.None);
                            }
                            //从客户端接收数据 
                            var receiveNumberClient = clientSocket.Receive(request);
                            if (receiveNumberClient > 0)
                            {
                                //从客户端接收到数据时，转发给服务端 
                                hostSocket.Send(request, receiveNumberClient, SocketFlags.None);
                            }
                            if (receiveNumberServer == 0 && receiveNumberClient == 0)
                            {
                                break;
                            }
                        }
                        #endregion
                    }
                    hostSocket.Shutdown(SocketShutdown.Both);
                    hostSocket.Dispose();
                    clientSocket.Shutdown(SocketShutdown.Both);
                    clientSocket.Dispose();
                    #endregion
                }
            }
            catch { }
        }
        
    }
}
