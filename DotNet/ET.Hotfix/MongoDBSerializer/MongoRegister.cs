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
            // 自动注册IgnoreExtraElements（版本兼容性：允许数据库文档包含多余字段）
            ConventionPack conventionPack = new() { new IgnoreExtraElementsConvention(true) };
            ConventionRegistry.Register("IgnoreExtraElements", conventionPack, type => true);

            // 注册 Unity.Mathematics 结构体（防御性：如果 Entity 使用这些类型并保存到 MongoDB）
            RegisterStruct<float2>();
            RegisterStruct<float3>();
            RegisterStruct<float4>();
            RegisterStruct<quaternion>();
            
            // 预注册消息类型（性能优化：避免首次序列化时的延迟，可选）
            // MongoDB 会在首次使用时自动创建映射，所以不是必需的
            // foreach (Type type in MessageOpcodeTypeMap.OpcodeToType.Values)
            // {
            //     if (!type.IsSubclassOf(typeof (Object)) || type.IsGenericType)
            //         continue;
            //     BsonClassMap.LookupClassMap(type);
            // }
        }
    }
}