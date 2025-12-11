using System;
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
                    
                    if (prefix[^1] != '/')
                    {
                        prefix += "/";
                    }

                    self.Listener.Prefixes.Add(prefix);
                }

                self.Listener.Start();

                // 启动多个Accept任务以支持并发处理，提高吞吐量
                // HttpListener.GetContextAsync() 是线程安全的，可以并发调用
                const int acceptCount = 1; // 并发接受连接数，可根据实际负载调整
                for (int i = 0; i < acceptCount; i++)
                {
                    self.Accept().NoContext();
                }
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
                if (listener == null || !listener.IsListening)
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
                    // Listener 已被释放，正常退出
                    break;
                }
                catch (HttpListenerException) when (!listener.IsListening)
                {
                    // Listener 已停止，正常退出
                    break;
                }
                catch (HttpListenerException e)
                {
                    // 其他 HttpListenerException（很少见，通常是临时网络问题）
                    Log.Warning($"HttpListener异常，继续等待: {e.Message}");
                }
                catch (Exception e)
                {
                    Log.Error($"Accept循环异常: {e}");
                }
            }
        }

        private static async ETTask Handle(this HttpComponent self, HttpListenerContext context)
        {
            HttpListenerResponse response = context.Response;
            try
            {
                if (self.IsDisposed)
                {
                    await TryResponseText(response, HttpStatusCode.ServiceUnavailable, "Service Unavailable");
                    return;
                }

                HttpListenerRequest request = context.Request;
                if (request?.Url == null)
                {
                    Log.Error("Http request url is null");
                    await TryResponseText(response, HttpStatusCode.BadRequest, "Bad Request");
                    return;
                }

                string path = request.Url.AbsolutePath;
                int sceneType = self.IScene.SceneType;
                if (!HttpDispatcher.Instance.TryGet(sceneType, path, out IHttpHandler handler))
                {
                    Log.Warning($"Http handler not found, sceneType: {sceneType}, path: {path}");
                    await TryResponseText(response, HttpStatusCode.NotFound, "Not Found");
                    return;
                }
                
                await handler.Handle(self.Scene(), context);
            }
            catch (ObjectDisposedException)
            {
                // HttpListener 或 Response 已被释放，静默处理
            }
            catch (HttpListenerException e)
            {
                // 网络相关异常，记录但不返回错误响应（可能已经关闭）
                Log.Warning($"HttpListener exception in Handle: {e.Message}");
            }
            catch (Exception e)
            {
                Log.Error($"Unexpected exception in HttpComponent.Handle: {e}");
                try
                {
                    await TryResponseText(response, HttpStatusCode.InternalServerError, "Internal Server Error");
                }
                catch (Exception inner)
                {
                    Log.Error($"Failed to send error response: {inner}");
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
                    // 忽略关闭时的异常（可能已经关闭）
                    Log.Debug($"Exception closing response: {closeException.Message}");
                }
            }
        }

        private static async ETTask TryResponseText(HttpListenerResponse response, HttpStatusCode statusCode, string message)
        {
            if (response.OutputStream.CanWrite)
            {
                await HttpHelper.ResponseText(response, statusCode, message);
            }
        }
    }
}