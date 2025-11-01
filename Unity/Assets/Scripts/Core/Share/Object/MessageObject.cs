using System;
using System.ComponentModel;
#if DOTNET
using MongoDB.Bson.Serialization.Attributes;
#endif
using Nino.Core;

namespace ET
{
    [DisableNew]
    [NinoType]
    public abstract class MessageObject: IMessage, IPool
    {
        public virtual void Dispose() { }
#if DOTNET
        [BsonIgnore]
#endif
        [NinoIgnore]
        public bool IsFromPool { get; set; }
    }
}