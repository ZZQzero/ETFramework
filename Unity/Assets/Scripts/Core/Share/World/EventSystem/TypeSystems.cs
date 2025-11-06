using System;
using System.Collections.Generic;

namespace ET
{
    public class TypeSystems
    {
        public class OneTypeSystems
        {
            public readonly UnOrderMultiMap<Type, SystemObject> Map = new();
            // 这里不用hash，数量比较少，直接for循环速度更快
            public readonly List<Type> ClassType = new();
            // 在RegisterEntitySystem时设置，运行时只读，用于快速能力检查
            public SystemFlags Capabilities = SystemFlags.None;
        }
        
        private readonly Dictionary<Type, OneTypeSystems> typeSystemsMap = new();

        public OneTypeSystems GetOrCreateOneTypeSystems(Type type)
        {
            this.typeSystemsMap.TryGetValue(type, out var systems);
            if (systems != null)
            {
                return systems;
            }

            systems = new OneTypeSystems();
            this.typeSystemsMap.Add(type, systems);
            return systems;
        }

        public OneTypeSystems GetOneTypeSystems(Type type)
        {
            return typeSystemsMap.GetValueOrDefault(type);
        }

        public List<SystemObject> GetSystems(Type type, Type systemType)
        {
            if (!this.typeSystemsMap.TryGetValue(type, out var oneTypeSystems))
            {
                return null;
            }

            return oneTypeSystems.Map.GetValueOrDefault(systemType);
        }
    }
}