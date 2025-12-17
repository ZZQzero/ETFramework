using Nino.Core;
using System.Collections.Generic;

namespace ET
{
    [NinoType(false)]
    [Message(StateSyncInner.M2A_Reload)]
    [ResponseType(nameof(A2M_Reload))]
    public partial class M2A_Reload : MessageObject, IRequest
    {
        public static M2A_Reload Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2A_Reload>(isFromPool);
        }

        [NinoMember(0)]
        public int RpcId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = 0;

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(StateSyncInner.A2M_Reload)]
    public partial class A2M_Reload : MessageObject, IResponse
    {
        public static A2M_Reload Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<A2M_Reload>(isFromPool);
        }

        [NinoMember(0)]
        public int RpcId { get; set; }

        [NinoMember(1)]
        public int Error { get; set; }

        [NinoMember(2)]
        public string Message { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = 0;
            this.Error = 0;
            this.Message = null;

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(StateSyncInner.M2M_UnitTransferRequest)]
    [ResponseType(nameof(M2M_UnitTransferResponse))]
    public partial class M2M_UnitTransferRequest : MessageObject, IRequest
    {
        public static M2M_UnitTransferRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2M_UnitTransferRequest>(isFromPool);
        }

        [NinoMember(0)]
        public int RpcId { get; set; }

        [NinoMember(1)]
        public ActorId OldActorId { get; set; }

        [NinoMember(2)]
        public UnitInfo UnitInfo { get; set; }

        [NinoMember(3)]
        public List<byte[]> Entitys { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = 0;
            this.OldActorId = default;
            this.UnitInfo?.Dispose();
            this.UnitInfo = null;
            this.Entitys.Clear();

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(StateSyncInner.M2M_UnitTransferResponse)]
    public partial class M2M_UnitTransferResponse : MessageObject, IResponse
    {
        public static M2M_UnitTransferResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2M_UnitTransferResponse>(isFromPool);
        }

        [NinoMember(0)]
        public int RpcId { get; set; }

        [NinoMember(1)]
        public int Error { get; set; }

        [NinoMember(2)]
        public string Message { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = 0;
            this.Error = 0;
            this.Message = null;

            ObjectPool.Recycle(this);
        }
    }

    public static class StateSyncInner
    {
        public const ushort M2A_Reload = 11595;
        public const ushort A2M_Reload = 38719;
        public const ushort M2M_UnitTransferRequest = 34708;
        public const ushort M2M_UnitTransferResponse = 13520;
    }
}