using System;
using System.IO;
using System.Net.Http;
#if UNITY
using UnityEngine;
using UnityEngine.Networking;
#endif

namespace ET
{
    public static partial class HttpClientHelper
    {
        public static async ETTask<byte[]> Get(string link)
        {
            try
            {
#if UNITY
                using UnityEngine.Networking.UnityWebRequest req = UnityEngine.Networking.UnityWebRequest.Get(link);
                await req.SendWebRequest();
                return req.downloadHandler.data;
#else
                using HttpClient httpClient = new();
                HttpResponseMessage response =  await httpClient.GetAsync(link);
                var result = await response.Content.ReadAsByteArrayAsync();
                return result;
#endif
            }
            catch (Exception e)
            {
                throw new Exception($"http request fail: {link.Substring(0,link.IndexOf('?'))}\n{e}");
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