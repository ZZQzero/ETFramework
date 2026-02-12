using System;
using System.Collections.Generic;

namespace ET
{
    public class EntitySystem
    {
        private readonly Dictionary<Type, Queue<EntityRef<Entity>>> queues = new();
        
        public Queue<EntityRef<Entity>> GetQueue(Type type)
        {
            if (!this.queues.TryGetValue(type, out var queue))
            {
                queue = new Queue<EntityRef<Entity>>();
                this.queues.Add(type, queue);
            }

            return queue;
        }
        
        public virtual void RegisterSystem(Entity component)
        {
            Type type = component.GetType();
            
            TypeSystems.OneTypeSystems oneTypeSystems = EntitySystemSingleton.TypeSystems.GetOneTypeSystems(type);
            if (oneTypeSystems == null)
            {
                return;
            }

            foreach (Type queueType in oneTypeSystems.ClassType)
            {
                var queue = this.GetQueue(queueType);
                queue.Enqueue(component);
            }
        }
        
        public void Publish<T>(T t) where T: struct
        {
            Type systemType = typeof(AClassEventSystem<T>);
            
            if (!this.queues.TryGetValue(systemType, out var queue))
            {
                return;
            }
            
            int count = queue.Count;
            while (count-- > 0)
            {
                Entity component = queue.Dequeue();
                if (component == null || component.IsDisposed)
                {
                    continue;
                }

                queue.Enqueue(component);
                this.RunClassSystems(component, systemType, t);
            }
        }

        public bool TryPublishTo<T>(long targetInstanceId, T t) where T : struct
        {
            if (targetInstanceId == 0)
            {
                return false;
            }

            Type systemType = typeof(AClassEventSystem<T>);
            if (!this.queues.TryGetValue(systemType, out var queue))
            {
                return false;
            }

            int count = queue.Count;
            while (count-- > 0)
            {
                Entity component = queue.Dequeue();
                if (component == null || component.IsDisposed)
                {
                    continue;
                }

                queue.Enqueue(component);
                if (component.InstanceId != targetInstanceId)
                {
                    continue;
                }

                this.RunClassSystems(component, systemType, t);
                return true;
            }

            return false;
        }

        public void PublishTo<T>(long targetInstanceId, T t) where T : struct
        {
            this.TryPublishTo(targetInstanceId, t);
        }

        private void RunClassSystems<T>(Entity component, Type systemType, T t) where T : struct
        {
            Type componentType = component.GetType();
            try
            {
                List<SystemObject> systems = EntitySystemSingleton.TypeSystems.GetSystems(componentType, systemType);
                if (systems == null)
                {
                    return;
                }

                foreach (AClassEventSystem<T> classSystem in systems)
                {
                    try
                    {
                        classSystem.Run(component, t);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception($"entity system update fail: {componentType.FullName}", e);
            }
        }
    }
}