using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Hosting;
namespace TcpRouter
{
    public class Program
    {

        public static void Main(string[] args)
        {
            //System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            //Console.OutputEncoding = Encoding.Unicode;
            //先设定启动代理服务（延迟启动）
            new System.Threading.Thread(StartProxy).Start();
            //启动Web服务（用于Web代理的错误信息输出）
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseStartup<Startup>()
                .UseUrls("http://*:6338")
                .Build();
            host.Run();            
        }
        /// <summary>
        /// 启动网站服务（用于Web代理的错误信息输出）
        /// </summary>
        public static void StartProxy()
        {
            var portList = Wlniao.Config.GetSetting("PortList");
            if (string.IsNullOrEmpty(portList))
            {
                new Proxy().Listen(Proxy.WebPort);
            }
            else
            {
                var _portList = portList.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var _port in _portList)
                {
                    var port = Wlniao.Convert.ToInt(_port.Trim());
                    if (port > 0)
                    {
                        new Proxy().Listen(port);
                    }
                }
            }
        }
    }
}
