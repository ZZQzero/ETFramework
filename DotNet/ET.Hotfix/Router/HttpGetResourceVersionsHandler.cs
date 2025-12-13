using System;
using System.Net;
using System.Net.Http;
using System.Threading;

namespace ET
{
    /// <summary>
    /// HTTP获取资源服配置接口
    /// 从资源服读取 config.json 并直接返回JSON字符串给客户端
    /// </summary>
    [HttpHandler(SceneType.RouterManager, "/get_resource_versions")]
    public class HttpGetResourceVersionsHandler : IHttpHandler
    {
        private static readonly HttpClient HttpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        // 资源服配置缓存（JSON字符串，只缓存一次，后台主动刷新）
        private static string cachedJson;
        private static readonly object cacheLock = new object();

        public async ETTask Handle(Scene scene, HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            // CORS支持
            response.AppendHeader("Access-Control-Allow-Origin", "*");

            // Preflight
            if (request.HttpMethod == "OPTIONS")
            {
                response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept, X-Requested-With");
                response.AddHeader("Access-Control-Allow-Methods", "GET, OPTIONS");
                response.AddHeader("Access-Control-Max-Age", "1728000");
                response.StatusCode = (int)HttpStatusCode.OK;
                response.ContentLength64 = 0;
                return;
            }

            try
            {
                // 只允许GET方法
                if (request.HttpMethod != "GET")
                {
                    await HttpHelper.ResponseText(response, HttpStatusCode.MethodNotAllowed, "Only GET method is allowed");
                    return;
                }

                // 从配置中获取资源服地址
                string resourcePath = "http://127.0.0.1:8080";
                
                // 确保资源服地址以/结尾
                if (!resourcePath.EndsWith("/"))
                {
                    resourcePath += "/";
                }

                // 获取资源服配置（带缓存）
                string json = await GetResourceConfigWithCache(resourcePath);

                // 直接返回JSON字符串
                await HttpHelper.ResponseText(response, HttpStatusCode.OK, json, "application/json; charset=utf-8");
            }
            catch (Exception e)
            {
                Log.Error($"获取资源服配置失败: {e}\n{e.StackTrace}");
                try
                {
                    await HttpHelper.ResponseText(response, HttpStatusCode.InternalServerError, $"Failed to get resource config: {e.Message}");
                }
                catch (Exception inner)
                {
                    Log.Error(inner);
                }
            }
        }

        /// <summary>
        /// 获取资源服配置（带缓存机制）
        /// 如果已有缓存，直接返回缓存；否则访问资源服获取配置
        /// 缓存只加载一次，后续由后台管理界面主动刷新
        /// </summary>
        public static async ETTask<string> GetResourceConfigWithCache(string resourcePath)
        {
            // 第一次检查：快速路径（无锁读取）
            if (cachedJson != null)
            {
                return cachedJson;
            }

            // 第二次检查：加锁检查，避免重复加载
            lock (cacheLock)
            {
                // 双重检查锁定
                if (cachedJson != null)
                {
                    return cachedJson;
                }
            }

            // 访问资源服获取配置（await 在 lock 外）
            string json = await GetResourceConfig(resourcePath);

            // 更新缓存
            lock (cacheLock)
            {
                if (cachedJson == null)
                {
                    cachedJson = json;
                    Log.Info("首次加载资源服配置缓存成功");
                }
            }

            return json;
        }

        /// <summary>
        /// 刷新缓存（访问资源服获取最新配置）
        /// 供后台管理界面调用，主动刷新缓存
        /// </summary>
        public static async ETTask RefreshResourceConfigCache(string resourcePath)
        {
            // 确保资源服地址以/结尾
            if (!resourcePath.EndsWith("/"))
            {
                resourcePath += "/";
            }

            // 访问资源服获取最新配置
            string json = await GetResourceConfig(resourcePath);

            // 更新缓存
            lock (cacheLock)
            {
                cachedJson = json;
                Log.Info("后台刷新资源服配置缓存成功");
            }
        }

        /// <summary>
        /// 从资源服获取配置（读取 config.json）
        /// 直接返回JSON字符串
        /// </summary>
        private static async ETTask<string> GetResourceConfig(string resourcePath)
        {
            try
            {
                // 构建 config.json 的完整URL
                string configUrl = resourcePath.TrimEnd('/') + "/config.json";
                
                // 使用HttpClient发送请求
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, configUrl);
                request.Headers.Add("User-Agent", "ETFramework-ResourceConfigReader/1.0");

                HttpResponseMessage response = await HttpClient.SendAsync(request, CancellationToken.None);
                
                if (response.IsSuccessStatusCode)
                {
                    // 直接读取JSON字符串
                    string json = await response.Content.ReadAsStringAsync();
                    
                    if (string.IsNullOrEmpty(json))
                    {
                        throw new Exception("资源服返回的 config.json 内容为空");
                    }
                    
                    return json;
                }
                else
                {
                    Log.Warning($"资源服返回状态码: {response.StatusCode}");
                    throw new Exception($"资源服返回错误状态码: {response.StatusCode}");
                }
            }
            catch (HttpRequestException e)
            {
                Log.Warning($"访问资源服失败: {e.Message}");
                throw new Exception($"无法访问资源服 config.json: {e.Message}", e);
            }
            catch (TaskCanceledException e)
            {
                Log.Warning($"访问资源服超时: {e.Message}");
                throw new Exception($"访问资源服 config.json 超时: {e.Message}", e);
            }
            catch (Exception e)
            {
                Log.Error($"获取资源服配置时发生异常: {e}");
                throw;
            }
        }
    }
}