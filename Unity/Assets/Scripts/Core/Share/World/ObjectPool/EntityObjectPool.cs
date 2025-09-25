using System.Collections.Generic;

namespace ET
{
    public class EntityObjectPool
    {
        private static EntityObjectPool _instance;
        public static EntityObjectPool Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new EntityObjectPool();
                }
                return _instance;
            }
        }
        
        private readonly Dictionary<long,Queue<Entity>> _pool = new Dictionary<long,Queue<Entity>>();

        public T GetEntity<T>(long typeId) where T : Entity,new()
        {
            var queue = _pool.GetValueOrDefault(typeId);
            if (queue != null)
            {
                if (queue.Count > 0)
                {
                    return (T)queue.Dequeue();
                }

                return CreateEntity<T>();
            }

            queue = new Queue<Entity>();
            _pool.Add(typeId, queue);
            return CreateEntity<T>();
        }

        public void RecycleEntity(Entity entity)
        {
            if (_pool.TryGetValue(entity.TypeId, out var queue))
            {
                queue.Enqueue(entity);
            }
        }

        private T CreateEntity<T>() where T : Entity,new()
        {
            T component = new T();
            component.TypeId = TypeId<T>.Id;
            component.IsNew = true;
            component.Id = 0;
            return component; 
        }
    }
}