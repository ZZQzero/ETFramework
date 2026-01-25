using UnityEngine;

namespace ET
{
    [Event(SceneType.Main)]
    public class AfterUnitCreate_CreateMonsterUnitView: AEvent<Scene, AfterUnitCreate>
    {
        protected override async ETTask Run(Scene scene, AfterUnitCreate args)
        {
            Unit unit = args.Unit;
            
            if (unit.UnitType() != UnitType.Monster)
            {
                return;
            }

            var monster = unit.GetComponent<MonsterIdentityComponent>();
            GameObject go = await ResourcesLoadManager.Instance.LoadGameObjectAsync(monster.MonsterTable.Prefab);
            go.transform.position = unit.Position;
            go.transform.localScale = Vector3.one;
            UnityEngine.Object.DontDestroyOnLoad(go);

            unit.AddComponent<GameObjectComponent>().GameObject = go;
            unit.AddComponent<CombatFeedbackComponent>();
            await ETTask.CompletedTask;
        }
    }
}