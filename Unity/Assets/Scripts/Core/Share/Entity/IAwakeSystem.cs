using System;

namespace ET
{
    public interface IAwake
    {
    }

    public interface IAwake<A>
    {
    }
    
    public interface IAwake<A, B>
    {
    }
    
    public interface IAwake<A, B, C>
    {
    }
    
    public interface IAwake<A, B, C, D>
    {
    }
    
    /// <summary>
    /// IAwakeSystem标记接口，用于快速识别所有AwakeSystem变体
    /// </summary>
    public interface IAwakeSystemMarker
    {
    }
    
    public interface IAwakeSystem: ISystemType, IAwakeSystemMarker
    {
        void Run(Entity o);
    }
    
    public interface IAwakeSystem<A>: ISystemType, IAwakeSystemMarker
    {
        void Run(Entity o, A a);
    }
    
    public interface IAwakeSystem<A, B>: ISystemType, IAwakeSystemMarker
    {
        void Run(Entity o, A a, B b);
    }
    
    public interface IAwakeSystem<A, B, C>: ISystemType, IAwakeSystemMarker
    {
        void Run(Entity o, A a, B b, C c);
    }
    
    public interface IAwakeSystem<A, B, C, D>: ISystemType, IAwakeSystemMarker
    {
        void Run(Entity o, A a, B b, C c, D d);
    }

    [EntitySystem]
    public abstract class AwakeSystem<T> : SystemBase<T, IAwakeSystem>, IAwakeSystem where T: Entity, IAwake
    {
        void IAwakeSystem.Run(Entity o) => this.Awake((T)o);
        protected abstract void Awake(T self);
    }
    
    [EntitySystem]
    public abstract class AwakeSystem<T, A> : SystemBase<T, IAwakeSystem<A>>, IAwakeSystem<A> where T: Entity, IAwake<A>
    {
        void IAwakeSystem<A>.Run(Entity o, A a) => this.Awake((T)o, a);
        protected abstract void Awake(T self, A a);
    }

    [EntitySystem]
    public abstract class AwakeSystem<T, A, B> : SystemBase<T, IAwakeSystem<A, B>>, IAwakeSystem<A, B> where T: Entity, IAwake<A, B>
    {
        void IAwakeSystem<A, B>.Run(Entity o, A a, B b) => this.Awake((T)o, a, b);
        protected abstract void Awake(T self, A a, B b);
    }

    [EntitySystem]
    public abstract class AwakeSystem<T, A, B, C> : SystemBase<T, IAwakeSystem<A, B, C>>, IAwakeSystem<A, B, C> where T: Entity, IAwake<A, B, C>
    {
        void IAwakeSystem<A, B, C>.Run(Entity o, A a, B b, C c) => this.Awake((T)o, a, b, c);
        protected abstract void Awake(T self, A a, B b, C c);
    }
    
    [EntitySystem]
    public abstract class AwakeSystem<T, A, B, C, D> : SystemBase<T, IAwakeSystem<A, B, C, D>>, IAwakeSystem<A, B, C, D> where T: Entity, IAwake<A, B, C, D>
    {
        void IAwakeSystem<A, B, C, D>.Run(Entity o, A a, B b, C c, D d) => this.Awake((T)o, a, b, c, d);
        protected abstract void Awake(T self, A a, B b, C c, D d);
    }
}