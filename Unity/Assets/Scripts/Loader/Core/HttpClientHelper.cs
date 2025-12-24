using System;
using System.Net.Http;
#if UNITY
using UnityEngine.Networking;
#endif

namespace ET
{
    public static partial class HttpClientHelper
    {
        public static async ETTask<byte[]> GetBytes(string link, int timeoutSeconds = 30)
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
        
        public static async ETTask<string> GetJson(string link)
        {
            try
            {
#if UNITY
                UnityWebRequest req = UnityWebRequest.Get(link);
                await req.SendWebRequest();
                // UnityWebRequest 需要检查错误
                if (req.result != UnityWebRequest.Result.Success)
                {
                    Log.Error($"HTTP请求失败: {link}, {req.result} ,状态码: {req.responseCode}, 错误: {req.error}");
                    return null;
                }
                
                // 检查HTTP状态码
                if (req.responseCode >= 400)
                {
                    Log.Error($"HTTP错误: {link}, 状态码: {req.responseCode}, 响应: {req.downloadHandler?.text ?? "无响应内容"}");
                    return null;
                }
                return req.downloadHandler.text;
#else
                using HttpClient httpClient = new();
                HttpResponseMessage response = await httpClient.GetAsync(link);
                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"HTTP错误: {link}, 状态码: {(int)response.StatusCode} {response.StatusCode}, 响应: {errorContent}");
                }
                return await response.Content.ReadAsStringAsync();
#endif
            }
            catch (Exception e)
            {
                string cleanUrl = link.Contains('?') ? link.Substring(0, link.IndexOf('?')) : link;
                throw new Exception($"http request fail: {cleanUrl}\n{e}");
            }
        }

        /// <summary>
        /// HTTP POST请求方法（直接传序列化后的bytes，效率更高）
        /// </summary>
        /// <param name="url">请求URL</param>
        /// <param name="bodyBytes">请求体的字节数组</param>
        /// <param name="timeoutSeconds">超时时间（秒），0表示不设置超时</param>
        /// <returns>响应体的字节数组</returns>
        public static async ETTask<byte[]> PostBytes(string url, byte[] bodyBytes, int timeoutSeconds = 30)
        {
            try
            {
#if UNITY
                using UnityEngine.Networking.UnityWebRequest req = new UnityEngine.Networking.UnityWebRequest(url, "POST");
                
                // 设置请求体
                if (bodyBytes != null && bodyBytes.Length > 0)
                {
                    req.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyBytes);
                    req.uploadHandler.contentType = "application/octet-stream";
                }
                req.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();

                // 设置超时
                if (timeoutSeconds > 0)
                {
                    req.timeout = timeoutSeconds;
                }

                // 添加Authorization请求头
                string apiKey = "your-secret-api-key-change-in-production";
                req.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                await req.SendWebRequest();

                // UnityWebRequest 需要检查错误
                if (req.result != UnityWebRequest.Result.Success)
                {
                    throw new Exception($"HTTP请求失败: {url}, {req.result}, 状态码: {req.responseCode}, 错误: {req.error}");
                }

                // 检查HTTP状态码
                if (req.responseCode >= 400)
                {
                    string errorContent = req.downloadHandler?.text ?? "无响应内容";
                    throw new Exception($"HTTP错误: {url}, 状态码: {req.responseCode}, 响应: {errorContent}");
                }

                return req.downloadHandler.data;
#else
                using HttpClient httpClient = new();
                if (timeoutSeconds > 0)
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
                }

                // 添加Authorization请求头
                string apiKey = "your-secret-api-key-change-in-production";
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                HttpContent content = null;
                if (bodyBytes != null && bodyBytes.Length > 0)
                {
                    content = new ByteArrayContent(bodyBytes);
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                }

                HttpResponseMessage response = await httpClient.PostAsync(url, content);

                // 检查HTTP状态码
                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"HTTP错误: {url}, 状态码: {(int)response.StatusCode} {response.StatusCode}, 响应: {errorContent}");
                }

                byte[] result = await response.Content.ReadAsByteArrayAsync();
                return result;
#endif
            }
            catch (Exception e)
            {
                string cleanUrl = url.Contains('?') ? url.Substring(0, url.IndexOf('?')) : url;
                throw new Exception($"HTTP请求失败: {cleanUrl}\n{e}");
            }
        }
        
        /// <summary>
        /// HTTP POST请求方法（JSON字符串版本）
        /// </summary>
        /// <param name="url">请求URL</param>
        /// <param name="jsonBody">JSON格式的请求体</param>
        /// <param name="timeoutSeconds">超时时间（秒），0表示不设置超时</param>
        /// <returns>响应体的字符串内容</returns>
        public static async ETTask<string> Post(string url, string jsonBody, int timeoutSeconds = 30)
        {
            try
            {
#if UNITY
                using UnityEngine.Networking.UnityWebRequest req = new UnityEngine.Networking.UnityWebRequest(url, "POST");
                
                // 设置请求体
                if (!string.IsNullOrEmpty(jsonBody))
                {
                    byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
                    req.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
                    req.uploadHandler.contentType = "application/json";
                }
                req.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();

                // 设置超时
                if (timeoutSeconds > 0)
                {
                    req.timeout = timeoutSeconds;
                }

                // 添加Authorization请求头
                string apiKey = "your-secret-api-key-change-in-production";
                req.SetRequestHeader("Authorization", $"Bearer {apiKey}");

                await req.SendWebRequest();

                // UnityWebRequest 需要检查错误
                if (req.result != UnityWebRequest.Result.Success)
                {
                    throw new Exception($"HTTP请求失败: {url}, {req.result}, 状态码: {req.responseCode}, 错误: {req.error}");
                }

                // 检查HTTP状态码
                if (req.responseCode >= 400)
                {
                    string errorContent = req.downloadHandler?.text ?? "无响应内容";
                    throw new Exception($"HTTP错误: {url}, 状态码: {req.responseCode}, 响应: {errorContent}");
                }

                return req.downloadHandler.text;
#else
                using HttpClient httpClient = new();
                if (timeoutSeconds > 0)
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
                }

                // 添加Authorization请求头
                string apiKey = "your-secret-api-key-change-in-production";
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                HttpContent content = null;
                if (!string.IsNullOrEmpty(jsonBody))
                {
                    content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json");
                }

                HttpResponseMessage response = await httpClient.PostAsync(url, content);

                // 检查HTTP状态码
                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"HTTP错误: {url}, 状态码: {(int)response.StatusCode} {response.StatusCode}, 响应: {errorContent}");
                }

                string result = await response.Content.ReadAsStringAsync();
                return result;
#endif
            }
            catch (Exception e)
            {
                string cleanUrl = url.Contains('?') ? url.Substring(0, url.IndexOf('?')) : url;
                throw new Exception($"HTTP请求失败: {cleanUrl}\n{e}");
            }
        }

        /// <summary>
        /// 通用的HTTP POST请求（JSON格式）
        /// </summary>
        /// <param name="url">请求URL</param>
        /// <param name="jsonBody">JSON格式的请求体</param>
        /// <param name="timeoutSeconds">超时时间（秒），0表示不设置超时</param>
        /// <returns>响应体的字符串内容</returns>
        public static async ETTask<string> PostJson(string url, string jsonBody, int timeoutSeconds = 30)
        {
            return await Post(url, jsonBody, timeoutSeconds);
        }
        
    }
}