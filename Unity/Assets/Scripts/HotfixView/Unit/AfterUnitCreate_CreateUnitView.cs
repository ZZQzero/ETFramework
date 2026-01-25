using UnityEngine;

namespace ET
{
    [Event(SceneType.Main)]
    public class AfterUnitCreate_CreateUnitView: AEvent<Scene, AfterUnitCreate>
    {
        protected override async ETTask Run(Scene scene, AfterUnitCreate args)
        {
            Unit unit = args.Unit;
            if (unit.UnitType() != UnitType.Player)
            {
                return;
            }
            var roleComponent = unit.GetComponent<RoleIdentityComponent>();
            GameObject go = await ResourcesLoadManager.Instance.LoadGameObjectAsync(roleComponent.RoleTable.Prefab);
            go.transform.position = unit.Position;
            go.transform.localScale = Vector3.one;
            UnityEngine.Object.DontDestroyOnLoad(go);
            unit.AddComponent<GameObjectComponent>().GameObject = go;
            unit.AddComponent<InputComponent,Transform>(go.transform);
            unit.AddComponent<CheckGroundedComponent,GameObject>(go);
            unit.AddComponent<CombatFeedbackComponent>();
            unit.AddComponent<AttackComponent>();
            unit.AddComponent<CharacterControllerComponent,GameObject>(go);
            unit.AddComponent<HitReactionComponent,Transform>(go.transform);
            unit.AddComponent<AnimatorComponent>();
            await ETTask.CompletedTask;
        }
    }
}