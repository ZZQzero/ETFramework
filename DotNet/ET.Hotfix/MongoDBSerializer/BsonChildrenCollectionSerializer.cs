/*
using System;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace ET
{
    public sealed class BsonChildrenCollectionSerializer : IBsonSerializer<ChildrenCollection>
    {
        [StaticField]
        private static readonly IBsonSerializer<Entity> EntitySer = BsonSerializer.LookupSerializer<Entity>();
        public Type ValueType => typeof(ChildrenCollection);

        public ChildrenCollection Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var reader = context.Reader;
            var children = ChildrenCollection.Create(true);

            reader.ReadStartArray();
            while (reader.ReadBsonType() != BsonType.EndOfDocument)
            {
                var entity = EntitySer.Deserialize(context);
                entity.IsSerializeWithParent = true;
                children.Add(entity.Id, entity);
            }
            reader.ReadEndArray();

            return children;
        }

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, ChildrenCollection value)
        {
            var writer = context.Writer;
            
            if (value is null)
            {
                writer.WriteNull();
                writer.WriteEndArray();
                return;
            }
            
            writer.WriteStartArray();
            foreach (var kv in value)
            {
                var entity = kv.Value;
                if (entity is null)
                {
                    continue;
                }
                if (entity is ISerializeToEntity || entity.IsSerializeWithParent)
                {
                    EntitySer.Serialize(context, entity);
                }
            }

            writer.WriteEndArray();
        }

        // 非泛型接口转发到泛型实现，避免重复/分叉逻辑
        object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
            => Deserialize(context, args);

        void IBsonSerializer.Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
            => Serialize(context, args, (ChildrenCollection)value);
    }
}
*/
