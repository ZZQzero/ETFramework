using System.Net;

namespace ET
{
    [EnableClass]
    public partial class StartProcessTable
    {
        public string InnerIP => this.MachineConfig.InnerIP;

        public string OuterIP => this.MachineConfig.OuterIP;
        
        // 内网地址外网端口，通过防火墙映射端口过来
        private IPEndPoint ipEndPoint;

        public IPEndPoint IPEndPoint
        {
            get
            {
                if (ipEndPoint == null)
                {
                    this.ipEndPoint = NetworkHelper.ToIPEndPoint(this.InnerIP, this.Port);
                }

                return this.ipEndPoint;
            }
        }

        public StartMachineTable MachineConfig => StartMachineConfig.Instance.Get(this.MachineId);
    }
}