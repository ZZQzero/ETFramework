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
            try
            {
                self.Listener = new HttpListener();

                foreach (string s in address.Split(';'))
                {
                    if (s.Trim() == "")
                    {
                        continue;
                    }
                    self.Listener.Prefixes.Add(s);
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
            self.Listener.Stop();
            self.Listener.Close();
        }

        private static async ETTask Accept(this HttpComponent self)
        {
            long instanceId = self.InstanceId;
            while (self.InstanceId == instanceId)
            {
                try
                {
                    HttpListenerContext context = await self.Listener.GetContextAsync();
                    self.Handle(context).NoContext();
                }
                catch (ObjectDisposedException)
                {
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }

        private static async ETTask Handle(this HttpComponent self, HttpListenerContext context)
        {
            try
            {
                if (context.Request.Url != null)
                {
                    string path = context.Request.Url.AbsolutePath;
                    IHttpHandler handler = HttpDispatcher.Instance.Get(self.IScene.SceneType, path);
                    await handler.Handle(self.Scene(), context);
                }
                else
                {
                    Log.Error("context.Request.Url isNull");
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
            finally
            {
                context.Response.Close();    
            }
            
        }
    }
}