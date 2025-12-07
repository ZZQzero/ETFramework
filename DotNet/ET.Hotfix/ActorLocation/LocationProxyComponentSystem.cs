using System;
using System.Collections.Generic;

namespace ET
{
    public static partial class LocationProxyComponentSystem
    {
        private static ActorId GetLocationSceneId(this LocationProxyComponent self, long key)
        {
            List<StartSceneTable> locationConfigs = StartSceneConfigManager.Instance.GetBySceneType(self.Zone(), SceneType.Location);
            if (locationConfigs == null || locationConfigs.Count == 0)
            {
                throw new Exception($"Location server not found for zone: {self.Zone()}");
            }
            return locationConfigs[(int)(key % locationConfigs.Count)].ActorId;
        }

        public static async ETTask Add(this LocationProxyComponent self, int type, long key, ActorId actorId)
        {
            Log.Info($"location proxy add {key}, {actorId} {TimeInfo.Instance.ServerNow()}");
            ObjectAddRequest objectAddRequest = ObjectAddRequest.Create();
            objectAddRequest.Type = type;
            objectAddRequest.Key = key;
            objectAddRequest.ActorId = actorId;
            await self.Root().GetComponent<MessageSender>().Call(self.GetLocationSceneId(key), objectAddRequest);
        }

        public static async ETTask Lock(this LocationProxyComponent self, int type, long key, ActorId actorId, int time = 60000)
        {
            Log.Info($"location proxy lock {key}, {actorId} {TimeInfo.Instance.ServerNow()}");

            ObjectLockRequest objectLockRequest = ObjectLockRequest.Create();
            objectLockRequest.Type = type;
            objectLockRequest.Key = key;
            objectLockRequest.ActorId = actorId;
            objectLockRequest.Time = time;
            await self.Root().GetComponent<MessageSender>().Call(self.GetLocationSceneId(key), objectLockRequest);
        }

        public static async ETTask UnLock(this LocationProxyComponent self, int type, long key, ActorId oldActorId, ActorId newActorId)
        {
            Log.Info($"location proxy unlock {key}, {newActorId} {TimeInfo.Instance.ServerNow()}");
            ObjectUnLockRequest objectUnLockRequest = ObjectUnLockRequest.Create();
            objectUnLockRequest.Type = type;
            objectUnLockRequest.Key = key;
            objectUnLockRequest.OldActorId = oldActorId;
            objectUnLockRequest.NewActorId = newActorId;
            await self.Root().GetComponent<MessageSender>().Call(self.GetLocationSceneId(key), objectUnLockRequest);
        }

        public static async ETTask Remove(this LocationProxyComponent self, int type, long key)
        {
            Log.Info($"location proxy remove {key}, {TimeInfo.Instance.ServerNow()}");

            ObjectRemoveRequest objectRemoveRequest = ObjectRemoveRequest.Create();
            objectRemoveRequest.Type = type;
            objectRemoveRequest.Key = key;
            await self.Root().GetComponent<MessageSender>().Call(self.GetLocationSceneId(key), objectRemoveRequest);
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
            var sender = self.Root().GetComponent<MessageSender>();
            var response = (ObjectGetResponse) await sender.Call(self.GetLocationSceneId(key), objectGetRequest);
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
        public static async ETTask AddBatchLocation(this LocationProxyComponent self, List<(int type, long key, ActorId actorId)> items)
        {
            if (items == null || items.Count == 0)
            {
                return;
            }
            
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
            
            // 优化：如果只有1个Location服务器，直接await，避免WaitAll的开销
            // 典型场景中通常只有1个Location服务器，所以这个优化很重要
            if (groupedItems.Count == 1)
            {
                // 单个Location服务器，直接await（避免WaitAll的额外开销）
                var locationValue = groupedItems.First();
                ObjectAddBatchRequest batchRequest = ObjectAddBatchRequest.Create();
                foreach (var item in locationValue.Value)
                {
                    ObjectAddBatchItem batchItem = new ObjectAddBatchItem
                    {
                        Type = item.type,
                        Key = item.key,
                        ActorId = item.actorId
                    };
                    batchRequest.Items.Add(batchItem);
                }
                
                IResponse response = await self.Root().GetComponent<MessageSender>().Call(locationValue.Key, batchRequest);
                if (response == null)
                {
                    throw new Exception("location batch add failed: response is null");
                }
                if (response.Error != ErrorCode.ERR_Success)
                {
                    throw new Exception($"location batch add failed: {response.Message} (Error={response.Error})");
                }
            }
            else
            {
                // 多个Location服务器，使用WaitAll并行等待
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
                    
                    tasks.Add(self.Root().GetComponent<MessageSender>().Call(kvp.Key, batchRequest));
                }
                
                // 并行等待所有批量请求完成
                IResponse[] responses = await ETTaskHelper.WaitAll(tasks);
                
                // 检查所有响应的错误
                for (int i = 0; i < responses.Length; i++)
                {
                    IResponse response = responses[i];
                    if (response == null)
                    {
                        throw new Exception("location batch add failed: response is null");
                    }
                    if (response.Error != ErrorCode.ERR_Success)
                    {
                        throw new Exception($"location batch add failed: {response.Message} (Error={response.Error})");
                    }
                }
            }
        }

        public static async ETTask RemoveLocation(this Entity self, int type)
        {
            await self.Root().GetComponent<LocationProxyComponent>().Remove(type, self.Id);
        }
    }
}