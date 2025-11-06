using System;

namespace ET
{
    public interface IDestroy
    {
    }
    
    /// <summary>
    /// IDestroySystem标记接口，用于快速识别DestroySystem
    /// </summary>
    public interface IDestroySystemMarker
    {
    }
    
    public interface IDestroySystem: ISystemType, IDestroySystemMarker
    {
        void Run(Entity o);
    }

    [EntitySystem]
    public abstract class DestroySystem<T> : SystemBase<T, IDestroySystem>, IDestroySystem where T: Entity, IDestroy
    {
        void IDestroySystem.Run(Entity o) => this.Destroy((T)o);
        protected abstract void Destroy(T self);
    }
}