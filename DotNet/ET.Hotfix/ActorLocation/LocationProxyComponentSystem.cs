using System;
using System.Collections.Generic;

namespace ET
{
    public static partial class LocationProxyComponentSystem
    {
        private static ActorId GetLocationSceneId(this LocationProxyComponent self, long key)
        {
            List<StartSceneConfig> locationConfigs = StartSceneConfigManager.Instance.GetBySceneType(self.Zone(), SceneType.Location);
            return locationConfigs[(int)(key % locationConfigs.Count)].ActorId;
        }

        public static async ETTask Add(this LocationProxyComponent self, int type, long key, ActorId actorId)
        {
            Fiber fiber = self.Fiber();
            Log.Info($"location proxy add {key}, {actorId} {TimeInfo.Instance.ServerNow()}");
            ObjectAddRequest objectAddRequest = ObjectAddRequest.Create();
            objectAddRequest.Type = type;
            objectAddRequest.Key = key;
            objectAddRequest.ActorId = actorId;
            await fiber.Root.GetComponent<MessageSender>().Call(self.GetLocationSceneId(key), objectAddRequest);
        }

        public static async ETTask Lock(this LocationProxyComponent self, int type, long key, ActorId actorId, int time = 60000)
        {
            Fiber fiber = self.Fiber();
            Log.Info($"location proxy lock {key}, {actorId} {TimeInfo.Instance.ServerNow()}");

            ObjectLockRequest objectLockRequest = ObjectLockRequest.Create();
            objectLockRequest.Type = type;
            objectLockRequest.Key = key;
            objectLockRequest.ActorId = actorId;
            objectLockRequest.Time = time;
            await fiber.Root.GetComponent<MessageSender>().Call(self.GetLocationSceneId(key), objectLockRequest);
        }

        public static async ETTask UnLock(this LocationProxyComponent self, int type, long key, ActorId oldActorId, ActorId newActorId)
        {
            Fiber fiber = self.Fiber();
            Log.Info($"location proxy unlock {key}, {newActorId} {TimeInfo.Instance.ServerNow()}");
            ObjectUnLockRequest objectUnLockRequest = ObjectUnLockRequest.Create();
            objectUnLockRequest.Type = type;
            objectUnLockRequest.Key = key;
            objectUnLockRequest.OldActorId = oldActorId;
            objectUnLockRequest.NewActorId = newActorId;
            await fiber.Root.GetComponent<MessageSender>().Call(self.GetLocationSceneId(key), objectUnLockRequest);
        }

        public static async ETTask Remove(this LocationProxyComponent self, int type, long key)
        {
            Fiber fiber = self.Fiber();
            Log.Info($"location proxy remove {key}, {TimeInfo.Instance.ServerNow()}");

            ObjectRemoveRequest objectRemoveRequest = ObjectRemoveRequest.Create();
            objectRemoveRequest.Type = type;
            objectRemoveRequest.Key = key;
            await fiber.Root.GetComponent<MessageSender>().Call(self.GetLocationSceneId(key), objectRemoveRequest);
        }

        public static async ETTask<ActorId> Get(this LocationProxyComponent self, int type, long key)
        {
            if (key == 0)
            {
                throw new Exception($"get location key 0");
            }

            // location server配置到共享区，一个大战区可以配置N多个location server,这里暂时为1
            ObjectGetRequest objectGetRequest = ObjectGetRequest.Create();
            objectGetRequest.Type = type;
            objectGetRequest.Key = key;
            ObjectGetResponse response =
                    (ObjectGetResponse) await self.Root().GetComponent<MessageSender>().Call(self.GetLocationSceneId(key), objectGetRequest);
            return response.ActorId;
        }

        public static async ETTask AddLocation(this Entity self, int type)
        {
            await self.Root().GetComponent<LocationProxyComponent>().Add(type, self.Id, self.GetActorId());
        }

        /// <summary>
        /// 批量添加Location（性能优化：减少网络往返次数）
        /// 一次RPC可以注册多个Location，减少网络往返次数
        /// </summary>
        public static async ETTask AddBatch(this LocationProxyComponent self, List<(int type, long key, ActorId actorId)> items)
        {
            if (items == null || items.Count == 0)
            {
                return;
            }
            
            Fiber fiber = self.Fiber();
            Log.Info($"location proxy batch add count={items.Count}, {TimeInfo.Instance.ServerNow()}");
            
            // 按Location服务器分组，因为不同的key可能路由到不同的Location服务器
            Dictionary<ActorId, List<(int type, long key, ActorId actorId)>> groupedItems = new Dictionary<ActorId, List<(int type, long key, ActorId actorId)>>();
            
            foreach (var item in items)
            {
                ActorId locationSceneId = self.GetLocationSceneId(item.key);
                if (!groupedItems.TryGetValue(locationSceneId, out var list))
                {
                    list = new List<(int type, long key, ActorId actorId)>();
                    groupedItems[locationSceneId] = list;
                }
                list.Add(item);
            }
            
            // 并行向不同的Location服务器发送批量请求
            List<ETTask<IResponse>> tasks = new List<ETTask<IResponse>>();
            foreach (var kvp in groupedItems)
            {
                ObjectAddBatchRequest batchRequest = ObjectAddBatchRequest.Create();
                foreach (var item in kvp.Value)
                {
                    ObjectAddBatchItem batchItem = new ObjectAddBatchItem
                    {
                        Type = item.type,
                        Key = item.key,
                        ActorId = item.actorId
                    };
                    batchRequest.Items.Add(batchItem);
                }
                
                tasks.Add(fiber.Root.GetComponent<MessageSender>().Call(kvp.Key, batchRequest));
            }
            
            // 等待所有批量请求完成，并检查响应错误
            foreach (var task in tasks)
            {
                IResponse response = await task;
                if (response.Error != ErrorCode.ERR_Success)
                {
                    throw new Exception($"location batch add failed: {response.Message}");
                }
            }
        }

        public static async ETTask RemoveLocation(this Entity self, int type)
        {
            await self.Root().GetComponent<LocationProxyComponent>().Remove(type, self.Id);
        }
    }
}