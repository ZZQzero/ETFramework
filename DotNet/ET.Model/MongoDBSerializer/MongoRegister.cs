using System;
using System.Collections.Generic;
using System.Reflection;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using Unity.Mathematics;

namespace ET
{
    public static class MongoRegister
    {
        public static void RegisterStruct<T>() where T : struct
        {
            BsonSerializer.RegisterSerializer(typeof (T), new StructBsonSerialize<T>());
        }
        
        public static void Init()
        {
            // 清理老的数据
            MethodInfo createSerializerRegistry = typeof (BsonSerializer).GetMethod("CreateSerializerRegistry", BindingFlags.Static | BindingFlags.NonPublic);
            createSerializerRegistry.Invoke(null, Array.Empty<object>());
            MethodInfo registerIdGenerators = typeof (BsonSerializer).GetMethod("RegisterIdGenerators", BindingFlags.Static | BindingFlags.NonPublic);
            registerIdGenerators.Invoke(null, Array.Empty<object>());
            
            //BsonSerializer.RegisterSerializer(typeof(ComponentsCollection), new BsonComponentsCollectionSerializer());
            //BsonSerializer.RegisterSerializer(typeof(ChildrenCollection), new BsonChildrenCollectionSerializer());
            
            
            // 自动注册IgnoreExtraElements
            ConventionPack conventionPack = new() { new IgnoreExtraElementsConvention(true) };
            ConventionRegistry.Register("IgnoreExtraElements", conventionPack, type => true);

            RegisterStruct<float2>();
            RegisterStruct<float3>();
            RegisterStruct<float4>();
            RegisterStruct<quaternion>();
            foreach (Type type in MessageOpcodeTypeMap.OpcodeToType.Values)
            {
                if (!type.IsSubclassOf(typeof (Object)))
                {
                    continue;
                }

                if (type.IsGenericType)
                {
                    continue;
                }
                
                BsonClassMap.LookupClassMap(type);
            }
        }
    }
}