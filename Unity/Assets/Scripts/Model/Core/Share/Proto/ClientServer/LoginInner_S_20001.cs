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
        public long UserId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = 0;
            this.UserId = 0;

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

            this.RpcId = 0;
            this.Error = 0;
            this.Message = null;
            this.Key = 0;
            this.GateId = 0;

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

            this.RpcId = 0;

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
        public long UserId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = 0;
            this.UserId = 0;

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

            this.RpcId = 0;
            this.Error = 0;
            this.Message = null;

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
        public long UserId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = 0;
            this.UserId = 0;

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

            this.RpcId = 0;
            this.Error = 0;
            this.Message = null;

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

        /// <summary>
        /// 用户ID
        /// </summary>
        [NinoMember(3)]
        public long UserId { get; set; }

        /// <summary>
        /// 角色ID
        /// </summary>
        [NinoMember(4)]
        public long RoleId { get; set; }

        [NinoMember(5)]
        public string GateAddress { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = 0;
            this.Account = null;
            this.RealmKey = 0;
            this.UserId = 0;
            this.RoleId = 0;
            this.GateAddress = null;

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

            this.RpcId = 0;
            this.Error = 0;
            this.Message = null;
            this.PlayerId = 0;

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(LoginInner.G2L_AddLoginRecord)]
    [ResponseType(nameof(L2G_AddLoginRecord))]
    public partial class G2L_AddLoginRecord : MessageObject, IRequest
    {
        public static G2L_AddLoginRecord Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2L_AddLoginRecord>(isFromPool);
        }

        [NinoMember(0)]
        public int RpcId { get; set; }

        [NinoMember(1)]
        public long UserId { get; set; }

        [NinoMember(2)]
        public int ServerId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = 0;
            this.UserId = 0;
            this.ServerId = 0;

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(LoginInner.L2G_AddLoginRecord)]
    public partial class L2G_AddLoginRecord : MessageObject, IResponse
    {
        public static L2G_AddLoginRecord Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<L2G_AddLoginRecord>(isFromPool);
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
    [Message(LoginInner.G2M_RequestExitGame)]
    [ResponseType(nameof(M2G_RequestExitGame))]
    public partial class G2M_RequestExitGame : MessageObject, ILocationRequest
    {
        public static G2M_RequestExitGame Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2M_RequestExitGame>(isFromPool);
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
    [Message(LoginInner.M2G_RequestExitGame)]
    public partial class M2G_RequestExitGame : MessageObject, ILocationResponse
    {
        public static M2G_RequestExitGame Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2G_RequestExitGame>(isFromPool);
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
    [Message(LoginInner.G2L_RemoveLoginRecord)]
    [ResponseType(nameof(L2G_RemoveLoginRecord))]
    public partial class G2L_RemoveLoginRecord : MessageObject, IRequest
    {
        public static G2L_RemoveLoginRecord Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2L_RemoveLoginRecord>(isFromPool);
        }

        [NinoMember(0)]
        public int RpcId { get; set; }

        [NinoMember(1)]
        public string AccountName { get; set; }

        [NinoMember(2)]
        public int ServerId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = 0;
            this.AccountName = null;
            this.ServerId = 0;

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(LoginInner.L2G_RemoveLoginRecord)]
    public partial class L2G_RemoveLoginRecord : MessageObject, IResponse
    {
        public static L2G_RemoveLoginRecord Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<L2G_RemoveLoginRecord>(isFromPool);
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
    [Message(LoginInner.G2M_SecondLogin)]
    [ResponseType(nameof(M2G_SecondLogin))]
    public partial class G2M_SecondLogin : MessageObject, ILocationRequest
    {
        public static G2M_SecondLogin Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2M_SecondLogin>(isFromPool);
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
    [Message(LoginInner.M2G_SecondLogin)]
    public partial class M2G_SecondLogin : MessageObject, ILocationResponse
    {
        public static M2G_SecondLogin Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2G_SecondLogin>(isFromPool);
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

    public static class LoginInner
    {
        public const ushort R2G_GetLoginKey = 15589;
        public const ushort G2R_GetLoginKey = 10201;
        public const ushort G2M_SessionDisconnect = 12862;
        public const ushort R2L_AccountRequest = 60896;
        public const ushort L2R_AccountResponse = 38788;
        public const ushort L2G_DisconnectGateUnit = 322;
        public const ushort G2L_DisconnectGateUnit = 42370;
        public const ushort Main2NetClient_LoginGame = 63694;
        public const ushort NetClient2Main_LoginGame = 40110;
        public const ushort G2L_AddLoginRecord = 50404;
        public const ushort L2G_AddLoginRecord = 53028;
        public const ushort G2M_RequestExitGame = 20011;
        public const ushort M2G_RequestExitGame = 29035;
        public const ushort G2L_RemoveLoginRecord = 24935;
        public const ushort L2G_RemoveLoginRecord = 9271;
        public const ushort G2M_SecondLogin = 24221;
        public const ushort M2G_SecondLogin = 59145;
    }
}