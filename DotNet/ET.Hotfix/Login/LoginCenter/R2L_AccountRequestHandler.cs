namespace ET;

[MessageHandler(SceneType.LoginCenter)]
public class R2L_AccountRequestHandler : MessageHandler<Scene,R2L_AccountRequest,L2R_AccountResponse>
{
    protected override async ETTask Run(Scene scene, R2L_AccountRequest request, L2R_AccountResponse response)
    {
        var accountId = request.Account.GetLongHashCode();
        var coroutineLock = scene.GetComponent<CoroutineLockComponent>();
        using (await coroutineLock.Wait(CoroutineLockType.LoginCenterAccount,accountId))
        {
            var loginInfo = scene.GetComponent<LoginInfoRecordComponent>();
            if (!loginInfo.IsExist(accountId))
            {
                return;
            }

            int zone = loginInfo.Get(accountId);
            var gateConfig = RealmGateAddressHelper.GetGate(zone, request.Account);
            L2G_DisconnectGateUnit l2GDisconnectGate = L2G_DisconnectGateUnit.Create();
            l2GDisconnectGate.AccountName = request.Account;
            var g2LDisconnectGate = (G2L_DisconnectGateUnit)await scene.GetComponent<MessageSender>().Call(gateConfig.ActorId, l2GDisconnectGate);
            response.Error = g2LDisconnectGate.Error;
        }
        await ETTask.CompletedTask;
    }
}