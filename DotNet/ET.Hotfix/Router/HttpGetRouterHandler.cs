using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Nino.Core;

namespace ET
{
    [HttpHandler(SceneType.RouterManager, "/get_router")]
    public class HttpGetRouterHandler : IHttpHandler
    {
        public async ETTask Handle(Scene scene, HttpListenerContext context)
        {
            HttpGetRouterResponse httpGetRouterResponse = HttpGetRouterResponse.Create();
            List<StartSceneTable> realms = StartSceneConfigManager.Instance.GetBySceneType(SceneType.Realm);
            foreach (StartSceneTable startSceneConfig in realms)
            {
                httpGetRouterResponse.Realms.Add(startSceneConfig.InnerIPPort.ToString());
            }
            foreach (StartSceneTable startSceneConfig in StartSceneConfigManager.Instance.GetBySceneType(SceneType.Router))
            {
                httpGetRouterResponse.Routers.Add($"{startSceneConfig.ProcessConfig.OuterIP}:{startSceneConfig.Port}");
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

            try
            {
                byte[] bytes = NinoSerializer.Serialize(httpGetRouterResponse);
                await HttpHelper.ResponseBinary(response, HttpStatusCode.OK, bytes, "application/octet-stream");
            }
            catch (Exception e)
            {
                Log.Error(e + "\n" + e.StackTrace);
                // 尽量返回 500 信息给客户端
                try
                {
                    if (response.OutputStream.CanWrite)
                    {
                        await HttpHelper.ResponseText(response, HttpStatusCode.InternalServerError, "Internal Server Error");
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
