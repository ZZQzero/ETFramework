using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace ET
{
    public class BsonComponentsCollectionSerializer: IBsonSerializer<ComponentsCollection>, IBsonSerializer
    {
        [StaticField]
        private static readonly IBsonSerializer<Entity> EntitySer = BsonSerializer.LookupSerializer<Entity>();
        public System.Type ValueType => typeof(ComponentsCollection);
        public ComponentsCollection  Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            IBsonReader bsonReader = context.Reader;
            bsonReader.ReadStartArray();
            ComponentsCollection componentsCollection = ComponentsCollection.Create(true);
            var eArgs = new BsonDeserializationArgs { NominalType = typeof(Entity) };

            while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
            {
                Entity entity = EntitySer.Deserialize(context,eArgs);
                entity.IsSerializeWithParent = true;
                componentsCollection.Add(entity.GetLongHashCode(), entity);
            }
            
            bsonReader.ReadEndArray();
            return componentsCollection;
        }

        object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
            => Deserialize(context, args);
        
        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, ComponentsCollection  value)
        {
            IBsonWriter bsonWriter = context.Writer;
            bsonWriter.WriteStartArray();
            var eArgs = new BsonSerializationArgs { NominalType = typeof(Entity) };
            
            IBsonSerializer<Entity> bsonSerializer = BsonSerializer.LookupSerializer<Entity>();
            foreach (var entity in value.Values)
            {
                if (entity is ISerializeToEntity || entity.IsSerializeWithParent)
                {
                    bsonSerializer.Serialize(context,eArgs,entity);
                }
            }
            bsonWriter.WriteEndArray();
        }

        void IBsonSerializer.Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
            => Serialize(context, args, (ComponentsCollection)value);
    }
}