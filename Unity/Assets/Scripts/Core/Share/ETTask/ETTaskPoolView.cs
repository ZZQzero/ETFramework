#if ENABLE_VIEW
using System;
using System.Collections.Generic;
using System.Reflection;


namespace ET
{
    /// <summary>
    /// ETTask对象池查看器
    /// 提供统一接口查看所有对象池的统计信息和管理操作
    /// </summary>
    public static class ETTaskPoolView
    {
        /// <summary>
        /// 获取ETTask对象池信息（通过反射访问内部字段）
        /// </summary>
        public static ETTaskPoolInfo GetETTaskPoolInfo()
        {
            const int MAX_POOL_SIZE = 100; // 与ETTaskPool中的常量保持一致
            
#if DOTNET
            // 服务端：访问ThreadLocal字段
            var localStackField = typeof(ETTaskPool).GetField("localStack", 
                BindingFlags.NonPublic | BindingFlags.Static);
            var totalAllocField = typeof(ETTaskPool).GetField("totalAlloc", 
                BindingFlags.NonPublic | BindingFlags.Static);
            var hitCountField = typeof(ETTaskPool).GetField("hitCount", 
                BindingFlags.NonPublic | BindingFlags.Static);
            var missCountField = typeof(ETTaskPool).GetField("missCount", 
                BindingFlags.NonPublic | BindingFlags.Static);
            var dupReturnCountField = typeof(ETTaskPool).GetField("dupReturnCount", 
                BindingFlags.NonPublic | BindingFlags.Static);
            
            if (localStackField == null || totalAllocField == null || 
                hitCountField == null || missCountField == null || dupReturnCountField == null)
            {
                return new ETTaskPoolInfo();
            }
            
            var localStack = localStackField.GetValue(null);
            var totalAlloc = totalAllocField.GetValue(null);
            var hitCount = hitCountField.GetValue(null);
            var missCount = missCountField.GetValue(null);
            var dupReturnCount = dupReturnCountField.GetValue(null);
            
            // 获取ThreadLocal的Value属性
            var stackValue = InvokeInstanceProperty<System.Collections.Generic.Stack<ETTask>>(
                localStack, "Value");
            var totalAllocValue = InvokeInstanceProperty<long>(totalAlloc, "Value");
            var hitCountValue = InvokeInstanceProperty<long>(hitCount, "Value");
            var missCountValue = InvokeInstanceProperty<long>(missCount, "Value");
            var dupReturnCountValue = InvokeInstanceProperty<long>(dupReturnCount, "Value");
            
            return new ETTaskPoolInfo
            {
                PoolSize = stackValue?.Count ?? 0,
                MaxSize = MAX_POOL_SIZE,
                TotalAllocations = totalAllocValue,
                PoolHits = hitCountValue,
                PoolMisses = missCountValue,
                DuplicateReturns = dupReturnCountValue
            };
#else
            // 客户端：访问数组和计数字段
            var poolCountField = typeof(ETTaskPool).GetField("poolCount", 
                BindingFlags.NonPublic | BindingFlags.Static);
            var totalAllocField = typeof(ETTaskPool).GetField("totalAlloc", 
                BindingFlags.NonPublic | BindingFlags.Static);
            var hitCountField = typeof(ETTaskPool).GetField("hitCount", 
                BindingFlags.NonPublic | BindingFlags.Static);
            var missCountField = typeof(ETTaskPool).GetField("missCount", 
                BindingFlags.NonPublic | BindingFlags.Static);
            var dupReturnCountField = typeof(ETTaskPool).GetField("dupReturnCount", 
                BindingFlags.NonPublic | BindingFlags.Static);
            
            if (poolCountField == null || totalAllocField == null || 
                hitCountField == null || missCountField == null || dupReturnCountField == null)
            {
                return new ETTaskPoolInfo();
            }
            
            var poolCount = (int)poolCountField.GetValue(null);
            var totalAlloc = (long)totalAllocField.GetValue(null);
            var hitCount = (long)hitCountField.GetValue(null);
            var missCount = (long)missCountField.GetValue(null);
            var dupReturnCount = (long)dupReturnCountField.GetValue(null);
            
            return new ETTaskPoolInfo
            {
                PoolSize = poolCount,
                MaxSize = MAX_POOL_SIZE,
                TotalAllocations = totalAlloc,
                PoolHits = hitCount,
                PoolMisses = missCount,
                DuplicateReturns = dupReturnCount
            };
#endif
        }
        /// <summary>
        /// 已注册的泛型池类型注册表（类型全名 -> 是否已使用）
        /// </summary>
        [StaticField]
        private static readonly HashSet<string> _registeredGenericPoolTypes = new HashSet<string>();

        /// <summary>
        /// 是否已经扫描过程序集（避免重复扫描）
        /// </summary>
        [StaticField]
        private static bool _hasScannedAssemblies = false;
        
        /// <summary>
        /// 是否正在扫描中（用于避免重复触发扫描）
        /// </summary>
        [StaticField]
        private static bool _isScanning = false;

        /// <summary>
        /// 注册泛型池类型（内部使用，通过反射发现已使用的类型）
        /// </summary>
        private static void RegisterGenericPoolTypeInternal(string typeName)
        {
            _registeredGenericPoolTypes.Add(typeName);
        }

        /// <summary>
        /// 获取已注册的泛型池类型名称列表（供ETTaskPoolView使用）
        /// </summary>
        public static HashSet<string> GetRegisteredGenericPoolTypesInternal()
        {
            return new HashSet<string>(_registeredGenericPoolTypes);
        }

        /// <summary>
        /// 获取所有泛型对象池信息（只返回实际使用过的池）
        /// </summary>
        public static Dictionary<string, ETTaskPoolInfo> GetAllGenericPoolInfos()
        {
            Dictionary<string, ETTaskPoolInfo> result = new Dictionary<string, ETTaskPoolInfo>();
            
            // 通过反射获取所有已注册的泛型池类型
            foreach (var typeName in GetRegisteredGenericPoolTypes())
            {
                try
                {
                    // 通过反射获取池信息
                    var poolInfo = GetGenericPoolInfoByTypeName(typeName);
                    if (poolInfo.HasValue)
                    {
                        // 只显示实际使用过的池（TotalAllocations > 0 或 PoolSize > 0）
                        // 避免显示只是类型定义中有但未实际使用的池
                        if (poolInfo.Value.TotalAllocations > 0 || poolInfo.Value.PoolSize > 0)
                        {
                            result[typeName] = poolInfo.Value;
                        }
                    }
                }
                catch
                {
                    // 忽略反射失败的类型
                }
            }
            
            return result;
        }

        /// <summary>
        /// 获取所有对象池的总览信息
        /// </summary>
        public static ETTaskPoolViewStats GetAllStats()
        {
            var etTaskPoolInfo = GetETTaskPoolInfo();
            var genericPoolInfos = GetAllGenericPoolInfos();
            
            int totalObjects = etTaskPoolInfo.PoolSize;
            foreach (var info in genericPoolInfos.Values)
            {
                totalObjects += info.PoolSize;
            }
            
            return new ETTaskPoolViewStats
            {
                PoolCount = 1 + genericPoolInfos.Count,
                TotalObjects = totalObjects,
                ETTaskPoolInfo = etTaskPoolInfo,
                GenericPoolInfos = genericPoolInfos
            };
        }

        /// <summary>
        /// 清空所有对象池（包括ETTask和所有ETTask<T>）
        /// </summary>
        public static void ClearAllPools()
        {
            // 通过反射清空ETTask池
            InvokeStaticVoidMethod(typeof(ETTaskPool), "Clear", null);
            // 清空所有泛型池
            ClearAllGenericPools();
        }

        /// <summary>
        /// 清空所有泛型对象池
        /// </summary>
        public static void ClearAllGenericPools()
        {
            foreach (var typeName in GetRegisteredGenericPoolTypes())
            {
                try
                {
                    ClearGenericPoolByTypeName(typeName);
                }
                catch
                {
                    // 忽略清空失败的类型
                }
            }
        }

        /// <summary>
        /// 运行健康检查（仅DEBUG模式，通过反射访问内部字段）
        /// </summary>
        public static void RunHealthCheck()
        {
            HashSet<ETTask> uniqueTasks = new HashSet<ETTask>();
            int duplicates = 0;
            int unmarkedCount = 0;
            int totalTasks = 0;

#if DOTNET
            // 服务端：访问ThreadLocal字段
            var localStackField = typeof(ETTaskPool).GetField("localStack", 
                BindingFlags.NonPublic | BindingFlags.Static);
            if (localStackField == null) return;
            
            var localStack = localStackField.GetValue(null);
            var stackValue = InvokeInstanceProperty<System.Collections.Generic.Stack<ETTask>>(
                localStack, "Value");
            
            if (stackValue == null) return;
            
            foreach (var task in stackValue)
            {
                totalTasks++;
                
                if (!uniqueTasks.Add(task))
                {
                    duplicates++;
                    Log.Error($"发现重复对象: HashCode={task.GetHashCode()}");
                }

                if (!task.IsPooled)
                {
                    unmarkedCount++;
                    Log.Error($"池中对象未标记IsPooled: HashCode={task.GetHashCode()}");
                }
            }
#else
            // 客户端：访问数组和计数字段
            var poolField = typeof(ETTaskPool).GetField("pool", 
                BindingFlags.NonPublic | BindingFlags.Static);
            var poolCountField = typeof(ETTaskPool).GetField("poolCount", 
                BindingFlags.NonPublic | BindingFlags.Static);
            
            if (poolField == null || poolCountField == null) return;
            
            var pool = poolField.GetValue(null) as ETTask[];
            var poolCount = (int)poolCountField.GetValue(null);
            
            if (pool == null) return;
            
            for (int i = 0; i < poolCount; i++)
            {
                if (pool[i] == null)
                {
                    Log.Error($"池中索引{i}为null");
                    continue;
                }
                
                totalTasks++;
                var task = pool[i];
                
                if (!uniqueTasks.Add(task))
                {
                    duplicates++;
                    Log.Error($"发现重复对象: HashCode={task.GetHashCode()}");
                }

                if (!task.IsPooled)
                {
                    unmarkedCount++;
                    Log.Error($"池中对象未标记IsPooled: HashCode={task.GetHashCode()}");
                }
            }
#endif

            if (duplicates > 0 || unmarkedCount > 0)
            {
                Log.Error($"池健康检查失败: 总数={totalTasks}, 重复={duplicates}, 未标记={unmarkedCount}");
            }
            else
            {
                Log.Info($"池健康检查通过: {totalTasks}个对象全部正常");
            }
        }

        #region 私有方法

        /// <summary>
        /// 通过反射调用静态方法（有返回值）
        /// </summary>
        private static T InvokeStaticMethod<T>(Type type, string methodName, object[] parameters)
        {
            try
            {
                var method = type.GetMethod(methodName, 
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                
                if (method == null)
                {
                    return default(T);
                }
                
                var result = method.Invoke(null, parameters);
                return (T)result;
            }
            catch
            {
                return default(T);
            }
        }

        /// <summary>
        /// 通过反射调用静态方法（void返回）
        /// </summary>
        private static void InvokeStaticVoidMethod(Type type, string methodName, object[] parameters)
        {
            try
            {
                var method = type.GetMethod(methodName, 
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                
                if (method == null)
                {
                    return;
                }
                
                method.Invoke(null, parameters);
            }
            catch
            {
                // 忽略反射调用失败
            }
        }

        /// <summary>
        /// 通过反射获取实例属性值
        /// </summary>
        private static T InvokeInstanceProperty<T>(object instance, string propertyName)
        {
            try
            {
                if (instance == null) return default(T);
                
                var property = instance.GetType().GetProperty(propertyName, 
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                
                if (property == null)
                {
                    return default(T);
                }
                
                var value = property.GetValue(instance);
                return (T)value;
            }
            catch
            {
                return default(T);
            }
        }

        /// <summary>
        /// 获取已注册的泛型池类型名称列表（自动发现并注册已使用的类型）
        /// </summary>
        private static HashSet<string> GetRegisteredGenericPoolTypes()
        {
            if (_isScanning)
            {
                return GetRegisteredGenericPoolTypesInternal();
            }
            
            if (!_hasScannedAssemblies)
            {
                _isScanning = true;
                try
                {
                    DiscoverAndRegisterGenericPoolTypes();
                }
                finally
                {
                    _isScanning = false;
                }
                return new HashSet<string>();
            }
            
            DiscoverAndRegisterGenericPoolTypes();
            return GetRegisteredGenericPoolTypesInternal();
        }

        /// <summary>
        /// 发现并注册所有已使用的泛型池类型（通过扫描程序集，带缓存）
        /// </summary>
        private static void DiscoverAndRegisterGenericPoolTypes()
        {
            if (_hasScannedAssemblies)
            {
                return;
            }

            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                int scannedCount = 0;
                int skippedCount = 0;
                int typeCount = 0;
                
                foreach (var assembly in assemblies)
                {
                    try
                    {
                        string assemblyName = assembly.GetName().Name;
                        
                        if (assemblyName.StartsWith("UnityEngine") ||
                            assemblyName.StartsWith("UnityEditor") ||
                            assemblyName.StartsWith("Unity.") ||
                            assemblyName.StartsWith("System") ||
                            assemblyName.StartsWith("mscorlib") ||
                            assemblyName.StartsWith("netstandard") ||
                            assemblyName.StartsWith("Microsoft") ||
                            assemblyName.StartsWith("nunit") ||
                            assemblyName.StartsWith("Newtonsoft") ||
                            assemblyName.StartsWith("YooAsset") ||
                            assemblyName.StartsWith("GameUI"))
                        {
                            skippedCount++;
                            continue;
                        }
                        
                        bool shouldScan = assemblyName.Contains("ET") || 
                                         assemblyName.Contains("Assembly-CSharp") ||
                                         assemblyName.Contains("Hotfix") ||
                                         assemblyName.Contains("Model") ||
                                         assemblyName.Contains("Core");
                        
                        Type[] types = null;
                        
                        if (!shouldScan)
                        {
                            try
                            {
                                types = assembly.GetTypes();
                                if (types.Length > 0)
                                {
                                    bool hasETNamespace = false;
                                    int checkCount = Math.Min(10, types.Length);
                                    for (int i = 0; i < checkCount; i++)
                                    {
                                        if (types[i].Namespace != null && types[i].Namespace.StartsWith("ET"))
                                        {
                                            hasETNamespace = true;
                                            break;
                                        }
                                    }
                                    if (!hasETNamespace)
                                    {
                                        skippedCount++;
                                        continue;
                                    }
                                }
                                else
                                {
                                    skippedCount++;
                                    continue;
                                }
                            }
                            catch
                            {
                                skippedCount++;
                                continue;
                            }
                        }
                        else
                        {
                            types = assembly.GetTypes();
                        }
                        
                        typeCount += types.Length;
                        foreach (var type in types)
                        {
                            DiscoverETTaskGenericTypes(type);
                        }
                        
                        scannedCount++;
                    }
                    catch
                    {
                        skippedCount++;
                    }
                }
                
                _hasScannedAssemblies = true;
            }
            catch (Exception ex)
            {
                _hasScannedAssemblies = true;
            }
        }

        /// <summary>
        /// 从类型中发现ETTask<T>的使用并注册
        /// </summary>
        private static void DiscoverETTaskGenericTypes(Type type)
        {
            try
            {
                // 检查字段
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | 
                    BindingFlags.Static | BindingFlags.Instance);
                
                foreach (var field in fields)
                {
                    if (field.FieldType.IsGenericType && 
                        field.FieldType.GetGenericTypeDefinition() == typeof(ETTask<>))
                    {
                        var genericArg = field.FieldType.GetGenericArguments()[0];
                        RegisterGenericPoolTypeInternal(genericArg.FullName ?? genericArg.Name);
                    }
                }
                
                // 检查属性
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | 
                    BindingFlags.Static | BindingFlags.Instance);
                
                foreach (var prop in properties)
                {
                    if (prop.PropertyType.IsGenericType && 
                        prop.PropertyType.GetGenericTypeDefinition() == typeof(ETTask<>))
                    {
                        var genericArg = prop.PropertyType.GetGenericArguments()[0];
                        RegisterGenericPoolTypeInternal(genericArg.FullName ?? genericArg.Name);
                    }
                }
                
                // 检查方法返回类型和参数
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | 
                    BindingFlags.Static | BindingFlags.Instance);
                
                foreach (var method in methods)
                {
                    // 返回类型
                    if (method.ReturnType.IsGenericType && 
                        method.ReturnType.GetGenericTypeDefinition() == typeof(ETTask<>))
                    {
                        var genericArg = method.ReturnType.GetGenericArguments()[0];
                        RegisterGenericPoolTypeInternal(genericArg.FullName ?? genericArg.Name);
                    }
                    
                    // 参数类型
                    foreach (var param in method.GetParameters())
                    {
                        if (param.ParameterType.IsGenericType && 
                            param.ParameterType.GetGenericTypeDefinition() == typeof(ETTask<>))
                        {
                            var genericArg = param.ParameterType.GetGenericArguments()[0];
                            RegisterGenericPoolTypeInternal(genericArg.FullName ?? genericArg.Name);
                        }
                    }
                }
            }
            catch
            {
                // 忽略无法访问的类型
            }
        }

        /// <summary>
        /// 通过类型名称获取泛型池信息（通过反射直接访问内部字段）
        /// </summary>
        private static ETTaskPoolInfo? GetGenericPoolInfoByTypeName(string typeName)
        {
            try
            {
                const int MAX_POOL_SIZE = 100; // 与ETTaskPool<T>中的常量保持一致
                
                // 查找类型
                var type = Type.GetType(typeName);
                if (type == null)
                {
                    // 尝试在已加载的程序集中查找
                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        type = assembly.GetType(typeName);
                        if (type != null) break;
                    }
                }
                
                if (type == null)
                {
                    return null;
                }
                
                // 构造泛型类型 ETTaskPool<T>
                var genericPoolType = typeof(ETTaskPool<>).MakeGenericType(type);
                
#if DOTNET
                // 服务端：访问ThreadLocal字段
                var localStackField = genericPoolType.GetField("localStack", 
                    BindingFlags.NonPublic | BindingFlags.Static);
                var totalAllocField = genericPoolType.GetField("totalAlloc", 
                    BindingFlags.NonPublic | BindingFlags.Static);
                var hitCountField = genericPoolType.GetField("hitCount", 
                    BindingFlags.NonPublic | BindingFlags.Static);
                var missCountField = genericPoolType.GetField("missCount", 
                    BindingFlags.NonPublic | BindingFlags.Static);
                var dupReturnCountField = genericPoolType.GetField("dupReturnCount", 
                    BindingFlags.NonPublic | BindingFlags.Static);
                
                if (localStackField == null || totalAllocField == null || 
                    hitCountField == null || missCountField == null || dupReturnCountField == null)
                {
                    return null;
                }
                
                var localStack = localStackField.GetValue(null);
                var totalAlloc = totalAllocField.GetValue(null);
                var hitCount = hitCountField.GetValue(null);
                var missCount = missCountField.GetValue(null);
                var dupReturnCount = dupReturnCountField.GetValue(null);
                
                // 获取ThreadLocal的Value属性
                var stackValue = InvokeInstanceProperty<System.Collections.Generic.Stack<object>>(
                    localStack, "Value");
                var totalAllocValue = InvokeInstanceProperty<long>(totalAlloc, "Value");
                var hitCountValue = InvokeInstanceProperty<long>(hitCount, "Value");
                var missCountValue = InvokeInstanceProperty<long>(missCount, "Value");
                var dupReturnCountValue = InvokeInstanceProperty<long>(dupReturnCount, "Value");
                
                return new ETTaskPoolInfo
                {
                    PoolSize = GetStackCount(stackValue),
                    MaxSize = MAX_POOL_SIZE,
                    TotalAllocations = totalAllocValue,
                    PoolHits = hitCountValue,
                    PoolMisses = missCountValue,
                    DuplicateReturns = dupReturnCountValue
                };
#else
                // 客户端：访问数组和计数字段
                var poolCountField = genericPoolType.GetField("poolCount", 
                    BindingFlags.NonPublic | BindingFlags.Static);
                var totalAllocField = genericPoolType.GetField("totalAlloc", 
                    BindingFlags.NonPublic | BindingFlags.Static);
                var hitCountField = genericPoolType.GetField("hitCount", 
                    BindingFlags.NonPublic | BindingFlags.Static);
                var missCountField = genericPoolType.GetField("missCount", 
                    BindingFlags.NonPublic | BindingFlags.Static);
                var dupReturnCountField = genericPoolType.GetField("dupReturnCount", 
                    BindingFlags.NonPublic | BindingFlags.Static);
                
                if (poolCountField == null || totalAllocField == null || 
                    hitCountField == null || missCountField == null || dupReturnCountField == null)
                {
                    return null;
                }
                
                var poolCount = (int)poolCountField.GetValue(null);
                var totalAlloc = (long)totalAllocField.GetValue(null);
                var hitCount = (long)hitCountField.GetValue(null);
                var missCount = (long)missCountField.GetValue(null);
                var dupReturnCount = (long)dupReturnCountField.GetValue(null);
                
                return new ETTaskPoolInfo
                {
                    PoolSize = poolCount,
                    MaxSize = MAX_POOL_SIZE,
                    TotalAllocations = totalAlloc,
                    PoolHits = hitCount,
                    PoolMisses = missCount,
                    DuplicateReturns = dupReturnCount
                };
#endif
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 获取Stack的Count（通过反射）
        /// </summary>
        private static int GetStackCount(object stack)
        {
            if (stack == null) return 0;
            try
            {
                var countProperty = stack.GetType().GetProperty("Count");
                if (countProperty != null)
                {
                    return (int)countProperty.GetValue(stack);
                }
            }
            catch
            {
            }
            return 0;
        }

        /// <summary>
        /// 通过类型名称清空泛型池（使用反射）
        /// </summary>
        private static void ClearGenericPoolByTypeName(string typeName)
        {
            try
            {
                // 查找类型
                var type = Type.GetType(typeName);
                if (type == null)
                {
                    // 尝试在已加载的程序集中查找
                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        type = assembly.GetType(typeName);
                        if (type != null) break;
                    }
                }
                
                if (type == null)
                {
                    return;
                }
                
                // 构造泛型类型 ETTaskPool<T>
                var genericPoolType = typeof(ETTaskPool<>).MakeGenericType(type);
                
                // 获取 Clear 方法
                var clearMethod = genericPoolType.GetMethod("Clear", 
                    BindingFlags.Public | BindingFlags.Static);
                
                if (clearMethod == null)
                {
                    return;
                }
                
                // 调用方法
                clearMethod.Invoke(null, null);
            }
            catch
            {
                // 忽略反射失败
            }
        }

        #endregion
    }

    /// <summary>
    /// ETTask对象池查看统计信息
    /// </summary>
    public struct ETTaskPoolViewStats
    {
        /// <summary>
        /// 对象池总数（1个ETTask池 + N个ETTask<T>池）
        /// </summary>
        public int PoolCount;
        
        /// <summary>
        /// 所有池中的对象总数
        /// </summary>
        public int TotalObjects;
        
        /// <summary>
        /// ETTask对象池信息
        /// </summary>
        public ETTaskPoolInfo ETTaskPoolInfo;
        
        /// <summary>
        /// 所有泛型对象池信息（类型名称 -> 池信息）
        /// </summary>
        public Dictionary<string, ETTaskPoolInfo> GenericPoolInfos;
    }
}
#endif

