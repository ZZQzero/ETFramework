using System;

namespace ET
{
    public struct LateUpdateEvent
    {
    }
    
    public interface ILateUpdate: IClassEvent<LateUpdateEvent>
    {
    }

    [EntitySystem]
    public abstract class LateUpdateSystem<T> : SystemBase<T, AClassEventSystem<LateUpdateEvent>>, AClassEventSystem<LateUpdateEvent> where T: Entity, ILateUpdate
    {
        public void Run(Entity e, LateUpdateEvent t) => this.LateUpdate((T)e);
        protected abstract void LateUpdate(T self);
    }
}