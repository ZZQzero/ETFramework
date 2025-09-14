using System.Net;

namespace ET
{
    public interface IHttpHandler
    {
        ETTask Handle(Scene scene, HttpListenerContext context);
    }
}