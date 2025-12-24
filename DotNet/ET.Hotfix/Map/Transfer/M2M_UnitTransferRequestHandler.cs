using System;
using Unity.Mathematics;

namespace ET
{
    [MessageHandler(SceneType.Map)]
    public class M2M_UnitTransferRequestHandler: MessageHandler<Scene, M2M_UnitTransferRequest, M2M_UnitTransferResponse>
    {
        protected override async ETTask Run(Scene scene, M2M_UnitTransferRequest request, M2M_UnitTransferResponse response)
        {
			Unit unit = UnitFactory.Create(scene, request.UnitInfo.UnitId, UnitType.Player);
            unit.AddComponent<PathfindingComponent, string>(scene.Name);
            unit.Position = new float3(-10, 0, -10);

            unit.AddComponent<MailBoxComponent, int>(MailBoxType.OrderedMessage);
            // 通知客户端开始切场景
            M2C_StartSceneChange m2CStartSceneChange = M2C_StartSceneChange.Create();
            m2CStartSceneChange.SceneInstanceId = scene.InstanceId;
            m2CStartSceneChange.SceneName = scene.Name;
            MapMessageHelper.SendToClient(unit, m2CStartSceneChange);

            // 通知客户端创建My Unit
            M2C_CreateMyUnit m2CCreateUnits = M2C_CreateMyUnit.Create();
            m2CCreateUnits.Unit = UnitHelper.CreateUnitInfo(unit);
            MapMessageHelper.SendToClient(unit, m2CCreateUnits);

            // 注册Location：如果是首次创建（OldActorId为default），直接Add；否则UnLock（转移场景）
            LocationProxyComponent locationProxyComponent = scene.Root().GetComponent<LocationProxyComponent>();
            if (request.OldActorId == default)
            {
                // 首次创建，直接Add到Location
                await locationProxyComponent.Add(LocationType.Unit, unit.Id, unit.GetActorId());
            }
            else
            {
                // 转移场景，UnLock（需要先Lock）
                await locationProxyComponent.UnLock(LocationType.Unit, unit.Id, request.OldActorId, unit.GetActorId());
            }
        }
    }
}