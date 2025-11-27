using System.Collections.Generic;

namespace ET
{
    [ChildOf(typeof(LocationOneType))]
    public class LockInfo: Entity, IAwake<ActorId, CoroutineLock>, IDestroy
    {
        public ActorId LockActorId;

        public CoroutineLock CoroutineLock
        {
            get;
            set;
        }
    }

    [ChildOf(typeof(LocationManagerComponent))]
    public class LocationOneType: Entity, IAwake
    {
        public readonly Dictionary<long, ActorId> locations = new();

        public readonly Dictionary<long, EntityRef<LockInfo>> lockInfos = new();
    }

    [ComponentOf(typeof(Scene))]
    public class LocationManagerComponent: Entity, IAwake
    {
    }
}