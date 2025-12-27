using UnityEngine;

namespace ET
{
    /// <summary>
    /// 场景切换完成后初始化相机跟随
    /// </summary>
    [Event(SceneType.Main)]
    public class SceneChangeFinish_InitCameraFollow: AEvent<Scene, SceneChangeFinish>
    {
        protected override async ETTask Run(Scene scene, SceneChangeFinish args)
        {
            // 添加相机跟随组件
            CameraFollowComponent cameraComponent = scene.GetComponent<CameraFollowComponent>();
            if (cameraComponent == null)
            {
                cameraComponent = scene.AddComponent<CameraFollowComponent>();
            }
            
            // 查找玩家控制的Unit，设置为跟随目标
            UnitComponent unitComponent = scene.GetComponent<UnitComponent>();
            if (unitComponent != null)
            {
                // 通过UserComponent获取玩家UnitId
                UserComponent userComponent = scene.Root()?.GetComponent<UserComponent>();
                if (userComponent != null)
                {
                    Unit playerUnit = unitComponent.Get(userComponent.UserId);
                    if (playerUnit != null)
                    {
                        cameraComponent.FollowUnit = playerUnit;
                        Log.Info($"相机跟随已设置为Unit {playerUnit.Id}");
                    }
                    else
                    {
                        Log.Warning($"未找到玩家Unit {userComponent.CurrentRoleId}，相机跟随未设置");
                    }
                }
            }
            
            await ETTask.CompletedTask;
        }
    }
}

