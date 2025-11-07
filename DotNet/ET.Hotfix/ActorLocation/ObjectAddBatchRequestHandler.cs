using System;
using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 批量Location注册Handler,减少网络往返次数
    /// </summary>
    [MessageHandler(SceneType.Location)]
    public class ObjectAddBatchRequestHandler: MessageHandler<Scene, ObjectAddBatchRequest, ObjectAddBatchResponse>
    {
        protected override async ETTask Run(Scene scene, ObjectAddBatchRequest request, ObjectAddBatchResponse response)
        {
            LocationManagerComoponent locationManager = scene.GetComponent<LocationManagerComoponent>();
            
            // 按LocationType分组，使用批量Add方法提高性能
            // 这样可以减少CoroutineLock的等待次数
            Dictionary<int, List<(long key, ActorId instanceId)>> groupedItems = new Dictionary<int, List<(long key, ActorId instanceId)>>();
            
            foreach (var item in request.Items)
            {
                if (!groupedItems.TryGetValue(item.Type, out var list))
                {
                    list = new List<(long key, ActorId instanceId)>();
                    groupedItems[item.Type] = list;
                }
                list.Add((item.Key, item.ActorId));
            }
            
            if (groupedItems.Count == 1)
            {
                var kvp = groupedItems.First();
                LocationOneType locationOneType = locationManager.Get(kvp.Key);
                await locationOneType.AddBatch(kvp.Value);
            }
            else
            {
                List<ETTask> tasks = new List<ETTask>();
                foreach (var kvp in groupedItems)
                {
                    LocationOneType locationOneType = locationManager.Get(kvp.Key);
                    tasks.Add(locationOneType.AddBatch(kvp.Value));
                }

                await ETTaskHelper.WaitAll(tasks);
            }
        }
    }
}