using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ET
{
    [Flags]
    public enum EntityStatus: byte
    {
        None = 0,
        IsFromPool = 1,
        IsRegister = 1 << 1,
        IsComponent = 1 << 2,
        IsNew = 1 << 3,
    }
    public abstract partial class Entity : IPool
    {
        #region 基础属性
        //实例化Id
        public long InstanceId { get; protected set; }
        //EntityId
        public long Id { get; set; }
        //类型Id (T)即使销毁也不变
        public long TypeId { get; set; }
        public bool IsDisposed => this.InstanceId == 0;
        
        private EntityStatus status = EntityStatus.None;
        private Entity parent;
        private IScene iScene;
        private SortedDictionary<long, Entity> children;
        private SortedDictionary<long, Entity> components;
        #endregion
        
        #region 构造函数
        protected Entity()
        {
        }
        #endregion

        #region 状态管理
        public bool IsFromPool
        {
            get => HasStatusFlag(EntityStatus.IsFromPool);
            set => SetStatusFlag(EntityStatus.IsFromPool, value);
        }

        protected bool IsRegister
        {
            get => HasStatusFlag(EntityStatus.IsRegister);
            set
            {
                if (this.IsRegister == value) return;
                
                SetStatusFlag(EntityStatus.IsRegister, value);
                
                if (value)
                {
                    this.RegisterSystem();
                }
            }
        }

        private bool IsComponent
        {
            get => HasStatusFlag(EntityStatus.IsComponent);
            set => SetStatusFlag(EntityStatus.IsComponent, value);
        }

        public bool IsNew
        {
            get => HasStatusFlag(EntityStatus.IsNew);
            set => SetStatusFlag(EntityStatus.IsNew, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HasStatusFlag(EntityStatus flag)
        {
            return (this.status & flag) == flag;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetStatusFlag(EntityStatus flag, bool value)
        {
            if (value)
                this.status |= flag;
            else
                this.status &= ~flag;
        }
        #endregion
        
        #region 系统注册
        protected virtual void RegisterSystem()
        {
            this.iScene.Fiber.EntitySystem.RegisterSystem(this);
        }
        #endregion

        #region 父级管理
        public Entity Parent
        {
            get => this.parent;
            private set
            {
                this.ValidateParent(value);
                this.SetParentInternal(value, false);
            }
        }

        private Entity ComponentParent
        {
            set
            {
                this.ValidateParent(value);
                this.SetParentInternal(value, true);
            }
        }

        private void ValidateParent(Entity value)
        {
            if (value == null)
                throw new Exception($"Cannot set parent to null: {this.GetType().FullName}");
            
            if (value == this)
                throw new Exception($"Cannot set parent to self: {this.GetType().FullName}");
            
            if (value.IScene == null)
                throw new Exception($"Cannot set parent because parent IScene is null: {this.GetType().FullName} {value.GetType().FullName}");
        }

        private void SetParentInternal(Entity value, bool isComponent)
        {
            if (this.parent == value)
            {
                Log.Error($"Duplicate parent assignment: {this.GetType().FullName} parent: {this.parent.GetType().FullName}");
                return;
            }

            // 移除旧的父子关系
            if (this.parent != null)
            {
                if (this.IsComponent)
                {
                    this.parent.RemoveComponent(TypeId);
                }
                else
                {
                    this.parent.RemoveChild(this);
                }
            }

            
            // 设置新的父子关系
            this.parent = value;
            this.IsComponent = isComponent;
            
            if (isComponent)
                this.parent.AddToComponents(this);
            else
                this.parent.AddToChildren(this);

            // 设置场景引用,如果组件本身继承了IScene,它本身就是场景（有点问题，又是组件，又是场景）
            if (this is IScene scene)
            {
                scene.Fiber = this.parent.iScene.Fiber;
                this.IScene = scene;
            }
            else
            {
                this.IScene = this.parent.iScene;
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetParent<T>() where T : Entity
        {
            return this.Parent as T;
        }
        #endregion

        #region 场景管理
        public IScene IScene
        {
            get => this.iScene;
            protected set
            {
                if (value == null)
                    throw new Exception($"IScene cannot be set to null: {this.GetType().FullName}");

                if (this.iScene == value) return;

                this.iScene = value;
                
                if (this.InstanceId == 0)
                {
                    this.InstanceId = GenerateIdManager.Instance.GenerateInstanceId();
                }

                this.IsRegister = true;
            }
        }
        
        #endregion

        #region 子级管理
        public SortedDictionary<long,Entity> Children => this.children ??= new SortedDictionary<long, Entity>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ChildrenCount() => this.children?.Count ?? 0;

        private void AddToChildren(Entity entity)
        {
            if (!Children.TryAdd(entity.Id, entity))
            {
                Log.Error($"已经存在相同的key {entity.GetType()}");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public K GetChild<K>(long id) where K : Entity
        {
            return this.children?.TryGetValue(id, out Entity child) == true ? child as K : null;
        }

        public void RemoveChild(long id)
        {
            if (this.children?.Remove(id, out Entity child) == true)
            {
                EntityObjectPool.Instance.RecycleEntity(child);
            }
        }
        
        private void RemoveChild(Entity entity)
        {
            if (this.children?.Remove(entity.Id) == true)
            {
                EntityObjectPool.Instance.RecycleEntity(entity);
            }
        }
        #endregion

        #region 组件管理
        public SortedDictionary<long,Entity> Components => this.components ??= new SortedDictionary<long, Entity>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ComponentsCount() => this.components?.Count ?? 0;

        private void AddToComponents(Entity component)
        {
            Components.Add(component.TypeId,component);
        }
        
        public K GetComponent<K>() where K : Entity
        {
            if (this.components == null) return null;
            return this.components.TryGetValue(TypeId<K>.Id, out Entity component) ? (K)component : null;
        }

        public void RemoveComponent<K>() where K : Entity, new()
        {
            if (this.IsDisposed || this.components == null) return;
            
            if (components.Remove(TypeId<K>.Id,out Entity component))
            {
                EntityObjectPool.Instance.RecycleEntity(component);
            }
        }
        
        private void RemoveComponent(long typeId)
        {
            if (this.components.Remove(typeId,out var entity))
            {
                EntityObjectPool.Instance.RecycleEntity(entity);
            }
        }
        
        #endregion

        #region 组件创建和添加

        public K AddComponentWithId<K>(long id, bool isFromPool = false) where K : Entity, IAwake, new()
        {
            return this.CreateAndAddComponent<K>(id, isFromPool, component => EntitySystemSingleton.Instance.Awake(component));
        }

        public K AddComponentWithId<K, P1>(long id, P1 p1, bool isFromPool = false) where K : Entity, IAwake<P1>, new()
        {
            return this.CreateAndAddComponent<K, P1>(id, isFromPool, (component, param1) => EntitySystemSingleton.Instance.Awake(component, param1), p1);
        }

        public K AddComponentWithId<K, P1, P2>(long id, P1 p1, P2 p2, bool isFromPool = false) where K : Entity, IAwake<P1, P2>, new()
        {
            return this.CreateAndAddComponent<K, P1, P2>(id, isFromPool, (component, param1, param2) => EntitySystemSingleton.Instance.Awake(component, param1, param2), p1, p2);
        }

        public K AddComponentWithId<K, P1, P2, P3>(long id, P1 p1, P2 p2, P3 p3, bool isFromPool = false) where K : Entity, IAwake<P1, P2, P3>, new()
        {
            return this.CreateAndAddComponent<K, P1, P2, P3>(id, isFromPool, (component, param1, param2, param3) => EntitySystemSingleton.Instance.Awake(component, param1, param2, param3), p1, p2, p3);
        }

        private K CreateAndAddComponent<K>(long id, bool isFromPool, Action<K> awakeAction) where K : Entity, new()
        {
            this.ValidateComponentNotExists<K>();
            K component = EntityObjectPool.Instance.GetEntity<K>(TypeId<K>.Id,isFromPool);
            component.Id = id;
            component.ComponentParent = this;
            awakeAction(component);
            return component;
        }

        private K CreateAndAddComponent<K, P1>(long id, bool isFromPool, Action<K, P1> awakeAction, P1 p1) where K : Entity, new()
        {
            this.ValidateComponentNotExists<K>();
            K component = EntityObjectPool.Instance.GetEntity<K>(TypeId<K>.Id,isFromPool);
            component.Id = id;
            component.ComponentParent = this;
            awakeAction(component, p1);
            return component;
        }

        private K CreateAndAddComponent<K, P1, P2>(long id, bool isFromPool, Action<K, P1, P2> awakeAction, P1 p1, P2 p2) where K : Entity, new()
        {
            this.ValidateComponentNotExists<K>();
            K component = EntityObjectPool.Instance.GetEntity<K>(TypeId<K>.Id,isFromPool);
            component.Id = id;
            component.ComponentParent = this;
            awakeAction(component, p1, p2);
            return component;
        }

        private K CreateAndAddComponent<K, P1, P2, P3>(long id, bool isFromPool, Action<K, P1, P2, P3> awakeAction, P1 p1, P2 p2, P3 p3) where K : Entity, new()
        {
            this.ValidateComponentNotExists<K>();
            K component = EntityObjectPool.Instance.GetEntity<K>(TypeId<K>.Id,isFromPool);
            component.Id = id;
            component.ComponentParent = this;
            awakeAction(component, p1, p2, p3);
            return component;
        }

        private void ValidateComponentNotExists<K>() where K : Entity
        {
            if (this.components?.ContainsKey(TypeId<K>.Id) == true)
            {
                throw new Exception($"Entity already has component: {typeof(K).FullName}");
            }
        }

        // 便捷方法
        public K AddComponent<K>(bool isFromPool = false) where K : Entity, IAwake, new()
        {
            return this.AddComponentWithId<K>(this.Id, isFromPool);
        }

        public K AddComponent<K, P1>(P1 p1, bool isFromPool = false) where K : Entity, IAwake<P1>, new()
        {
            return this.AddComponentWithId<K, P1>(this.Id, p1, isFromPool);
        }

        public K AddComponent<K, P1, P2>(P1 p1, P2 p2, bool isFromPool = false) where K : Entity, IAwake<P1, P2>, new()
        {
            return this.AddComponentWithId<K, P1, P2>(this.Id, p1, p2, isFromPool);
        }

        public K AddComponent<K, P1, P2, P3>(P1 p1, P2 p2, P3 p3, bool isFromPool = false) where K : Entity, IAwake<P1, P2, P3>, new()
        {
            return this.AddComponentWithId<K, P1, P2, P3>(this.Id, p1, p2, p3, isFromPool);
        }
        #endregion

        #region 子级创建和添加
        public Entity AddChild(Entity entity)
        {
            entity.Parent = this;
            return entity;
        }

        public T AddChild<T>(bool isFromPool = false) where T : Entity, IAwake, new()
        {
            return this.CreateAndAddChild<T>(isFromPool, GenerateIdManager.Instance.GenerateId(), 
                component => EntitySystemSingleton.Instance.Awake(component));
        }

        public T AddChild<T, A>(A a, bool isFromPool = false) where T : Entity, IAwake<A>, new()
        {
            return this.CreateAndAddChild<T, A>(isFromPool, GenerateIdManager.Instance.GenerateId(), 
                (component, param) => EntitySystemSingleton.Instance.Awake(component, param), a);
        }

        public T AddChild<T, A, B>(A a, B b, bool isFromPool = false) where T : Entity, IAwake<A, B>, new()
        {
            return this.CreateAndAddChild<T, A, B>(isFromPool, GenerateIdManager.Instance.GenerateId(), 
                (component, param1, param2) => EntitySystemSingleton.Instance.Awake(component, param1, param2), a, b);
        }

        public T AddChild<T, A, B, C>(A a, B b, C c, bool isFromPool = false) where T : Entity, IAwake<A, B, C>, new()
        {
            return this.CreateAndAddChild<T, A, B, C>(isFromPool, GenerateIdManager.Instance.GenerateId(), 
                (component, param1, param2, param3) => EntitySystemSingleton.Instance.Awake(component, param1, param2, param3), a, b, c);
        }

        public T AddChildWithId<T>(long id, bool isFromPool = false) where T : Entity, IAwake, new()
        {
            return this.CreateAndAddChild<T>(isFromPool, id, component => EntitySystemSingleton.Instance.Awake(component));
        }

        public T AddChildWithId<T, A>(long id, A a, bool isFromPool = false) where T : Entity, IAwake<A>, new()
        {
            return this.CreateAndAddChild<T, A>(isFromPool, id, (component, param) => EntitySystemSingleton.Instance.Awake(component, param), a);
        }

        public T AddChildWithId<T, A, B>(long id, A a, B b, bool isFromPool = false) where T : Entity, IAwake<A, B>, new()
        {
            return this.CreateAndAddChild<T, A, B>(isFromPool, id, (component, param1, param2) => EntitySystemSingleton.Instance.Awake(component, param1, param2), a, b);
        }

        public T AddChildWithId<T, A, B, C>(long id, A a, B b, C c, bool isFromPool = false) where T : Entity, IAwake<A, B, C>, new()
        {
            return this.CreateAndAddChild<T, A, B, C>(isFromPool, id, (component, param1, param2, param3) => EntitySystemSingleton.Instance.Awake(component, param1, param2, param3), a, b, c);
        }

        private T CreateAndAddChild<T>(bool isFromPool, long id, Action<T> awakeAction) where T : Entity, new()
        {
            T component = EntityObjectPool.Instance.GetEntity<T>(TypeId<T>.Id,isFromPool);
            component.Id = id;
            component.Parent = this;
            awakeAction(component);
            return component;
        }

        private T CreateAndAddChild<T, A>(bool isFromPool, long id, Action<T, A> awakeAction, A a) where T : Entity, new()
        {
            T component = EntityObjectPool.Instance.GetEntity<T>(TypeId<T>.Id,isFromPool);
            component.Id = id;
            component.Parent = this;
            awakeAction(component, a);
            return component;
        }

        private T CreateAndAddChild<T, A, B>(bool isFromPool, long id, Action<T, A, B> awakeAction, A a, B b) where T : Entity, new()
        {
            T component = EntityObjectPool.Instance.GetEntity<T>(TypeId<T>.Id,isFromPool);
            component.Id = id;
            component.Parent = this;
            awakeAction(component, a, b);
            return component;
        }

        private T CreateAndAddChild<T, A, B, C>(bool isFromPool, long id, Action<T, A, B, C> awakeAction, A a, B b, C c) where T : Entity, new()
        {
            T component = EntityObjectPool.Instance.GetEntity<T>(TypeId<T>.Id,isFromPool);
            component.Id = id;
            component.Parent = this;
            awakeAction(component, a, b, c);
            return component;
        }
        #endregion

        #region 资源清理

        public void Dispose()
        {
            if (this.IsDisposed) return;

            // 触发Destroy事件
            if (this is IDestroy)
            {
                EntitySystemSingleton.Instance.Destroy(this);
            }
            // 立即标记为已销毁，防止重入（parent.RemoveChild会调用RecycleEntity导致再次进入Dispose）
            this.InstanceId = 0;
    
            // 从父级移除（此时Id和TypeId都还有效）
            if (this.parent?.IsDisposed == false)
            {
                if (this.IsComponent)
                {
                    parent.RemoveComponent(TypeId);
                }
                else
                {
                    parent.RemoveChild(this);
                }
            }

            // 清空其他标识
            IsRegister = false;
            Id = 0;

            // 清理子级
            if (this.children != null && this.children.Count > 0)
            {
                long[] childKeys = new long[this.children.Count];
                this.children.Keys.CopyTo(childKeys, 0);
                foreach (long childId in childKeys)
                {
                    RemoveChild(childId);
                }
            }

            // 清理组件
            if (this.components != null && this.components.Count > 0)
            {
                long[] componentKeys = new long[this.components.Count];
                this.components.Keys.CopyTo(componentKeys, 0);
                foreach (long typeId in componentKeys)
                {
                    RemoveComponent(typeId);
                }
            }
            
            this.iScene = null;
            this.parent = null;
            // 重置状态
            this.status = EntityStatus.None;
        }

        #endregion
    }
}