using System.Collections.Generic;
using System.ComponentModel;
using System.Net;

namespace ET
{
    [EnableClass]
    public partial class StartSceneConfig
    {
        public ActorId ActorId;
        
        public int Type;

        public StartProcessConfig StartProcessConfig => StartProcessConfigConfigCategory.Instance.Get(this.Process);

        public StartZoneConfig StartZoneConfig
        {
            get
            {
                return StartZoneConfigConfigCategory.Instance.Get(this.Zone);
            }
        }

        // 内网地址外网端口，通过防火墙映射端口过来
        private IPEndPoint innerIPPort;

        public IPEndPoint InnerIPPort
        {
            get
            {
                if (innerIPPort == null)
                {
                    this.innerIPPort = NetworkHelper.ToIPEndPoint($"{this.StartProcessConfig.InnerIP}:{this.Port}");
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
                return this.outerIPPort ??= NetworkHelper.ToIPEndPoint($"{this.StartProcessConfig.OuterIP}:{this.Port}");
            }
        }

        public void Init()
        {
            this.ActorId = new ActorId(this.Process, this.Id, 1);
            this.Type = SceneTypeSingleton.Instance.GetSceneType(this.SceneType);
            Log.Error($"{this.Process}  | {this.Id}  |  {this.Type}  | {this.SceneType}");
        }
    }
}