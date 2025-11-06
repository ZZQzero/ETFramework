using System;
using System.Collections.Generic;
using System.Reflection;

namespace ET
{
    public class EntitySystemSingleton : Singleton<EntitySystemSingleton>, ISingletonAwake
    {
        [StaticField] public static TypeSystems TypeSystems { get; private set; } = new();

        public void Awake()
        {
        }

        public static void RegisterEntitySystem<T>() where T : SystemObject, new()
        {
            SystemObject obj = new T();
            if (obj is ISystemType iSystemType)
            {
                Type entityType = iSystemType.Type();
                Type systemType = iSystemType.SystemType();

                TypeSystems.OneTypeSystems oneTypeSystems = TypeSystems.GetOrCreateOneTypeSystems(entityType);
                oneTypeSystems.Map.Add(systemType, obj);

                SetSystemCapability(oneTypeSystems, obj);

                if (iSystemType is IClassEventSystem)
                {
                    oneTypeSystems.ClassType.Add(systemType);
                }
            }
        }

        // 辅助方法：设置能力位标记（使用接口检查，类型安全）
        private static void SetSystemCapability(TypeSystems.OneTypeSystems oneTypeSystems, SystemObject obj)
        {
            if (obj is IAwakeSystemMarker)
            {
                oneTypeSystems.Capabilities |= SystemFlags.Awake;
            }
            
            if (obj is IDestroySystemMarker)
            {
                oneTypeSystems.Capabilities |= SystemFlags.Destroy;
            }
            
            if (obj is AClassEventSystem<UpdateEvent>)
            {
                oneTypeSystems.Capabilities |= SystemFlags.Update;
            }
            
            if (obj is AClassEventSystem<LateUpdateEvent>)
            {
                oneTypeSystems.Capabilities |= SystemFlags.LateUpdate;
            }
        }

        public void Awake(Entity component)
        {
            Type entityType = component.GetType();

            var oneTypeSystems = TypeSystems.GetOneTypeSystems(entityType);
            if (oneTypeSystems == null || !oneTypeSystems.Capabilities.Has(SystemFlags.Awake))
            {
                return; // 没有AwakeSystem，直接返回
            }

            List<SystemObject> iAwakeSystems = TypeSystems.GetSystems(entityType, typeof(IAwakeSystem));
            if (iAwakeSystems == null)
            {
                return;
            }

            foreach (IAwakeSystem aAwakeSystem in iAwakeSystems)
            {
                if (aAwakeSystem == null)
                {
                    continue;
                }

                try
                {
                    aAwakeSystem.Run(component);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }

        public void Awake<P1>(Entity component, P1 p1)
        {
            Type entityType = component.GetType();

            var oneTypeSystems = TypeSystems.GetOneTypeSystems(entityType);
            if (oneTypeSystems == null || !oneTypeSystems.Capabilities.Has(SystemFlags.Awake))
            {
                return;
            }

            List<SystemObject> iAwakeSystems = TypeSystems.GetSystems(entityType, typeof(IAwakeSystem<P1>));
            if (iAwakeSystems == null)
            {
                return;
            }

            foreach (IAwakeSystem<P1> aAwakeSystem in iAwakeSystems)
            {
                if (aAwakeSystem == null)
                {
                    continue;
                }

                try
                {
                    aAwakeSystem.Run(component, p1);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }

        public void Awake<P1, P2>(Entity component, P1 p1, P2 p2)
        {
            Type entityType = component.GetType();

            var oneTypeSystems = TypeSystems.GetOneTypeSystems(entityType);
            if (oneTypeSystems == null || !oneTypeSystems.Capabilities.Has(SystemFlags.Awake))
            {
                return;
            }

            List<SystemObject> iAwakeSystems = TypeSystems.GetSystems(entityType, typeof(IAwakeSystem<P1, P2>));
            if (iAwakeSystems == null)
            {
                return;
            }

            foreach (IAwakeSystem<P1, P2> aAwakeSystem in iAwakeSystems)
            {
                if (aAwakeSystem == null)
                {
                    continue;
                }

                try
                {
                    aAwakeSystem.Run(component, p1, p2);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }

        public void Awake<P1, P2, P3>(Entity component, P1 p1, P2 p2, P3 p3)
        {
            Type entityType = component.GetType();

            var oneTypeSystems = TypeSystems.GetOneTypeSystems(entityType);
            if (oneTypeSystems == null || !oneTypeSystems.Capabilities.Has(SystemFlags.Awake))
            {
                return;
            }

            List<SystemObject> iAwakeSystems = TypeSystems.GetSystems(entityType, typeof(IAwakeSystem<P1, P2, P3>));
            if (iAwakeSystems == null)
            {
                return;
            }

            foreach (IAwakeSystem<P1, P2, P3> aAwakeSystem in iAwakeSystems)
            {
                if (aAwakeSystem == null)
                {
                    continue;
                }

                try
                {
                    aAwakeSystem.Run(component, p1, p2, p3);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }

        public void Destroy(Entity component)
        {
            Type entityType = component.GetType();

            var oneTypeSystems = TypeSystems.GetOneTypeSystems(entityType);
            if (oneTypeSystems == null || !oneTypeSystems.Capabilities.Has(SystemFlags.Destroy))
            {
                return;
            }

            List<SystemObject> iDestroySystems = TypeSystems.GetSystems(entityType, typeof(IDestroySystem));
            if (iDestroySystems == null)
            {
                return;
            }

            foreach (IDestroySystem iDestroySystem in iDestroySystems)
            {
                if (iDestroySystem == null)
                {
                    continue;
                }

                try
                {
                    iDestroySystem.Run(component);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }
    }
}