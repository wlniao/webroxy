using System;
using System.Net;

namespace TcpRouter
{
    public class DataApi
    {
        /// <summary>
        /// 
        /// </summary>
        private static Wlniao.Net.Dns.DnsTool dnsTool = new Wlniao.Net.Dns.DnsTool();
        /// <summary>
        /// 内置Web服务（错误输出等）
        /// </summary>
        internal static IPEndPoint ProxyWebEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6338);
        /// <summary>
        /// 分析Web后端地址及回源Host
        /// </summary>
        /// <param name="Host"></param>
        /// <param name="EndPoint"></param>
        /// <param name="TrueHost"></param>
        /// <param name="CacheTime"></param>
        /// <returns></returns>
        internal static IPEndPoint GetEndPointByHost(String Host, out String EndPoint, out String TrueHost, out Int32 CacheTime)
        {
            CacheTime = 10;
            TrueHost = Host;    //默认使用当前地址回源
            EndPoint = Wlniao.Config.GetEnvironment(Host);
            if (string.IsNullOrEmpty(EndPoint))
            {
                CacheTime = 300;
                #region 从DNS服务器读取配置
                var dnsCNAME = dnsTool.GetCNAME(Host);
                if (string.IsNullOrEmpty(dnsCNAME))
                {
                    EndPoint = dnsTool.GetTXT(Host + "." + Proxy.ProxyHost);
                    if (string.IsNullOrEmpty(EndPoint))
                    {
                        EndPoint = dnsTool.GetTXT(Host);
                    }
                }
                else
                {
                    //当域名通过CNAME解析到本机时，使用CNAME值换取TXT值
                    EndPoint = dnsTool.GetTXT(dnsCNAME);
                    if (EndPoint != Host && dnsCNAME.EndsWith("." + Proxy.ProxyHost))
                    {
                        TrueHost = EndPoint;     //使用后端服务器作为回源地址
                    }
                }
                #endregion
            }
            else
            {
                CacheTime = 86400 * 180;
            }
            if (!string.IsNullOrEmpty(EndPoint))
            {
                if (EndPoint.StartsWith("http://") || EndPoint.StartsWith("https://"))
                {
                    RedirectCache.Put(Host, EndPoint, CacheTime + Wlniao.DateTools.GetUnix());
                    EndPoint = "";
                }
                else
                {
                    try
                    {
                        var host = EndPoint;
                        var port = 80;
                        if (EndPoint.IndexOf(':') > 0)
                        {
                            var _host = EndPoint.Split(':');
                            host = _host[0];
                            port = Wlniao.Convert.ToInt(_host[1]);
                        }
                        if (Wlniao.Text.StringUtil.IsIP(host))
                        {
                            return new IPEndPoint(IPAddress.Parse(host), port);
                        }
                        else
                        {
                            return new IPEndPoint(dnsTool.GetIPAddress(host), port);
                        }
                    }
                    catch { }
                }
            }
            return ProxyWebEndPoint;
        }
        /// <summary>
        /// 获取TCP代理端口后端地址
        /// </summary>
        /// <param name="Port"></param>
        /// <returns></returns>
        internal static IPEndPoint GetEndPointByPort(Int32 Port)
        {
            var serverHost = Wlniao.Config.GetConfigsNoCache("host" + Port);
            if (string.IsNullOrEmpty(serverHost))
            {
                Wlniao.Config.SetConfigs("host" + Port, "");
                return null;
            }
            var host = serverHost;
            var port = Port;
            if (serverHost.IndexOf(':') > 0)
            {
                var _host = serverHost.Split(':');
                host = _host[0];
                port = Wlniao.cvt.ToInt(_host[1]);
            }

            if (Wlniao.strUtil.IsIP(host))
            {
                return new IPEndPoint(IPAddress.Parse(host), port);
            }
            else
            {
                return new IPEndPoint(dnsTool.GetIPAddress(host), port);
            }
        }
    }
}
