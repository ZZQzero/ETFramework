using System;
using System.IO;
using MongoDB.Bson;

namespace ET
{
    public static partial class MessageLocationSenderComponentSystem
    {
        [EntitySystem]
        private static void Awake(this MessageLocationSenderComponent self)
        {
        }
        
        public static MessageLocationSenderOneType Get(this MessageLocationSenderComponent self, int locationType)
        {
            MessageLocationSenderOneType messageLocationSenderOneType = self.GetChild<MessageLocationSenderOneType>(locationType);
            if (messageLocationSenderOneType != null)
            {
                return messageLocationSenderOneType;
            }

            messageLocationSenderOneType = self.AddChildWithId<MessageLocationSenderOneType>(locationType);
            return messageLocationSenderOneType;
        }
    }
}