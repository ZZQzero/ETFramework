using Nino.Core;
using System.Collections.Generic;

namespace ET
{
    [NinoType(false)]
    [Message(StateSyncOuter.RouterSync)]
    public partial class RouterSync : MessageObject
    {
        public static RouterSync Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<RouterSync>(isFromPool);
        }

        [NinoMember(0)]
        public uint ConnectId { get; set; }

        [NinoMember(1)]
        public string Address { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.ConnectId = 0;
            this.Address = null;

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(StateSyncOuter.C2M_TestRequest)]
    [ResponseType(nameof(M2C_TestResponse))]
    public partial class C2M_TestRequest : MessageObject, ILocationRequest
    {
        public static C2M_TestRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_TestRequest>(isFromPool);
        }

        [NinoMember(0)]
        public int RpcId { get; set; }

        [NinoMember(1)]
        public string request { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = 0;
            this.request = null;

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(StateSyncOuter.M2C_TestResponse)]
    public partial class M2C_TestResponse : MessageObject, IResponse
    {
        public static M2C_TestResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_TestResponse>(isFromPool);
        }

        [NinoMember(0)]
        public int RpcId { get; set; }

        [NinoMember(1)]
        public int Error { get; set; }

        [NinoMember(2)]
        public string Message { get; set; }

        [NinoMember(3)]
        public string response { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = 0;
            this.Error = 0;
            this.Message = null;
            this.response = null;

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(StateSyncOuter.C2G_EnterMap)]
    [ResponseType(nameof(G2C_EnterMap))]
    public partial class C2G_EnterMap : MessageObject, ISessionRequest
    {
        public static C2G_EnterMap Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2G_EnterMap>(isFromPool);
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
    [Message(StateSyncOuter.G2C_EnterMap)]
    public partial class G2C_EnterMap : MessageObject, ISessionResponse
    {
        public static G2C_EnterMap Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2C_EnterMap>(isFromPool);
        }

        [NinoMember(0)]
        public int RpcId { get; set; }

        [NinoMember(1)]
        public int Error { get; set; }

        [NinoMember(2)]
        public string Message { get; set; }

        /// <summary>
        /// 自己的UnitId
        /// </summary>
        [NinoMember(3)]
        public long MyId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = 0;
            this.Error = 0;
            this.Message = null;
            this.MyId = 0;

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(StateSyncOuter.MoveInfo)]
    public partial class MoveInfo : MessageObject
    {
        public static MoveInfo Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<MoveInfo>(isFromPool);
        }

        [NinoMember(0)]
        public List<Unity.Mathematics.float3> Points { get; set; } = new();

        [NinoMember(1)]
        public Unity.Mathematics.quaternion Rotation { get; set; }

        [NinoMember(2)]
        public int TurnSpeed { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Points.Clear();
            this.Rotation = default;
            this.TurnSpeed = 0;

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(StateSyncOuter.UnitInfo)]
    public partial class UnitInfo : MessageObject
    {
        public static UnitInfo Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<UnitInfo>(isFromPool);
        }

        [NinoMember(0)]
        public long UnitId { get; set; }

        [NinoMember(1)]
        public int ConfigId { get; set; }

        [NinoMember(2)]
        public int Type { get; set; }

        [NinoMember(3)]
        public Unity.Mathematics.float3 Position { get; set; }

        [NinoMember(4)]
        public Unity.Mathematics.float3 Forward { get; set; }

        #if DOTNET
        [MongoDB.Bson.Serialization.Attributes.BsonDictionaryOptions(MongoDB.Bson.Serialization.Options.DictionaryRepresentation.ArrayOfArrays)]
        #endif
        [NinoMember(5)]
        public Dictionary<int, long> KV { get; set; } = new();
        [NinoMember(6)]
        public MoveInfo MoveInfo { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.UnitId = 0;
            this.ConfigId = 0;
            this.Type = 0;
            this.Position = default;
            this.Forward = default;
            this.KV.Clear();
            this.MoveInfo?.Dispose();
            this.MoveInfo = null;

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(StateSyncOuter.M2C_CreateUnits)]
    public partial class M2C_CreateUnits : MessageObject, IMessage
    {
        public static M2C_CreateUnits Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_CreateUnits>(isFromPool);
        }

        [NinoMember(0)]
        public List<UnitInfo> Units { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Units.Clear();

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(StateSyncOuter.M2C_CreateMyUnit)]
    public partial class M2C_CreateMyUnit : MessageObject, IMessage
    {
        public static M2C_CreateMyUnit Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_CreateMyUnit>(isFromPool);
        }

        [NinoMember(0)]
        public UnitInfo Unit { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Unit?.Dispose();
            this.Unit = null;

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(StateSyncOuter.M2C_StartSceneChange)]
    public partial class M2C_StartSceneChange : MessageObject, IMessage
    {
        public static M2C_StartSceneChange Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_StartSceneChange>(isFromPool);
        }

        [NinoMember(0)]
        public long SceneInstanceId { get; set; }

        [NinoMember(1)]
        public string SceneName { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.SceneInstanceId = 0;
            this.SceneName = null;

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(StateSyncOuter.M2C_RemoveUnits)]
    public partial class M2C_RemoveUnits : MessageObject, IMessage
    {
        public static M2C_RemoveUnits Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_RemoveUnits>(isFromPool);
        }

        [NinoMember(0)]
        public List<long> Units { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Units.Clear();

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(StateSyncOuter.C2M_PathfindingResult)]
    public partial class C2M_PathfindingResult : MessageObject, ILocationMessage
    {
        public static C2M_PathfindingResult Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_PathfindingResult>(isFromPool);
        }

        [NinoMember(0)]
        public int RpcId { get; set; }

        [NinoMember(1)]
        public Unity.Mathematics.float3 Position { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = 0;
            this.Position = default;

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(StateSyncOuter.C2M_Stop)]
    public partial class C2M_Stop : MessageObject, ILocationMessage
    {
        public static C2M_Stop Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_Stop>(isFromPool);
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
    [Message(StateSyncOuter.M2C_PathfindingResult)]
    public partial class M2C_PathfindingResult : MessageObject, IMessage
    {
        public static M2C_PathfindingResult Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_PathfindingResult>(isFromPool);
        }

        [NinoMember(0)]
        public long Id { get; set; }

        [NinoMember(1)]
        public Unity.Mathematics.float3 Position { get; set; }

        [NinoMember(2)]
        public List<Unity.Mathematics.float3> Points { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Id = 0;
            this.Position = default;
            this.Points.Clear();

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(StateSyncOuter.M2C_Stop)]
    public partial class M2C_Stop : MessageObject, IMessage
    {
        public static M2C_Stop Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_Stop>(isFromPool);
        }

        [NinoMember(0)]
        public int Error { get; set; }

        [NinoMember(1)]
        public long Id { get; set; }

        [NinoMember(2)]
        public Unity.Mathematics.float3 Position { get; set; }

        [NinoMember(3)]
        public Unity.Mathematics.quaternion Rotation { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Error = 0;
            this.Id = 0;
            this.Position = default;
            this.Rotation = default;

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(StateSyncOuter.G2C_Test)]
    public partial class G2C_Test : MessageObject, ISessionMessage
    {
        public static G2C_Test Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2C_Test>(isFromPool);
        }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            
            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(StateSyncOuter.C2M_Reload)]
    [ResponseType(nameof(M2C_Reload))]
    public partial class C2M_Reload : MessageObject, ISessionRequest
    {
        public static C2M_Reload Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_Reload>(isFromPool);
        }

        [NinoMember(0)]
        public int RpcId { get; set; }

        [NinoMember(1)]
        public string Account { get; set; }

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
    [Message(StateSyncOuter.M2C_Reload)]
    public partial class M2C_Reload : MessageObject, ISessionResponse
    {
        public static M2C_Reload Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_Reload>(isFromPool);
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
    [Message(StateSyncOuter.G2C_TestHotfixMessage)]
    public partial class G2C_TestHotfixMessage : MessageObject, ISessionMessage
    {
        public static G2C_TestHotfixMessage Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2C_TestHotfixMessage>(isFromPool);
        }

        [NinoMember(0)]
        public string Info { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Info = null;

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(StateSyncOuter.C2M_TestRobotCase)]
    [ResponseType(nameof(M2C_TestRobotCase))]
    public partial class C2M_TestRobotCase : MessageObject, ILocationRequest
    {
        public static C2M_TestRobotCase Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_TestRobotCase>(isFromPool);
        }

        [NinoMember(0)]
        public int RpcId { get; set; }

        [NinoMember(1)]
        public int N { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = 0;
            this.N = 0;

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(StateSyncOuter.M2C_TestRobotCase)]
    public partial class M2C_TestRobotCase : MessageObject, ILocationResponse
    {
        public static M2C_TestRobotCase Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_TestRobotCase>(isFromPool);
        }

        [NinoMember(0)]
        public int RpcId { get; set; }

        [NinoMember(1)]
        public int Error { get; set; }

        [NinoMember(2)]
        public string Message { get; set; }

        [NinoMember(3)]
        public int N { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = 0;
            this.Error = 0;
            this.Message = null;
            this.N = 0;

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(StateSyncOuter.C2M_TestRobotCase2)]
    public partial class C2M_TestRobotCase2 : MessageObject, ILocationMessage
    {
        public static C2M_TestRobotCase2 Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_TestRobotCase2>(isFromPool);
        }

        [NinoMember(0)]
        public int RpcId { get; set; }

        [NinoMember(1)]
        public int N { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = 0;
            this.N = 0;

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(StateSyncOuter.M2C_TestRobotCase2)]
    public partial class M2C_TestRobotCase2 : MessageObject, ILocationMessage
    {
        public static M2C_TestRobotCase2 Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_TestRobotCase2>(isFromPool);
        }

        [NinoMember(0)]
        public int RpcId { get; set; }

        [NinoMember(1)]
        public int N { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = 0;
            this.N = 0;

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(StateSyncOuter.C2M_TransferMap)]
    [ResponseType(nameof(M2C_TransferMap))]
    public partial class C2M_TransferMap : MessageObject, ILocationRequest
    {
        public static C2M_TransferMap Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_TransferMap>(isFromPool);
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
    [Message(StateSyncOuter.M2C_TransferMap)]
    public partial class M2C_TransferMap : MessageObject, ILocationResponse
    {
        public static M2C_TransferMap Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_TransferMap>(isFromPool);
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
    [Message(StateSyncOuter.C2G_Benchmark)]
    [ResponseType(nameof(G2C_Benchmark))]
    public partial class C2G_Benchmark : MessageObject, ISessionRequest
    {
        public static C2G_Benchmark Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2G_Benchmark>(isFromPool);
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
    [Message(StateSyncOuter.G2C_Benchmark)]
    public partial class G2C_Benchmark : MessageObject, ISessionResponse
    {
        public static G2C_Benchmark Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2C_Benchmark>(isFromPool);
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

    public static class StateSyncOuter
    {
        public const ushort RouterSync = 10002;
        public const ushort C2M_TestRequest = 10003;
        public const ushort M2C_TestResponse = 10004;
        public const ushort C2G_EnterMap = 10005;
        public const ushort G2C_EnterMap = 10006;
        public const ushort MoveInfo = 10007;
        public const ushort UnitInfo = 10008;
        public const ushort M2C_CreateUnits = 10009;
        public const ushort M2C_CreateMyUnit = 10010;
        public const ushort M2C_StartSceneChange = 10011;
        public const ushort M2C_RemoveUnits = 10012;
        public const ushort C2M_PathfindingResult = 10013;
        public const ushort C2M_Stop = 10014;
        public const ushort M2C_PathfindingResult = 10015;
        public const ushort M2C_Stop = 10016;
        public const ushort G2C_Test = 10017;
        public const ushort C2M_Reload = 10018;
        public const ushort M2C_Reload = 10019;
        public const ushort G2C_TestHotfixMessage = 10020;
        public const ushort C2M_TestRobotCase = 10021;
        public const ushort M2C_TestRobotCase = 10022;
        public const ushort C2M_TestRobotCase2 = 10023;
        public const ushort M2C_TestRobotCase2 = 10024;
        public const ushort C2M_TransferMap = 10025;
        public const ushort M2C_TransferMap = 10026;
        public const ushort C2G_Benchmark = 10027;
        public const ushort G2C_Benchmark = 10028;
    }
}