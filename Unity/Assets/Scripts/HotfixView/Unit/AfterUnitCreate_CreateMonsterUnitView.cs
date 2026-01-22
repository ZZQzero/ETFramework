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
            unit.AddComponent<AnimatorComponent>();

            // 可选：如果后续你要做“打到怪物/选中怪物”，建议在 prefab 上挂 Collider，
            // 并在这里挂一个 UnitReference（MonoBehaviour）来反查 Unit。
            // var ur = go.GetComponent<UnitReference>() ?? go.AddComponent<UnitReference>();
            // ur.Unit = unit;

            await ETTask.CompletedTask;
        }
    }
}