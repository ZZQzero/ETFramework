using System;

namespace ET
{
    public struct UpdateEvent
    {
    }
    
    public interface IUpdate: IClassEvent<UpdateEvent>
    {
    }

    [EntitySystem]
    public abstract class UpdateSystem<T> : SystemBase<T, AClassEventSystem<UpdateEvent>>, AClassEventSystem<UpdateEvent> where T: Entity, IUpdate
    {
        public void Run(Entity e, UpdateEvent t) => this.Update((T)e);
        protected abstract void Update(T self);
    }
}