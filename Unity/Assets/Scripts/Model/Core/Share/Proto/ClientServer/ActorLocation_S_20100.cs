using Nino.Core;
using System.Collections.Generic;

namespace ET
{
    [NinoType(false)]
    [Message(ActorLocation.ObjectAddRequest)]
    [ResponseType(nameof(ObjectAddResponse))]
    public partial class ObjectAddRequest : MessageObject, IRequest
    {
        public static ObjectAddRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<ObjectAddRequest>(isFromPool);
        }

        [NinoMember(0)]
        public int RpcId { get; set; }

        [NinoMember(1)]
        public int Type { get; set; }

        [NinoMember(2)]
        public long Key { get; set; }

        [NinoMember(3)]
        public ActorId ActorId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Type = default;
            this.Key = default;
            this.ActorId = default;

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(ActorLocation.ObjectAddResponse)]
    public partial class ObjectAddResponse : MessageObject, IResponse
    {
        public static ObjectAddResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<ObjectAddResponse>(isFromPool);
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
    [Message(ActorLocation.ObjectLockRequest)]
    [ResponseType(nameof(ObjectLockResponse))]
    public partial class ObjectLockRequest : MessageObject, IRequest
    {
        public static ObjectLockRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<ObjectLockRequest>(isFromPool);
        }

        [NinoMember(0)]
        public int RpcId { get; set; }

        [NinoMember(1)]
        public int Type { get; set; }

        [NinoMember(2)]
        public long Key { get; set; }

        [NinoMember(3)]
        public ActorId ActorId { get; set; }

        [NinoMember(4)]
        public int Time { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Type = default;
            this.Key = default;
            this.ActorId = default;
            this.Time = default;

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(ActorLocation.ObjectLockResponse)]
    public partial class ObjectLockResponse : MessageObject, IResponse
    {
        public static ObjectLockResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<ObjectLockResponse>(isFromPool);
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
    [Message(ActorLocation.ObjectUnLockRequest)]
    [ResponseType(nameof(ObjectUnLockResponse))]
    public partial class ObjectUnLockRequest : MessageObject, IRequest
    {
        public static ObjectUnLockRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<ObjectUnLockRequest>(isFromPool);
        }

        [NinoMember(0)]
        public int RpcId { get; set; }

        [NinoMember(1)]
        public int Type { get; set; }

        [NinoMember(2)]
        public long Key { get; set; }

        [NinoMember(3)]
        public ActorId OldActorId { get; set; }

        [NinoMember(4)]
        public ActorId NewActorId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Type = default;
            this.Key = default;
            this.OldActorId = default;
            this.NewActorId = default;

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(ActorLocation.ObjectUnLockResponse)]
    public partial class ObjectUnLockResponse : MessageObject, IResponse
    {
        public static ObjectUnLockResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<ObjectUnLockResponse>(isFromPool);
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
    [Message(ActorLocation.ObjectRemoveRequest)]
    [ResponseType(nameof(ObjectRemoveResponse))]
    public partial class ObjectRemoveRequest : MessageObject, IRequest
    {
        public static ObjectRemoveRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<ObjectRemoveRequest>(isFromPool);
        }

        [NinoMember(0)]
        public int RpcId { get; set; }

        [NinoMember(1)]
        public int Type { get; set; }

        [NinoMember(2)]
        public long Key { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Type = default;
            this.Key = default;

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(ActorLocation.ObjectRemoveResponse)]
    public partial class ObjectRemoveResponse : MessageObject, IResponse
    {
        public static ObjectRemoveResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<ObjectRemoveResponse>(isFromPool);
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
    [Message(ActorLocation.ObjectGetRequest)]
    [ResponseType(nameof(ObjectGetResponse))]
    public partial class ObjectGetRequest : MessageObject, IRequest
    {
        public static ObjectGetRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<ObjectGetRequest>(isFromPool);
        }

        [NinoMember(0)]
        public int RpcId { get; set; }

        [NinoMember(1)]
        public int Type { get; set; }

        [NinoMember(2)]
        public long Key { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Type = default;
            this.Key = default;

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(ActorLocation.ObjectGetResponse)]
    public partial class ObjectGetResponse : MessageObject, IResponse
    {
        public static ObjectGetResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<ObjectGetResponse>(isFromPool);
        }

        [NinoMember(0)]
        public int RpcId { get; set; }

        [NinoMember(1)]
        public int Error { get; set; }

        [NinoMember(2)]
        public string Message { get; set; }

        [NinoMember(3)]
        public int Type { get; set; }

        [NinoMember(4)]
        public ActorId ActorId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.Type = default;
            this.ActorId = default;

            ObjectPool.Recycle(this);
        }
    }

    // 批量Location注册项
    [NinoType(false)]
    [Message(ActorLocation.ObjectAddBatchItem)]
    public partial class ObjectAddBatchItem : MessageObject
    {
        public static ObjectAddBatchItem Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<ObjectAddBatchItem>(isFromPool);
        }

        [NinoMember(0)]
        public int Type { get; set; }

        [NinoMember(1)]
        public long Key { get; set; }

        [NinoMember(2)]
        public ActorId ActorId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Type = default;
            this.Key = default;
            this.ActorId = default;

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(ActorLocation.ObjectAddBatchRequest)]
    [ResponseType(nameof(ObjectAddBatchResponse))]
    public partial class ObjectAddBatchRequest : MessageObject, IRequest
    {
        public static ObjectAddBatchRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<ObjectAddBatchRequest>(isFromPool);
        }

        [NinoMember(0)]
        public int RpcId { get; set; }

        [NinoMember(1)]
        public List<ObjectAddBatchItem> Items { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Items.Clear();

            ObjectPool.Recycle(this);
        }
    }

    [NinoType(false)]
    [Message(ActorLocation.ObjectAddBatchResponse)]
    public partial class ObjectAddBatchResponse : MessageObject, IResponse
    {
        public static ObjectAddBatchResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<ObjectAddBatchResponse>(isFromPool);
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

    public static class ActorLocation
    {
        public const ushort ObjectAddRequest = 20101;
        public const ushort ObjectAddResponse = 20102;
        public const ushort ObjectLockRequest = 20103;
        public const ushort ObjectLockResponse = 20104;
        public const ushort ObjectUnLockRequest = 20105;
        public const ushort ObjectUnLockResponse = 20106;
        public const ushort ObjectRemoveRequest = 20107;
        public const ushort ObjectRemoveResponse = 20108;
        public const ushort ObjectGetRequest = 20109;
        public const ushort ObjectGetResponse = 20110;
        public const ushort ObjectAddBatchItem = 20111;
        public const ushort ObjectAddBatchRequest = 20112;
        public const ushort ObjectAddBatchResponse = 20113;
    }
}