using System;
using System.Collections.Generic;
using System.Net;

namespace TcpRouter
{
    public class RedirectCache
    {
        private static Dictionary<String, RedirectCache> cache = new Dictionary<string, RedirectCache>();
        /// <summary>
        /// 跳转地址
        /// </summary>
        private String Url = "";
        /// <summary>
        /// 缓存过期时间
        /// </summary>
        private Int64 UpdateTime = 0;

        public static String Get(String host)
        {
            if (cache.ContainsKey(host) && cache[host].UpdateTime >= Wlniao.DateTools.GetUnix())
            {
                return cache[host].Url;
            }
            return "";
        }
        public static String Put(String host, String url, Int64 expire)
        {
            if (!cache.ContainsKey(host))
            {
                cache.TryAdd(host, new RedirectCache() { Url = url });
            }
            cache[host].UpdateTime = expire;
            return cache[host].Url;
        }
    }
}
