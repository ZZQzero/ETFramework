/*using System;
using System.Collections.Generic;
using System.Reflection;

namespace ET
{
    public class CodeTypes: Singleton<CodeTypes>, ISingletonAwake
    {
        private readonly Dictionary<string, Type> _allTypes = new();
        //private readonly UnOrderMultiMapSet<Type, Type> types = new();
        private readonly Dictionary<Type, HashSet<Type>> _types = new();
        
        private readonly List<Assembly> _assemblyList = new();
        private readonly Dictionary<string, Type> _allClassTypeDic = new();
        public void Awake()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                var assembly = assemblies[i];
                if (AssemblyHelper.AssembleNames.ContainsKey(assembly.FullName.Split(",")[0]))
                {
                    _assemblyList.Add(assembly);
                    
                    Log.Info(assembly.FullName);
                }
            }

            
            foreach (var item in _assemblyList)
            {
                var types = item.GetTypes();
                foreach (var type in types)
                {
                    if (type.FullName != null)
                    {
                        if (!_allClassTypeDic.TryAdd(type.FullName, type))
                        {
                            Log.Error($"已经存在相同的类型：{type.FullName}");
                        }
                    }
                }
            }

            foreach ((string fullName,Type type) in _allClassTypeDic)
            {
                _allTypes.Add(fullName,type);
                if (type.IsAbstract)
                {
                    continue;
                }
                object[] objects = type.GetCustomAttributes(typeof(BaseAttribute), true);
                foreach (object o in objects)
                {
                    var objType = o.GetType();
                    if (!_types.ContainsKey(objType))
                    {
                        HashSet<Type> hashSet = new HashSet<Type>();
                        _types.Add(objType,hashSet);
                    }
                    _types[objType].Add(type);
                }
            }
        }
        
        /*public void Awake(Assembly[] assemblies)
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
        #1#

        public HashSet<Type> GetTypes(Type systemAttributeType)
        {
            if (!this._types.ContainsKey(systemAttributeType))
            {
                return new HashSet<Type>();
            }

            return this._types[systemAttributeType];
        }

        public Dictionary<string, Type> GetTypes()
        {
            return _allTypes;
        }
        
        public Type GetType(string typeName)
        {
            Type type = null;
            this._allTypes.TryGetValue(typeName, out type);
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
            }#1#
        }
    }
}*/