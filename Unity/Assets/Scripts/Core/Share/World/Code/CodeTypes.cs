using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ET
{
    public class CodeTypes: Singleton<CodeTypes>, ISingletonAwake<Assembly[]>
    {
        private readonly Dictionary<string, Type> allTypes = new();
        private readonly UnOrderMultiMapSet<Type, Type> types = new();
        
        public void Awake(Assembly[] assemblies)
        {
            Dictionary<string, Type> addTypes = AssemblyHelper.GetAssemblyTypes(assemblies);
            foreach ((string fullName, Type type) in addTypes)
            {
                this.allTypes[fullName] = type;
                Debug.LogError($"CodeTypes: {type}  | {fullName}");
                if (type.IsAbstract)
                {
                    continue;
                }
                
                // 记录所有的有BaseAttribute标记的的类型
                object[] objects = type.GetCustomAttributes(typeof(BaseAttribute), true);

                foreach (object o in objects)
                {
                    Debug.LogError($"Attribute: {o.GetType()}  | {type}  |  {type.Assembly.FullName}");
                    this.types.Add(o.GetType(), type);
                }
            }
        }

        public HashSet<Type> GetTypes(Type systemAttributeType)
        {
            Debug.LogError($"systemAttributeType :{systemAttributeType}  |  {systemAttributeType.Assembly.FullName}");

            foreach (var item in types)
            {
                if(item.Key.FullName == systemAttributeType.FullName)
                {
                    Debug.LogError($"item.Key :{item.Key}  |  {item.Key.Assembly.FullName}");
                    if (item.Key.Equals(systemAttributeType))
                    {
                        Debug.LogError($"systemAttributeType  {systemAttributeType}");
                    }
                }
            }
            
            if (!this.types.ContainsKey(systemAttributeType))
            {
                Debug.LogError($"types里面不存在{systemAttributeType}类型，也有可能他们不在一个程序集，即使名字相同，也会被认为是两个对象");
                return new HashSet<Type>();
            }

            return this.types[systemAttributeType];
        }

        public Dictionary<string, Type> GetTypes()
        {
            return allTypes;
        }
        
        public Type GetType(string typeName)
        {
            Type type = null;
            this.allTypes.TryGetValue(typeName, out type);
            return type;
        }
        
        public void CodeProcess()
        {
            var hashSet = this.GetTypes(typeof (CodeProcessAttribute));
            foreach (Type type in hashSet)
            {
                object obj = Activator.CreateInstance(type);
                ((ISingletonAwake)obj).Awake();
                World.Instance.AddSingleton((ASingleton)obj);
            }
        }
    }
}