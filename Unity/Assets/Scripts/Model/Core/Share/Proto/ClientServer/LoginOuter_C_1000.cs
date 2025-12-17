using Nino.Core;
using System.Collections.Generic;

namespace ET
{
    // 用户信息
    [NinoType(false)]
    [Message(LoginOuter.UserInfo)]
    public partial class UserInfo : MessageObject
    {
        public static UserInfo Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<UserInfo>(isFromPool);
        }

        /// <summary>
        /// 登录账号
        /// </summary>
        [NinoMember(0)]
        public string Account { get; set; }

        /// <summary>
        /// 用户ID
        /// </summary>
        [NinoMember(1)]
        public long UserId { get; set; }

        /// <summary>
        /// 用户昵称
        /// </summary>
        [NinoMember(2)]
        public string Username { get; set; }

        /// <summary>
        /// VIP等级
        /// </summary>
        [NinoMember(3)]
        public int VipLevel { get; set; }

        /// <summary>
        /// 累计充值
        /// </summary>
        [NinoMember(4)]
        public long TotalRecharge { get; set; }

        /// <summary>
        /// 角色ID列表
        /// </summary>
        [NinoMember(5)]
        public List<long> RoleIds { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Account = null;
            this.UserId = 0;
            this.Username = null;
            this.VipLevel = 0;
            this.TotalRecharge = 0;
            this.RoleIds.Clear();

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(LoginOuter.Main2NetClient_Login)]
    [ResponseType(nameof(NetClient2Main_Login))]
    public partial class Main2NetClient_Login : MessageObject, IRequest
    {
        public static Main2NetClient_Login Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<Main2NetClient_Login>(isFromPool);
        }

        [NinoMember(0)]
        public int RpcId { get; set; }

        [NinoMember(1)]
        public int OwnerFiberId { get; set; }

        [NinoMember(2)]
        public string Address { get; set; }

        /// <summary>
        /// 账号
        /// </summary>
        [NinoMember(3)]
        public string Account { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        [NinoMember(4)]
        public string Password { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = 0;
            this.OwnerFiberId = 0;
            this.Address = null;
            this.Account = null;
            this.Password = null;

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(LoginOuter.NetClient2Main_Login)]
    public partial class NetClient2Main_Login : MessageObject, IResponse
    {
        public static NetClient2Main_Login Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<NetClient2Main_Login>(isFromPool);
        }

        [NinoMember(0)]
        public int RpcId { get; set; }

        [NinoMember(1)]
        public int Error { get; set; }

        [NinoMember(2)]
        public string Message { get; set; }

        [NinoMember(3)]
        public long PlayerId { get; set; }

        [NinoMember(4)]
        public string Token { get; set; }

        /// <summary>
        /// 用户信息
        /// </summary>
        [NinoMember(5)]
        public UserInfo UserInfo { get; set; }

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
            this.Token = null;
            this.UserInfo?.Dispose();
            this.UserInfo = null;

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(LoginOuter.C2G_Ping)]
    [ResponseType(nameof(G2C_Ping))]
    public partial class C2G_Ping : MessageObject, ISessionRequest
    {
        public static C2G_Ping Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2G_Ping>(isFromPool);
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
    [Message(LoginOuter.G2C_Ping)]
    public partial class G2C_Ping : MessageObject, ISessionResponse
    {
        public static G2C_Ping Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2C_Ping>(isFromPool);
        }

        [NinoMember(0)]
        public int RpcId { get; set; }

        [NinoMember(1)]
        public int Error { get; set; }

        [NinoMember(2)]
        public string Message { get; set; }

        [NinoMember(3)]
        public long Time { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = 0;
            this.Error = 0;
            this.Message = null;
            this.Time = 0;

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(LoginOuter.C2R_Login)]
    [ResponseType(nameof(R2C_Login))]
    public partial class C2R_Login : MessageObject, ISessionRequest
    {
        public static C2R_Login Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2R_Login>(isFromPool);
        }

        [NinoMember(0)]
        public int RpcId { get; set; }

        /// <summary>
        /// 帐号
        /// </summary>
        [NinoMember(1)]
        public string Account { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        [NinoMember(2)]
        public string Password { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = 0;
            this.Account = null;
            this.Password = null;

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(LoginOuter.R2C_Login)]
    public partial class R2C_Login : MessageObject, ISessionResponse
    {
        public static R2C_Login Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<R2C_Login>(isFromPool);
        }

        [NinoMember(0)]
        public int RpcId { get; set; }

        [NinoMember(1)]
        public int Error { get; set; }

        [NinoMember(2)]
        public string Message { get; set; }

        [NinoMember(3)]
        public string Address { get; set; }

        [NinoMember(4)]
        public long Key { get; set; }

        [NinoMember(5)]
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
            this.Address = null;
            this.Key = 0;
            this.GateId = 0;

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(LoginOuter.C2G_LoginGate)]
    [ResponseType(nameof(G2C_LoginGate))]
    public partial class C2G_LoginGate : MessageObject, ISessionRequest
    {
        public static C2G_LoginGate Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2G_LoginGate>(isFromPool);
        }

        [NinoMember(0)]
        public int RpcId { get; set; }

        /// <summary>
        /// 帐号
        /// </summary>
        [NinoMember(1)]
        public long Key { get; set; }

        [NinoMember(2)]
        public long GateId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = 0;
            this.Key = 0;
            this.GateId = 0;

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(LoginOuter.G2C_LoginGate)]
    public partial class G2C_LoginGate : MessageObject, ISessionResponse
    {
        public static G2C_LoginGate Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2C_LoginGate>(isFromPool);
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
    [Message(LoginOuter.C2R_LoginAccount)]
    [ResponseType(nameof(R2C_LoginAccount))]
    public partial class C2R_LoginAccount : MessageObject, ISessionRequest
    {
        public static C2R_LoginAccount Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2R_LoginAccount>(isFromPool);
        }

        [NinoMember(0)]
        public int RpcId { get; set; }

        /// <summary>
        /// 帐号
        /// </summary>
        [NinoMember(1)]
        public string Account { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        [NinoMember(2)]
        public string Password { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = 0;
            this.Account = null;
            this.Password = null;

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(LoginOuter.R2C_LoginAccount)]
    public partial class R2C_LoginAccount : MessageObject, ISessionResponse
    {
        public static R2C_LoginAccount Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<R2C_LoginAccount>(isFromPool);
        }

        [NinoMember(0)]
        public int RpcId { get; set; }

        [NinoMember(1)]
        public int Error { get; set; }

        [NinoMember(2)]
        public string Message { get; set; }

        [NinoMember(3)]
        public string Token { get; set; }

        /// <summary>
        /// 用户信息
        /// </summary>
        [NinoMember(4)]
        public UserInfo UserInfo { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = 0;
            this.Error = 0;
            this.Message = null;
            this.Token = null;
            this.UserInfo?.Dispose();
            this.UserInfo = null;

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(LoginOuter.A2C_Disconnect)]
    public partial class A2C_Disconnect : MessageObject, ISessionMessage
    {
        public static A2C_Disconnect Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<A2C_Disconnect>(isFromPool);
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
    [Message(LoginOuter.C2R_GetRealmKey)]
    [ResponseType(nameof(R2C_GetRealmKey))]
    public partial class C2R_GetRealmKey : MessageObject, ISessionRequest
    {
        public static C2R_GetRealmKey Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2R_GetRealmKey>(isFromPool);
        }

        [NinoMember(0)]
        public int RpcId { get; set; }

        [NinoMember(1)]
        public string Token { get; set; }

        [NinoMember(2)]
        public string Account { get; set; }

        [NinoMember(3)]
        public int ServerId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = 0;
            this.Token = null;
            this.Account = null;
            this.ServerId = 0;

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(LoginOuter.R2C_GetRealmKey)]
    public partial class R2C_GetRealmKey : MessageObject, ISessionResponse
    {
        public static R2C_GetRealmKey Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<R2C_GetRealmKey>(isFromPool);
        }

        [NinoMember(0)]
        public int RpcId { get; set; }

        [NinoMember(1)]
        public int Error { get; set; }

        [NinoMember(2)]
        public string Message { get; set; }

        [NinoMember(3)]
        public string Address { get; set; }

        [NinoMember(4)]
        public long Key { get; set; }

        [NinoMember(5)]
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
            this.Address = null;
            this.Key = 0;
            this.GateId = 0;

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(LoginOuter.C2G_LoginGameGate)]
    [ResponseType(nameof(G2C_LoginGameGate))]
    public partial class C2G_LoginGameGate : MessageObject, ISessionRequest
    {
        public static C2G_LoginGameGate Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2G_LoginGameGate>(isFromPool);
        }

        [NinoMember(0)]
        public int RpcId { get; set; }

        [NinoMember(1)]
        public long Key { get; set; }

        [NinoMember(2)]
        public string AccountName { get; set; }

        /// <summary>
        /// 用户ID（永久唯一标识）
        /// </summary>
        [NinoMember(3)]
        public long UserId { get; set; }

        /// <summary>
        /// 角色ID（当前选择的角色）
        /// </summary>
        [NinoMember(4)]
        public long RoleId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = 0;
            this.Key = 0;
            this.AccountName = null;
            this.UserId = 0;
            this.RoleId = 0;

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(LoginOuter.G2C_LoginGameGate)]
    public partial class G2C_LoginGameGate : MessageObject, ISessionResponse
    {
        public static G2C_LoginGameGate Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2C_LoginGameGate>(isFromPool);
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
    [Message(LoginOuter.C2G_EnterGame)]
    [ResponseType(nameof(G2C_EnterGame))]
    public partial class C2G_EnterGame : MessageObject, ISessionRequest
    {
        public static C2G_EnterGame Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2G_EnterGame>(isFromPool);
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
    [Message(LoginOuter.G2C_EnterGame)]
    public partial class G2C_EnterGame : MessageObject, ISessionResponse
    {
        public static G2C_EnterGame Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2C_EnterGame>(isFromPool);
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

    public static class LoginOuter
    {
        public const ushort UserInfo = 31906;
        public const ushort Main2NetClient_Login = 41960;
        public const ushort NetClient2Main_Login = 7172;
        public const ushort C2G_Ping = 58286;
        public const ushort G2C_Ping = 23994;
        public const ushort C2R_Login = 44012;
        public const ushort R2C_Login = 41840;
        public const ushort C2G_LoginGate = 35780;
        public const ushort G2C_LoginGate = 15628;
        public const ushort C2R_LoginAccount = 49719;
        public const ushort R2C_LoginAccount = 41035;
        public const ushort A2C_Disconnect = 19762;
        public const ushort C2R_GetRealmKey = 37663;
        public const ushort R2C_GetRealmKey = 62751;
        public const ushort C2G_LoginGameGate = 22110;
        public const ushort G2C_LoginGameGate = 35278;
        public const ushort C2G_EnterGame = 21958;
        public const ushort G2C_EnterGame = 45366;
    }
}