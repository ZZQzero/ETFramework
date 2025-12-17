using Nino.Core;
using System.Collections.Generic;

namespace ET
{
    [NinoType(false)]
    [Message(RouterProto.HttpGetRouterResponse)]
    public partial class HttpGetRouterResponse : MessageObject
    {
        public static HttpGetRouterResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<HttpGetRouterResponse>(isFromPool);
        }

        [NinoMember(0)]
        public List<string> Realms { get; set; } = new();

        [NinoMember(1)]
        public List<string> Routers { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Realms.Clear();
            this.Routers.Clear();

            ObjectPool.Recycle(this);
        }
    }

    public static class RouterProto
    {
        public const ushort HttpGetRouterResponse = 31221;
    }
}