using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using MemoryPack;

namespace ET
{
    [HttpHandler(SceneType.RouterManager, "/get_router")]
    public class HttpGetRouterHandler : IHttpHandler
    {
        public async ETTask Handle(Scene scene, HttpListenerContext context)
        {
            HttpGetRouterResponse httpGetRouterResponse = HttpGetRouterResponse.Create();
            List<StartSceneConfig> realms = StartSceneConfigManager.Instance.GetBySceneType(SceneType.Realm);
            foreach (StartSceneConfig startSceneConfig in realms)
            {
                httpGetRouterResponse.Realms.Add(startSceneConfig.InnerIPPort.ToString());
            }
            foreach (StartSceneConfig startSceneConfig in StartSceneConfigManager.Instance.GetBySceneType(SceneType.Router))
            {
                httpGetRouterResponse.Routers.Add($"{startSceneConfig.StartProcessConfig.OuterIP}:{startSceneConfig.Port}");
            }

            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            // Always allow origin (or restrict in production)
            response.AppendHeader("Access-Control-Allow-Origin", "*");

            // Preflight
            if (request.HttpMethod == "OPTIONS")
            {
                response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept, X-Requested-With");
                response.AddHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                response.AddHeader("Access-Control-Max-Age", "1728000");
                response.StatusCode = (int)HttpStatusCode.OK;
                response.ContentLength64 = 0;
                return; // don't continue to serialization
            }

            // Binary response for MemoryPack
            response.ContentType = "application/octet-stream";
            response.StatusCode = (int)HttpStatusCode.OK;

            try
            {
                byte[] bytes = MemoryPackSerializer.Serialize(httpGetRouterResponse);
                response.ContentLength64 = bytes.Length;
                await response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            }
            catch (Exception e)
            {
                Log.Error(e + "\n" + e.StackTrace);
                // 尽量返回 500 信息给客户端
                try
                {
                    if (response.OutputStream.CanWrite)
                    {
                        response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        byte[] err = "Internal Server Error"u8.ToArray();
                        response.ContentType = "text/plain";
                        response.ContentLength64 = err.Length;
                        await response.OutputStream.WriteAsync(err, 0, err.Length);
                    }
                }
                catch (Exception inner)
                {
                    Log.Error(inner);
                }
            }
        }
    }
}
