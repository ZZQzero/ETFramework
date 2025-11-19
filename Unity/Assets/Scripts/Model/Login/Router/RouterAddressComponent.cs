using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace ET
{
    [ComponentOf(typeof(Scene))]
    public class RouterAddressComponent: Entity, IAwake<string>
    {
        public AddressFamily AddressFamily { get; set; }
        public HttpGetRouterResponse Info;
        public string Address;
        public int RouterIndex;
        public long CacheTime;
        
        public const long CacheValidTime = 5 * 60 * 1000; // 5分钟
    }
}