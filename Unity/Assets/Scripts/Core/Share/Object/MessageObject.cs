using System;
using System.ComponentModel;
#if DOTNET
using MongoDB.Bson.Serialization.Attributes;
#endif

namespace ET
{
    [DisableNew]
    public abstract class MessageObject: IMessage, IPool
    {
        public virtual void Dispose()
        {
        }
#if DOTNET
        [BsonIgnore]
#endif
        public bool IsFromPool { get; set; }
    }
}