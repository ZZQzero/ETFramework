using System;
using System.Collections.Generic;

namespace ET
{
    public class HttpDispatcher: Singleton<HttpDispatcher>, ISingletonAwake
    {
        private readonly Dictionary<string, Dictionary<int, IHttpHandler>> dispatcher = new();
        
        public void Awake()
        {
        }

        public void HttpRegister<T>(int sceneType, string path) where T : IHttpHandler, new()
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("http path is null or empty", nameof(path));
            }

            IHttpHandler http = new T();
            this.InternalRegister(sceneType, NormalizePath(path), http);
        }

        public bool TryGet(int sceneType, string path, out IHttpHandler handler)
        {
            handler = default;
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            path = NormalizePath(path);
            if (!this.dispatcher.TryGetValue(path, out Dictionary<int, IHttpHandler> dict))
            {
                return false;
            }

            return dict.TryGetValue(sceneType, out handler);
        }

        public IHttpHandler Get(int sceneType, string path)
        {
            if (this.TryGet(sceneType, path, out IHttpHandler handler))
            {
                return handler;
            }

            throw new KeyNotFoundException($"Http handler not found, path: {path}, sceneType: {sceneType}");
        }

        private void InternalRegister(int sceneType, string path, IHttpHandler handler)
        {
            if (!this.dispatcher.TryGetValue(path, out Dictionary<int, IHttpHandler> dict))
            {
                dict = new Dictionary<int, IHttpHandler>();
                this.dispatcher.Add(path, dict);
            }

            if (!dict.TryAdd(sceneType, handler))
            {
                Log.Warning($"Http handler already registered, path: {path}, sceneType: {sceneType}");
                dict[sceneType] = handler;
            }
        }

        private static string NormalizePath(string path)
        {
            path = path.Trim();
            if (!path.StartsWith('/'))
            {
                path = "/" + path;
            }
            return path;
        }
    }
}