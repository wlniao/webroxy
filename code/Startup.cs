using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace TcpRouter
{
    public class Startup
    {
        private const string unkowndomain = "<html><head><title>Unknown Domain</title></head><body bgcolor=\"white\"><center><h1>Unknown Domain</h1></center><hr><center>ideploy</center></body></html>";
        public Startup(IHostingEnvironment env)
        {
            FirstRequest();
        }
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
        }
        public void Configure(IApplicationBuilder app)
        {
            app.Run(async (context) =>
            {
                var str = new System.IO.StreamReader(context.Request.Body, System.Text.UTF8Encoding.UTF8).ReadToEnd();
                var path = context.Request.Path.Value;
                if (string.IsNullOrEmpty(str))
                {
                    path = path.Trim('/');
                    if (string.IsNullOrEmpty(path))
                    {
                        await context.Response.WriteAsync(unkowndomain);
                    }
                    else if (path.IndexOf('.') < 0 && path.IndexOf('/') < 0)
                    {
                        //短网址处理
                        var query = context.Request.QueryString.Value;
                        try
                        {
                            var jsonStr = Wlniao.XServer.Common.GetResponseString("http://manage.wlniao.com/redirect?key=" + path);
                            var jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<String, String>>(jsonStr);
                            if (jsonObj != null && jsonObj.ContainsKey("url") && !string.IsNullOrEmpty(jsonObj["url"]))
                            {
                                if (jsonObj["url"].IndexOf('?') > 0)
                                {
                                    context.Response.Redirect(jsonObj["url"]);
                                }
                                else
                                {
                                    context.Response.Redirect(jsonObj["url"] + query);
                                }
                                return;
                            }
                        }
                        catch { }
                        await context.Response.WriteAsync("Request path \"/" + path + "\" not found!");
                    }
                }
                else
                {
                    var input = Wlniao.Json.ToObject<InputHook>(str);
                    if (input != null && input.repository != null)
                    {
                        var keyWORKDIR = "GITHOOK_WORKDIR_" + input.RepositoryName().Replace("/", "_").ToUpper();
                        var local = new LocalHook();
                        local.workdir = Wlniao.Config.GetSetting(keyWORKDIR, "").TrimEnd('\\').TrimEnd('/');
                        local.pushtime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        local.pulltime = "";
                        if (string.IsNullOrEmpty(local.workdir))
                        {
                            Console.Write("Githook is not set,you must add env:" + keyWORKDIR);
                        }
                        else if (!System.IO.Directory.Exists(local.workdir))
                        {
                            Console.Write("Sorry,you must git clone first in the" + local.workdir);
                        }
                        else
                        {
                            try
                            {
                                var proc = new System.Diagnostics.Process();
                                proc.StartInfo.CreateNoWindow = false;
                                proc.StartInfo.RedirectStandardError = true;
                                proc.StartInfo.RedirectStandardInput = false;
                                proc.StartInfo.RedirectStandardOutput = true;
                                proc.StartInfo.WorkingDirectory = local.workdir;
                                proc.StartInfo.FileName = "git";
                                proc.StartInfo.Arguments = "pull";
                                //proc.StartInfo.Arguments = ("--git-dir=" + local.workdir + "\\.git --work-tree=" + local.workdir + " pull").Replace("\\", "\\\\");
                                proc.Start();
                                var outStr = proc.StandardOutput.ReadToEnd();
                                if (string.IsNullOrEmpty(outStr))
                                {
                                    Console.WriteLine(proc.StandardError.ReadToEnd());
                                }
                                else if (outStr.StartsWith("Updating "))
                                {
                                    local.pulltime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                    var by = string.IsNullOrEmpty(input.AuthorName()) || string.IsNullOrEmpty(input.AuthorEmail()) ? input.AuthorName() : input.AuthorName() + "<" + input.AuthorEmail() + ">";
                                    if (!string.IsNullOrEmpty(by))
                                    {
                                        by = "/n" + local.pulltime + " by" + by;
                                    }
                                    Console.WriteLine("Repository<" + input.RepositoryName() + "> " + outStr + by);
                                }
                                else
                                {
                                    Console.WriteLine(outStr);
                                }
                            }
                            catch (Exception ex)
                            {
                                Wlniao.log.Error(ex.Message);
                            }
                        }
                    }
                }
            });
        }

        /// <summary>
        /// 进行首次请求
        /// </summary>
        public static async void FirstRequest()
        {
            try
            {
                await Task.Delay(5000);
                using (var client = new System.Net.Http.HttpClient())
                {
                    var reqest = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, "http://127.0.0.1:6338");
                    reqest.Headers.Date = DateTime.UtcNow;
                    client.SendAsync(reqest).ContinueWith((requestTask) =>
                    {
                        var response = requestTask.Result;
                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            response.Content.ReadAsStringAsync().ContinueWith((readTask) =>
                            {
                                var testRlt = readTask.Result;
                            }).Wait();
                        }
                        else
                        {
                            response.Content.ReadAsStringAsync().ContinueWith((readTask) =>
                            {
                                var testRlt = readTask.Result;
                            }).Wait();
                        }
                    }).Wait();
                }
            }
            catch { }
        }
    }
}
