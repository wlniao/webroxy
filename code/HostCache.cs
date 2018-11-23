using System;
using System.Collections.Generic;
using System.Net;

namespace TcpRouter
{
    public class HostCache
    {
        private static Dictionary<String, HostCache> cache = new Dictionary<string, HostCache>();
        /// <summary>
        /// 回源Host
        /// </summary>
        private String TrueHost = "";
        /// <summary>
        /// 后端服务地址
        /// </summary>
        private IPEndPoint IPEndPoint = DataApi.ProxyWebEndPoint;
        /// <summary>
        /// 缓存过期时间
        /// </summary>
        private Int64 UpdateTime = 0;

        /// <summary>
        /// 通过请求Host获取服务端（Web代理）
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        public static IPEndPoint FoundByHost(String host)
        {
            var outHost = "";
            return FoundByHost(host, out outHost);
        }
        public static IPEndPoint FoundByHost(String host, out String TrueHost)
        {
            if (!cache.ContainsKey(host))
            {
                cache.Add(host, new HostCache());
            }
            lock (cache[host])
            {
                var now = Wlniao.DateTools.GetUnix();
                if (cache[host].UpdateTime == 0)
                {
                    var CacheTime = 0;
                    var EndPoint = "";
                    var ipEndPoint = DataApi.GetEndPointByHost(host, out EndPoint, out TrueHost, out CacheTime);
                    if (host == Proxy.ProxyHost || string.IsNullOrEmpty(EndPoint))
                    {
                        if (host == Proxy.ProxyHost || host == "127.0.0.1" || host == "localhost")
                        {
                            cache[host].TrueHost = host;
                            cache[host].UpdateTime = now + 86400 * 3652;
                        }
                        else
                        {
                            Console.WriteLine("new empty hostname:" + host + " ,you can set key in environment.");
                            cache[host].UpdateTime = now + CacheTime;
                        }
                    }
                    else
                    {
                        Console.WriteLine("new proxy hostname:" + host + " >>> " + ipEndPoint.ToString() + (TrueHost == host ? "" : " >>> " + TrueHost));
                        cache[host].TrueHost = TrueHost;
                        cache[host].IPEndPoint = ipEndPoint;
                        cache[host].UpdateTime = now + CacheTime;
                    }
                }
                else if (cache[host].UpdateTime < now)
                {
                    #region 异步更新已缓存的域名
                    System.Threading.Tasks.Task.Run(() =>
                    {
                        var CacheTime = 0;
                        var EndPoint = "";
                        var TrueHostTemp = "";
                        var ipEndPoint = DataApi.GetEndPointByHost(host, out EndPoint, out TrueHostTemp, out CacheTime);
                        if (!string.IsNullOrEmpty(EndPoint))
                        {
                            cache[host].UpdateTime = now + CacheTime;
                            if (cache[host].IPEndPoint.ToString() != ipEndPoint.ToString())
                            {
                                Console.WriteLine("update proxy hostname:" + host + " >>> " + ipEndPoint.ToString() + (TrueHostTemp == host ? "" : " >>> " + TrueHostTemp));
                                cache[host].TrueHost = TrueHostTemp;
                                cache[host].IPEndPoint = ipEndPoint;
                            }
                        }
                    });
                    #endregion
                }
                TrueHost = cache[host].TrueHost;
                return cache[host].IPEndPoint;
            }
        }
        /// <summary>
        /// 通过端口号获取服务端（TCP代理）
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public static IPEndPoint FoundByPort(Int32 port)
        {
            var key = "Port-" + port;
            var now = Wlniao.DateTools.GetUnix();
            if (!cache.ContainsKey(key))
            {
                cache.Add(key, new HostCache());
            }
            if (cache[key].UpdateTime < now)
            {
                cache[key].IPEndPoint = DataApi.GetEndPointByPort(port);
                cache[key].UpdateTime = now + 600;
            }
            return cache[key].IPEndPoint;
        }

    }
}
