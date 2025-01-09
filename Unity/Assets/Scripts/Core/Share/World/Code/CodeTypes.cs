﻿using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ET
{
    public class CodeTypes: Singleton<CodeTypes>, ISingletonAwake<Assembly[]>
    {
        private readonly Dictionary<string, Type> allTypes = new();
        //private readonly UnOrderMultiMapSet<Type, Type> types = new();
        private readonly Dictionary<Type, HashSet<Type>> types = new();
        
        public void Awake(Assembly[] assemblies)
        {
            Dictionary<string, Type> addTypes = AssemblyHelper.GetAssemblyTypes(assemblies);
            foreach ((string fullName, Type type) in addTypes)
            {
                this.allTypes[fullName] = type;
                //Debug.LogError($"CodeTypes: {type}  | {fullName}");
                if (type.IsAbstract)
                {
                    continue;
                }
                
                // 记录所有的有BaseAttribute标记的的类型
                object[] objects = type.GetCustomAttributes(typeof(BaseAttribute), true);

                foreach (object o in objects)
                {
                    if (!this.types.ContainsKey(o.GetType()))
                    {
                        this.types[o.GetType()] = new HashSet<Type>();
                    }
                    this.types[o.GetType()].Add(type);
                    //Debug.LogError($"BaseAttribute {o.GetType()}  |  {type}   |   {o.GetType().Assembly.FullName}");;
                }
            }
        }

        public HashSet<Type> GetTypes(Type systemAttributeType)
        {
            if (!this.types.ContainsKey(systemAttributeType))
            {
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
            /*var hashSet = this.GetTypes(typeof (CodeProcessAttribute));
            foreach (Type type in hashSet)
            {
                object obj = Activator.CreateInstance(type);
                ((ISingletonAwake)obj).Awake();
                World.Instance.AddSingleton((ASingleton)obj);
            }*/
        }
    }
}