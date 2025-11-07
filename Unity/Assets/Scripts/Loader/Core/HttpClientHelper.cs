using System;
using System.IO;
using System.Net.Http;
#if UNITY
using UnityEngine.Networking;
#endif

namespace ET
{
    public static partial class HttpClientHelper
    {
        public static async ETTask<byte[]> Get(string link, int timeoutSeconds = 30)
        {
            try
            {
#if UNITY
                using UnityEngine.Networking.UnityWebRequest req = UnityEngine.Networking.UnityWebRequest.Get(link);
                if (timeoutSeconds > 0)
                {
                    req.timeout = timeoutSeconds;
                }
                await req.SendWebRequest();
                
                // UnityWebRequest 需要检查错误
                if (req.result != UnityWebRequest.Result.Success)
                {
                    throw new Exception($"HTTP请求失败: {link}, {req.result} ,状态码: {req.responseCode}, 错误: {req.error}");
                }
                
                // 检查HTTP状态码
                if (req.responseCode >= 400)
                {
                    throw new Exception($"HTTP错误: {link}, 状态码: {req.responseCode}, 响应: {req.downloadHandler?.text ?? "无响应内容"}");
                }
                
                return req.downloadHandler.data;
#else
                using HttpClient httpClient = new();
                if (timeoutSeconds > 0)
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
                }
                HttpResponseMessage response = await httpClient.GetAsync(link);
                
                // 检查HTTP状态码
                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"HTTP错误: {link}, 状态码: {(int)response.StatusCode} {response.StatusCode}, 响应: {errorContent}");
                }
                
                var result = await response.Content.ReadAsByteArrayAsync();
                return result;
#endif
            }
            catch (Exception e)
            {
                string cleanUrl = link.Contains('?') ? link.Substring(0, link.IndexOf('?')) : link;
                throw new Exception($"HTTP请求失败: {cleanUrl}\n{e}");
            }
        }
        
        public static async ETTask<string> HttpGet(string link)
        {
            try
            {
                UnityWebRequest req = UnityWebRequest.Get(link);
                await req.SendWebRequest();
                return req.downloadHandler.text;
            }
            catch (Exception e)
            {
                throw new Exception($"http request fail: {link.Substring(0,link.IndexOf('?'))}\n{e}");
            }
        }
        
    }
}