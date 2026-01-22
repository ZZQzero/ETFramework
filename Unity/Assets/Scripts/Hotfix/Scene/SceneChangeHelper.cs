using System;

namespace ET
{
    public static partial class SceneChangeHelper
    {
        // 场景切换协程
        public static async ETTask SceneChangeTo(Scene root, string sceneName, long sceneInstanceId)
        {
            var currentScene = root;
            
            //添加UnitComponent（如果还没有）
            UnitComponent unitComponent = currentScene.GetComponent<UnitComponent>() ?? currentScene.AddComponent<UnitComponent>();
            
            //发布场景切换开始事件（可以在此时显示Loading界面）
            EventSystem.Instance.Publish(root, new SceneChangeStart() { SceneName = sceneName });
            
            //等待Unity场景加载完成
            Log.Info($"等待Unity场景加载完成: {sceneName}");
            Wait_UnitySceneLoaded waitUnitySceneLoaded = await root.GetComponent<ObjectWait>().Wait<Wait_UnitySceneLoaded>();
            if (waitUnitySceneLoaded.Error != ErrorCode.ERR_Success)
            {
                Log.Error($"Unity场景加载失败: {sceneName}, Error: {waitUnitySceneLoaded.Error}");
                return;
            }
            Log.Info($"Unity场景加载完成: {sceneName}");
            
            ETTask<Wait_CreateMyUnit> waitUnitTask = root.GetComponent<ObjectWait>().Wait<Wait_CreateMyUnit>();

            //通知服务端：场景已加载完成，请求发送Unit数据
            C2M_SceneLoadFinish request = C2M_SceneLoadFinish.Create();
            request.SceneName = sceneName;
            M2C_SceneLoadFinish response = await root.GetComponent<ClientSenderComponent>().Call(request) as M2C_SceneLoadFinish;
            
            if (response.Error != ErrorCode.ERR_Success)
            {
                Log.Error($"场景加载通知失败: {response.Error}, Message: {response.Message}");
                return;
            }
            Log.Info("服务端已收到场景加载完成通知");
            
            //等待服务端发送CreateMyUnit消息
            Log.Info("等待服务端发送Unit数据...");
            Wait_CreateMyUnit waitCreateMyUnit = await waitUnitTask;
            M2C_CreateMyUnit m2CCreateMyUnit = waitCreateMyUnit.Message;
            
            Log.Error($"{m2CCreateMyUnit.Unit.RoleConfigId}  {m2CCreateMyUnit.Unit.RoleId}  {m2CCreateMyUnit.Unit.EntityId}");
            //创建Unit（此时场景已完全准备好）
            Unit unit = UnitFactory.Create(currentScene, m2CCreateMyUnit.Unit);
            Log.Info($"Unit创建成功: {unit.EntityId}");
            
            //发布场景切换完成事件
            EventSystem.Instance.Publish(root, new SceneChangeFinish());
            
            //通知等待场景切换的协程
            root.GetComponent<ObjectWait>().Notify(new Wait_SceneChangeFinish());
        }
    }
}