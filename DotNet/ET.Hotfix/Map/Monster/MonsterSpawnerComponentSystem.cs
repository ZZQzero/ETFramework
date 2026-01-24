using System.Collections.Generic;
using Unity.Mathematics;

namespace ET
{
    [Invoke(TimerInvokeType.MonsterSpawnTimer)]
    public class MonsterSpawnTimer: ATimer<MonsterSpawnerComponent>
    {
        protected override void Run(MonsterSpawnerComponent t)
        {
        }
    }

    [EntitySystemOf(typeof(MonsterSpawnerComponent))]
    public static partial class MonsterSpawnerComponentSystem
    {
        [EntitySystem]
        private static void Awake(this MonsterSpawnerComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this MonsterSpawnerComponent self)
        {
        }

        public static void SpawnTick(this MonsterSpawnerComponent self)
        {
            if (self.Spawned)
            {
                return;
            }
            self.Spawned = true;

            Scene scene = self.GetParent<Scene>();

            List<MonsterTable> monsters = MonsterConfig.Instance.GetDataList();
            if (monsters == null || monsters.Count == 0)
            {
                Log.Warning($"MonsterConfig 为空：scene={scene.Name}");
                return;
            }

            // 先做最小闭环：把前 N 个怪物配置各刷 1 只，出生点围绕玩家初始点(-10,-10)
            const int maxSpawn = 10;
            int count = 0;
            foreach (MonsterTable cfg in monsters)
            {
                if (count >= maxSpawn)
                {
                    break;
                }

                float x = -12f - count * 1.5f;
                float z = -12f;
                float3 pos = new float3(x, 0f, z);

                UnitFactory.CreateMonster(scene, cfg, pos);
                ++count;
            }

            Log.Info($"刷怪完成：scene={scene.Name}, spawned={count}");
        }
    }
}

