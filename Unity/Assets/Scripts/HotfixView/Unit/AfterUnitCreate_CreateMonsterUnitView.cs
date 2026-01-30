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
            unit.UnitName = monster.MonsterTable.Name;
            unit.AddComponent<GameObjectComponent>().GameObject = go;
            unit.AddComponent<LocomotionIntentComponent>();
            unit.AddComponent<AttackCommandComponent>();
            unit.AddComponent<AIDriverComponent>();
            unit.AddComponent<SimpleMonsterAIComponent>();
            var animCatalog = unit.AddComponent<AnimationCatalogComponent>();
            animCatalog.Set(AnimationCatalogComponent.AnimKey.Locomotion_Move, monster.MonsterTable.MoveAsset);
            animCatalog.Set(AnimationCatalogComponent.AnimKey.Locomotion_Jump, monster.MonsterTable.JumpAsset);

            var attackCatalog = unit.AddComponent<AttackCatalogComponent>();
            attackCatalog.SkillIds.Clear();
            if (monster.MonsterTable.Skills != null)
            {
                attackCatalog.SkillIds.AddRange(monster.MonsterTable.Skills);
            }
            if (attackCatalog.SkillIds.Count > 0)
            {
                // 约定：第0个为基础攻击（后续可改为表字段/AI配置映射）
                attackCatalog.BasicAttackSkillId = attackCatalog.SkillIds[0];
            }
            else
            {
                Log.Error("Monster 未配置技能列表（AttackCatalog.SkillIds 为空）");
            }
            // 目标层：怪物攻击默认打 Player（项目里若没有 Player layer，请在 Layer 配置里补上）
            attackCatalog.TargetLayerMask = LayerMask.GetMask("Player");

            // MovementConfig：把怪物表里的 Speed 合成到最终执行参数
            var moveConfig = unit.AddComponent<MovementConfigComponent>();
            if (monster.MonsterTable.Speed > 0.01f)
            {
                moveConfig.MoveSpeed = monster.MonsterTable.Speed;
            }
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