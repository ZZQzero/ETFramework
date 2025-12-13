using System;
using System.Net;
using System.Text;

namespace ET
{
    public static partial class HttpHelper
    {
        public static void Response(HttpListenerContext context, object response)
        {
            byte[] bytes = MongoHelper.ToJson(response).ToUtf8();
            context.Response.StatusCode = 200;
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.ContentLength64 = bytes.Length;
            context.Response.OutputStream.Write(bytes, 0, bytes.Length);
        }

        public static ETTask ResponseText(HttpListenerContext context, HttpStatusCode statusCode, string message)
        {
            return ResponseText(context.Response, statusCode, message);
        }

        public static async ETTask ResponseText(HttpListenerResponse response, HttpStatusCode statusCode, string message)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(message ?? string.Empty);
            response.StatusCode = (int)statusCode;
            response.ContentType = "text/plain; charset=utf-8";
            response.ContentLength64 = bytes.Length;
            await response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
        }
        
        public static async ETTask ResponseText(HttpListenerResponse response, HttpStatusCode statusCode, string message, string contentType)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(message ?? string.Empty);
            response.StatusCode = (int)statusCode;
            response.ContentType = contentType;
            response.ContentLength64 = bytes.Length;
            await response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
        }

        public static async ETTask ResponseBinary(HttpListenerResponse response, HttpStatusCode statusCode, byte[] payload, string contentType)
        {
            payload ??= [];
            response.StatusCode = (int)statusCode;
            response.ContentType = contentType;
            response.ContentLength64 = payload.Length;

            if (payload.Length > 0)
            {
                await response.OutputStream.WriteAsync(payload, 0, payload.Length);
            }
        }
    }
}