using System;
using System.Collections.Generic;
using System.Net;

namespace ET
{
    [EntitySystemOf(typeof(HttpComponent))]
    public static partial class HttpComponentSystem
    {
        [EntitySystem]
        private static void Awake(this HttpComponent self, string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentException("http address is null or empty", nameof(address));
            }

            try
            {
                self.Listener = new HttpListener();

                foreach (string s in address.Split(';'))
                {
                    string prefix = s.Trim();
                    if (prefix.Length == 0)
                    {
                        continue;
                    }

                    if (!prefix.EndsWith('/'))
                    {
                        prefix += "/";
                    }

                    self.Listener.Prefixes.Add(prefix);
                }

                self.Listener.Start();

                self.Accept().NoContext();
            }
            catch (HttpListenerException e)
            {
                throw new Exception($"请先在cmd中运行: netsh http add urlacl url=http://*:你的address中的端口/ user=Everyone, address: {address}", e);
            }
        }
        
        [EntitySystem]
        private static void Destroy(this HttpComponent self)
        {
            if (self.Listener == null)
            {
                return;
            }

            try
            {
                if (self.Listener.IsListening)
                {
                    self.Listener.Stop();
                }
                self.Listener.Close();
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
            finally
            {
                self.Listener = null;
            }
        }

        private static async ETTask Accept(this HttpComponent self)
        {
            long instanceId = self.InstanceId;
            while (self.InstanceId == instanceId)
            {
                HttpListener listener = self.Listener;
                if (listener == null)
                {
                    break;
                }

                try
                {
                    HttpListenerContext context = await listener.GetContextAsync();
                    self.Handle(context).NoContext();
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (HttpListenerException) when (!listener.IsListening)
                {
                    break;
                }
                catch (HttpListenerException e)
                {
                    Log.Error(e);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }

        private static async ETTask Handle(this HttpComponent self, HttpListenerContext context)
        {
            HttpListenerResponse response = context.Response;
            try
            {
                HttpListenerRequest request = context.Request;
                if (request?.Url == null)
                {
                    Log.Error("Http request url is null");
                    if (response.OutputStream.CanWrite)
                    {
                        await HttpHelper.ResponseText(response, HttpStatusCode.BadRequest, "Bad Request");
                    }
                    return;
                }

                string path = request.Url.AbsolutePath;
                if (!HttpDispatcher.Instance.TryGet(self.IScene.SceneType, path, out IHttpHandler handler))
                {
                    Log.Warning($"Http handler not found, sceneType: {self.IScene.SceneType}, path: {path}");
                    if (response.OutputStream.CanWrite)
                    {
                        await HttpHelper.ResponseText(response, HttpStatusCode.NotFound, "Not Found");
                    }
                    return;
                }

                await handler.Handle(self.Scene(), context);
            }
            catch (Exception e)
            {
                Log.Error(e);
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
            finally
            {
                try
                {
                    response.Close();    
                }
                catch (Exception closeException)
                {
                    Log.Error(closeException);
                }
            }
        }
    }
}