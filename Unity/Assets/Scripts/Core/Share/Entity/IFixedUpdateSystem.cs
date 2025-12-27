using System;

namespace ET
{
    public struct FixedUpdateEvent
    {
    }
    
    public interface IFixedUpdate: IClassEvent<FixedUpdateEvent>
    {
    }

    [EntitySystem]
    public abstract class FixedUpdateSystem<T> : SystemBase<T, AClassEventSystem<FixedUpdateEvent>>, AClassEventSystem<FixedUpdateEvent> where T: Entity, IFixedUpdate
    {
        public void Run(Entity e, FixedUpdateEvent t) => this.FixedUpdate((T)e);
        protected abstract void FixedUpdate(T self);
    }
}