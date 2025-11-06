using System;
using System.Collections.Generic;


namespace ET
{
    public class C2G_LoginGateHandler : MessageSessionHandler<C2G_LoginGate, G2C_LoginGate>
    {
        protected override async ETTask Run(Session session, C2G_LoginGate request, G2C_LoginGate response)
        {
            Scene root = session.Root();
            string account = root.GetComponent<GateSessionKeyComponent>().Get(request.Key);
            if (account == null)
            {
                response.Error = ErrorCode.ERR_ConnectGateKeyError;
                response.Message = "Gate key验证失败!";
                return;
            }
            
            session.RemoveComponent<SessionAcceptTimeoutComponent>();

            PlayerComponent playerComponent = root.GetComponent<PlayerComponent>();
            Player player = playerComponent.GetByAccount(account);
            if (player == null)
            {
                player = playerComponent.AddChild<Player, string>(account);
                playerComponent.Add(player);
                PlayerSessionComponent playerSessionComponent = player.AddComponent<PlayerSessionComponent>();
                playerSessionComponent.AddComponent<MailBoxComponent, int>(MailBoxType.GateSession);
                
                // 优化：使用批量Location注册接口，减少网络往返次数（预期节省50-60ms）
                // 注意：MailBoxComponent必须在AddLocation之前添加，因为AddLocation需要ActorId
                player.AddComponent<MailBoxComponent, int>(MailBoxType.UnOrderedMessage);
                
                // 使用批量注册接口，一次RPC注册两个Location（GateSession和Player）
                // 这样可以减少网络往返次数从2次→1次，进一步提升性能
                List<(int type, long key, ActorId actorId)> batchItems = new List<(int type, long key, ActorId actorId)>
                {
                    (LocationType.GateSession, playerSessionComponent.Id, playerSessionComponent.GetActorId()),
                    (LocationType.Player, player.Id, player.GetActorId())
                };
                await root.GetComponent<LocationProxyComponent>().AddBatch(batchItems);
			
                session.AddComponent<SessionPlayerComponent>().Player = player;
                playerSessionComponent.Session = session;
            }
            else
            {
                throw new Exception("not write");
            }

            response.PlayerId = player.Id;
            await ETTask.CompletedTask;
        }
    }
}