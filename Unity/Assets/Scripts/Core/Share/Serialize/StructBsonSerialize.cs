using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;

namespace ET
{
    /// <summary>
    /// 结构体 BSON 序列化器（仅字段；支持常见特性；静态缓存；高性能 Getter/Setter）
    /// </summary>
    public class StructBsonSerialize<TValue> : StructSerializerBase<TValue> where TValue : struct
    {
        [StaticField]
        private static readonly List<FieldMeta> FieldList;
        [StaticField]
        private static readonly Dictionary<string, FieldMeta> Lookup;
        private sealed class FieldMeta
        {
            public string ElementName { get; set; }
            public FieldInfo Field { get; set; }
            public Type FieldType { get; set; }
            public bool IgnoreIfDefault { get; set; }
            public object DefaultValue { get; set; }
            public Func<TValue, object> Getter { get; set; }
            public Action<object, object> Setter { get; set; }
        }

        static StructBsonSerialize()
        {
            var t = typeof(TValue);
            FieldList = new List<FieldMeta>();

            foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                // 显式忽略
                if (f.IsDefined(typeof(BsonIgnoreAttribute), false))
                    continue;

                // 仅包含：公有字段，或带了 [BsonElement] 的非公有字段
                var el = f.GetCustomAttribute<BsonElementAttribute>(false);
                var include = f.IsPublic || el != null;
                if (!include) continue;

                // 避免 struct 自身类型递归
                if (f.FieldType == t) continue;

                var elementName = el?.ElementName ?? f.Name;
                var ignoreIfDefault = f.IsDefined(typeof(BsonIgnoreIfDefaultAttribute), false);

                // 默认值优化
                object defaultValue = null;
                var defAttr = f.GetCustomAttribute<BsonDefaultValueAttribute>(false);
                if (defAttr != null)
                {
                    defaultValue = defAttr.DefaultValue;
                }
                else if (f.FieldType.IsValueType && !f.FieldType.IsGenericType)
                {
                    defaultValue = Activator.CreateInstance(f.FieldType);
                }

                // 高性能 getter/setter
                var getter = CreateGetter(f);
                var setter = CreateSetter(f);

                FieldList.Add(new FieldMeta
                {
                    ElementName = elementName,
                    Field = f,
                    FieldType = f.FieldType,
                    IgnoreIfDefault = ignoreIfDefault,
                    DefaultValue = defaultValue,
                    Getter = getter,
                    Setter = setter
                });
            }
            

            Lookup = new Dictionary<string, FieldMeta>(FieldList.Count, StringComparer.Ordinal);
            foreach (var field in FieldList)
            {
                Lookup[field.ElementName] = field;
            }
        }

        // Getter：TValue → object
        private static Func<TValue, object> CreateGetter(FieldInfo field)
        {
            var instanceParam = Expression.Parameter(typeof(TValue), "instance");
            var fieldAccess = Expression.Field(instanceParam, field);
            var convert = Expression.Convert(fieldAccess, typeof(object));
            return Expression.Lambda<Func<TValue, object>>(convert, instanceParam).Compile();
        }

        // Setter：object (boxed) → 修改值
        private static Action<object, object> CreateSetter(FieldInfo field)
        {
            return (instance, value) =>
            {
                var boxed = instance;
                var current = (TValue)boxed; // unbox
                field.SetValueDirect(__makeref(current), value); // 直接修改值类型字段
                instance = current; // 写回
            };
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TValue value)
        {
            var writer = context.Writer;
            writer.WriteStartDocument();

            foreach (var m in FieldList)
            {
                var v = m.Getter(value);
                if (m.IgnoreIfDefault && Equals(v, m.DefaultValue))
                    continue;

                writer.WriteName(m.ElementName);
                BsonSerializer.Serialize(writer, m.FieldType, v);
            }

            writer.WriteEndDocument();
        }

        public override TValue Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var reader = context.Reader;
            reader.ReadStartDocument();

            object boxed = new TValue();

            while (reader.ReadBsonType() != BsonType.EndOfDocument)
            {
                var name = reader.ReadName(Utf8NameDecoder.Instance);

                if (Lookup.TryGetValue(name, out var meta))
                {
                    var val = BsonSerializer.Deserialize(reader, meta.FieldType);
                    meta.Setter(boxed, val);
                }
                else
                {
                    reader.SkipValue(); // 忽略未知字段
                }
            }

            reader.ReadEndDocument();
            return (TValue)boxed;
        }
    }
}
