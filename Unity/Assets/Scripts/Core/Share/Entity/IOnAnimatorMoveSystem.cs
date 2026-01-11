

namespace ET
{
    public struct OnAnimatorMoveEvent
    {
    }
    
    public interface IOnAnimatorMove: IClassEvent<OnAnimatorMoveEvent>
    {
    }

    [EntitySystem]
    public abstract class OnAnimatorMoveSystem<T> : SystemBase<T, AClassEventSystem<OnAnimatorMoveEvent>>, AClassEventSystem<OnAnimatorMoveEvent> where T: Entity, IOnAnimatorMove
    {
        public void Run(Entity e, OnAnimatorMoveEvent t) => this.OnAnimatorMove((T)e);
        protected abstract void OnAnimatorMove(T self);
    }
}