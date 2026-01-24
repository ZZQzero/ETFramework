using System;
using Unity.Mathematics;

namespace ET
{
    [MessageHandler(SceneType.Map)]
    public class M2M_UnitTransferRequestHandler: MessageHandler<Scene, M2M_UnitTransferRequest, M2M_UnitTransferResponse>
    {
        protected override async ETTask Run(Scene scene, M2M_UnitTransferRequest request, M2M_UnitTransferResponse response)
        {
            Unit unit = UnitFactory.CreatePlayer(scene, request.UnitInfo);
            unit.AddComponent<PathfindingComponent, string>(scene.Name);
            unit.Position = new float3(-10, 0, -10);

            unit.AddComponent<MailBoxComponent, int>(MailBoxType.OrderedMessage);

            var monster = scene.GetComponent<MonsterSpawnerComponent>();
            monster.SpawnTick();
            // 通知客户端开始切场景
            M2C_StartSceneChange m2CStartSceneChange = M2C_StartSceneChange.Create();
            m2CStartSceneChange.SceneInstanceId = scene.InstanceId;
            m2CStartSceneChange.SceneName = scene.Name;
            MapMessageHelper.SendToClient(unit, m2CStartSceneChange);

            // ⚠️ 注意：不再立即发送M2C_CreateMyUnit
            // 等待客户端场景加载完成后，在C2M_SceneLoadFinishHandler中发送

            // 注册Location：如果是首次创建（OldActorId为default），直接Add；否则UnLock（转移场景）
            LocationProxyComponent locationProxyComponent = scene.Root().GetComponent<LocationProxyComponent>();
            if (request.OldActorId == default)
            {
                // 首次创建，直接Add到Location
                await locationProxyComponent.Add(LocationType.Unit, unit.EntityId, unit.GetActorId());
            }
            else
            {
                // 转移场景，UnLock（需要先Lock）
                await locationProxyComponent.UnLock(LocationType.Unit, unit.EntityId, request.OldActorId, unit.GetActorId());
            }
        }
    }
}