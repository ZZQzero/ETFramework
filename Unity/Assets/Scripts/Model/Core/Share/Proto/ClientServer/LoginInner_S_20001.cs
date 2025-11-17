using Nino.Core;
using System.Collections.Generic;

namespace ET
{
    [NinoType(false)]
    [Message(LoginInner.R2G_GetLoginKey)]
    [ResponseType(nameof(G2R_GetLoginKey))]
    public partial class R2G_GetLoginKey : MessageObject, IRequest
    {
        public static R2G_GetLoginKey Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<R2G_GetLoginKey>(isFromPool);
        }

        [NinoMember(0)]
        public int RpcId { get; set; }

        [NinoMember(1)]
        public string Account { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Account = default;

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(LoginInner.G2R_GetLoginKey)]
    public partial class G2R_GetLoginKey : MessageObject, IResponse
    {
        public static G2R_GetLoginKey Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2R_GetLoginKey>(isFromPool);
        }

        [NinoMember(0)]
        public int RpcId { get; set; }

        [NinoMember(1)]
        public int Error { get; set; }

        [NinoMember(2)]
        public string Message { get; set; }

        [NinoMember(3)]
        public long Key { get; set; }

        [NinoMember(4)]
        public long GateId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.Key = default;
            this.GateId = default;

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(LoginInner.G2M_SessionDisconnect)]
    public partial class G2M_SessionDisconnect : MessageObject, ILocationMessage
    {
        public static G2M_SessionDisconnect Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2M_SessionDisconnect>(isFromPool);
        }

        [NinoMember(0)]
        public int RpcId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(LoginInner.R2L_AccountRequest)]
    [ResponseType(nameof(L2R_AccountResponse))]
    public partial class R2L_AccountRequest : MessageObject, IRequest
    {
        public static R2L_AccountRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<R2L_AccountRequest>(isFromPool);
        }

        [NinoMember(0)]
        public int RpcId { get; set; }

        [NinoMember(1)]
        public string Account { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Account = default;

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(LoginInner.L2R_AccountResponse)]
    public partial class L2R_AccountResponse : MessageObject, IResponse
    {
        public static L2R_AccountResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<L2R_AccountResponse>(isFromPool);
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

            this.RpcId = default;
            this.Error = default;
            this.Message = default;

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(LoginInner.L2G_DisconnectGateUnit)]
    [ResponseType(nameof(G2L_DisconnectGateUnit))]
    public partial class L2G_DisconnectGateUnit : MessageObject, IRequest
    {
        public static L2G_DisconnectGateUnit Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<L2G_DisconnectGateUnit>(isFromPool);
        }

        [NinoMember(0)]
        public int RpcId { get; set; }

        [NinoMember(1)]
        public string AccountName { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.AccountName = default;

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(LoginInner.G2L_DisconnectGateUnit)]
    public partial class G2L_DisconnectGateUnit : MessageObject, IResponse
    {
        public static G2L_DisconnectGateUnit Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2L_DisconnectGateUnit>(isFromPool);
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

            this.RpcId = default;
            this.Error = default;
            this.Message = default;

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(LoginInner.Main2NetClient_LoginGame)]
    [ResponseType(nameof(NetClient2Main_LoginGame))]
    public partial class Main2NetClient_LoginGame : MessageObject, IRequest
    {
        public static Main2NetClient_LoginGame Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<Main2NetClient_LoginGame>(isFromPool);
        }

        [NinoMember(0)]
        public int RpcId { get; set; }

        [NinoMember(1)]
        public string Account { get; set; }

        [NinoMember(2)]
        public long RealmKey { get; set; }

        [NinoMember(3)]
        public long RoleId { get; set; }

        [NinoMember(4)]
        public string GateAddress { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Account = default;
            this.RealmKey = default;
            this.RoleId = default;
            this.GateAddress = default;

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(LoginInner.NetClient2Main_LoginGame)]
    public partial class NetClient2Main_LoginGame : MessageObject, IResponse
    {
        public static NetClient2Main_LoginGame Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<NetClient2Main_LoginGame>(isFromPool);
        }

        [NinoMember(0)]
        public int RpcId { get; set; }

        [NinoMember(1)]
        public int Error { get; set; }

        [NinoMember(2)]
        public string Message { get; set; }

        [NinoMember(3)]
        public long PlayerId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.PlayerId = default;

            ObjectPool.Recycle(this);
        }
    }

    public static class LoginInner
    {
        public const ushort R2G_GetLoginKey = 20002;
        public const ushort G2R_GetLoginKey = 20003;
        public const ushort G2M_SessionDisconnect = 20004;
        public const ushort R2L_AccountRequest = 20005;
        public const ushort L2R_AccountResponse = 20006;
        public const ushort L2G_DisconnectGateUnit = 20007;
        public const ushort G2L_DisconnectGateUnit = 20008;
        public const ushort Main2NetClient_LoginGame = 20009;
        public const ushort NetClient2Main_LoginGame = 20010;
    }
}