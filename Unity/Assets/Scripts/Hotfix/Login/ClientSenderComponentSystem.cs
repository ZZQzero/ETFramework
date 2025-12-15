using System.Threading.Tasks;

namespace ET
{
    [EntitySystemOf(typeof(ClientSenderComponent))]
    public static partial class ClientSenderComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ClientSenderComponent self,Fiber fiber)
        {
            self.fiberId = fiber.Id;
            self.netClientActorId = new ActorId(Options.Instance.Process, SceneType.NetClient);
        }
        
        [EntitySystem]
        private static void Destroy(this ClientSenderComponent self)
        {
            self.RemoveFiberAsync().NoContext();
        }

        private static async ETTask RemoveFiberAsync(this ClientSenderComponent self)
        {
            if (self.fiberId == 0)
            {
                return;
            }

            int fiberId = self.fiberId;
            self.fiberId = 0;
            await FiberManager.Instance.Remove(fiberId);
        }

        public static async ETTask DisposeAsync(this ClientSenderComponent self)
        {
            await self.RemoveFiberAsync();
            self.Dispose();
        }

        public static async ETTask<NetClient2Main_Login> LoginAsync(this ClientSenderComponent self, string address, string account, string password)
        {
            /*var fiber = await FiberManager.Instance.Create(SchedulerType.Main, 0, SceneType.NetClient, "");
            self.fiberId = fiber.Id;
            self.netClientActorId = new ActorId(self.Fiber().Process, self.fiberId);*/
            
            Main2NetClient_Login main2NetClientLogin = Main2NetClient_Login.Create();
            main2NetClientLogin.OwnerFiberId = self.Fiber().Id;
            main2NetClientLogin.Account = account;
            main2NetClientLogin.Password = password;
            main2NetClientLogin.Address = address;
            NetClient2Main_Login response = await self.Root().GetComponent<ProcessInnerSender>().Call(self.netClientActorId, main2NetClientLogin) as NetClient2Main_Login;
            return response;
        }

        public static async ETTask<NetClient2Main_LoginGame> LoginGameAsync(this ClientSenderComponent self, string account, long key, long userId, long roleId, string address)
        {
            Main2NetClient_LoginGame main2NetClientLoginGame = Main2NetClient_LoginGame.Create();
            main2NetClientLoginGame.RealmKey = key;
            main2NetClientLoginGame.Account = account;
            main2NetClientLoginGame.UserId = userId;
            main2NetClientLoginGame.RoleId = roleId;
            main2NetClientLoginGame.GateAddress = address;
            var response = await self.Root().GetComponent<ProcessInnerSender>().Call(self.netClientActorId, main2NetClientLoginGame) as NetClient2Main_LoginGame;
            return response;
        }
       
        public static void Send(this ClientSenderComponent self, IMessage message)
        {
            A2NetClient_Message a2NetClientMessage = A2NetClient_Message.Create(true);
            a2NetClientMessage.MessageObject = message;
            Log.Error($"A2NetClient_Message  {a2NetClientMessage.MessageObject}");
            self.Root().GetComponent<ProcessInnerSender>().Send(self.netClientActorId, a2NetClientMessage);
        }

        public static async ETTask<IResponse> Call(this ClientSenderComponent self, IRequest request, bool needException = true)
        {
            A2NetClient_Request a2NetClientRequest = A2NetClient_Request.Create();
            a2NetClientRequest.MessageObject = request;
            using A2NetClient_Response a2NetClientResponse = await self.Root().GetComponent<ProcessInnerSender>().Call(self.netClientActorId, a2NetClientRequest) as A2NetClient_Response;
            IResponse response = a2NetClientResponse.MessageObject;
                        
            if (response.Error == ErrorCode.ERR_MessageTimeout)
            {
                throw new RpcException(response.Error, $"Rpc error: request, 注意Actor消息超时，请注意查看是否死锁或者没有reply: {request}, response: {response}");
            }

            if (needException && ErrorCode.IsRpcNeedThrowException(response.Error))
            {
                throw new RpcException(response.Error, $"Rpc error: {request}, response: {response}");
            }
            return response;
        }

    }
}