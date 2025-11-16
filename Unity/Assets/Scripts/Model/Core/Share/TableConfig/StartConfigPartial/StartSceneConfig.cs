using System.Collections.Generic;
using System.ComponentModel;
using System.Net;

namespace ET
{
    [EnableClass]
    public partial class StartSceneTable
    {
        public ActorId ActorId;
        
        public int Type;

        public StartProcessTable ProcessConfig => StartProcessConfig.Instance.Get(this.Process);

        public StartZoneTable ZoneConfig => StartZoneConfig.Instance.Get(this.Zone);

        // 内网地址外网端口，通过防火墙映射端口过来
        private IPEndPoint innerIPPort;

        public IPEndPoint InnerIPPort
        {
            get
            {
                if (innerIPPort == null)
                {
                    this.innerIPPort = NetworkHelper.ToIPEndPoint($"{this.ProcessConfig.InnerIP}:{this.Port}");
                }

                return this.innerIPPort;
            }
        }

        private IPEndPoint outerIPPort;

        // 外网地址外网端口
        public IPEndPoint OuterIPPort
        {
            get
            {
                return this.outerIPPort ??= NetworkHelper.ToIPEndPoint($"{this.ProcessConfig.OuterIP}:{this.Port}");
            }
        }

        public void Init()
        {
            this.ActorId = new ActorId(this.Process, this.Id, 1);
            this.Type = SceneTypeSingleton.Instance.GetSceneType(this.SceneType);
        }
    }
}