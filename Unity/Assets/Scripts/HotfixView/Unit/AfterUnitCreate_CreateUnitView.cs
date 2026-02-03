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
            unit.UnitName = roleComponent.RoleTable.Name;
            unit.AddComponent<GameObjectComponent>().GameObject = go;
            unit.AddComponent<CombatContextComponent>();
            unit.AddComponent<MovementContextComponent>();
            unit.AddComponent<InputComponent>();
            unit.AddComponent<LocomotionIntentComponent>();
            unit.AddComponent<AttackCommandComponent>();
            unit.AddComponent<PlayerDriverComponent>();
            var animCatalog = unit.AddComponent<AnimationCatalogComponent>();
            animCatalog.Set(AnimationCatalogComponent.AnimKey.Locomotion_Move, roleComponent.RoleTable.MoveAsset);
            animCatalog.Set(AnimationCatalogComponent.AnimKey.Locomotion_Jump, roleComponent.RoleTable.JumpAsset);

            var attackCatalog = unit.AddComponent<AttackCatalogComponent>();
            attackCatalog.BasicAttackSkillId = roleComponent.RoleSkillSetTable.BasicAttackSkillId;
            attackCatalog.SkillIds.Clear();
            if (roleComponent.RoleSkillSetTable.SkillIds != null)
            {
                attackCatalog.SkillIds.AddRange(roleComponent.RoleSkillSetTable.SkillIds);
            }
            // 目标层：玩家攻击默认打 Enemy
            attackCatalog.TargetLayerMask = LayerMask.GetMask("Enemy");
            
            // MovementConfig：玩家可后续接 Numeric/装备/BUFF 合成，这里先使用默认值（商业级：执行层不读表）
            unit.AddComponent<MovementConfigComponent>();
            unit.AddComponent<CheckGroundedComponent,GameObject>(go);
            unit.AddComponent<CharacterControllerComponent,GameObject>(go);
            unit.AddComponent<AnimatorComponent>();
            unit.AddComponent<HitStopComponent>();
            unit.AddComponent<AirComboComponent>();
            unit.AddComponent<AttackComponent>();
            unit.AddComponent<HitReactionComponent,Transform>(go.transform);
            var unitReference = go.GetComponent<UnitReference>();
            if (unitReference == null)
            {
                unitReference = go.AddComponent<UnitReference>();
            }

            unitReference.Unit = unit;
            await ETTask.CompletedTask;
        }
    }
}