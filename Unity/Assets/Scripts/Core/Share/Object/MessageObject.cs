using System;
using System.ComponentModel;

namespace ET
{
    [DisableNew]
    public abstract class MessageObject: ProtoObject, IMessage, IPool
    {
        public virtual void Dispose()
        {
        }

        //[BsonIgnore]
        public bool IsFromPool { get; set; }

        public void Reset()
        {
            
        }
    }
}