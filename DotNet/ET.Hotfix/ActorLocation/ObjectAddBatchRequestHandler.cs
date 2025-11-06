using System;
using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 批量Location注册Handler（性能优化：减少网络往返次数）
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
            
            // 并行处理所有LocationType的批量添加
            List<ETTask> tasks = new List<ETTask>();
            foreach (var kvp in groupedItems)
            {
                LocationOneType locationOneType = locationManager.Get(kvp.Key);
                tasks.Add(locationOneType.AddBatch(kvp.Value));
            }
            
            // 等待所有批量操作完成
            foreach (var task in tasks)
            {
                await task;
            }
            
            Log.Info($"location batch add completed: count={request.Items.Count}, types={groupedItems.Count}");
        }
    }
}

