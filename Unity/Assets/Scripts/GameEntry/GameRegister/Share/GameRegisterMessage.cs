
namespace ET
{
    public static partial class GameRegister
    {
        public static void RegisterMessage()
        {
#if UNITY
            MessageDispatcher.Instance.RegisterMessage<A2NetClient_MessageHandler>(SceneType.NetClient);
            MessageDispatcher.Instance.RegisterMessage<A2NetClient_RequestHandler>(SceneType.NetClient);
            MessageDispatcher.Instance.RegisterMessage<Main2NetClient_LoginHandler>(SceneType.NetClient);
            MessageDispatcher.Instance.RegisterMessage<NetClient2Main_SessionDisposeHandler>(SceneType.All);
            MessageDispatcher.Instance.RegisterMessage<M2C_PathfindingResultHandler>(SceneType.StateSync);
            MessageDispatcher.Instance.RegisterMessage<M2C_CreateMyUnitHandler>(SceneType.StateSync);
            MessageDispatcher.Instance.RegisterMessage<M2C_CreateUnitsHandler>(SceneType.StateSync);
            MessageDispatcher.Instance.RegisterMessage<M2C_RemoveUnitsHandler>(SceneType.StateSync);
            MessageDispatcher.Instance.RegisterMessage<M2C_StartSceneChangeHandler>(SceneType.StateSync);
            MessageDispatcher.Instance.RegisterMessage<M2C_StopHandler>(SceneType.StateSync);
#endif

#if DOTNET
            MessageDispatcher.Instance.RegisterMessage<ObjectAddRequestHandler>(SceneType.Location);
            MessageDispatcher.Instance.RegisterMessage<ObjectGetRequestHandler>(SceneType.Location);
            MessageDispatcher.Instance.RegisterMessage<ObjectLockRequestHandler>(SceneType.Location);
            MessageDispatcher.Instance.RegisterMessage<ObjectRemoveRequestHandler>(SceneType.Location);
            MessageDispatcher.Instance.RegisterMessage<ObjectUnLockRequestHandler>(SceneType.Location);
            MessageDispatcher.Instance.RegisterMessage<R2G_GetLoginKeyHandler>(SceneType.Gate);
            MessageDispatcher.Instance.RegisterMessage<A2NetInner_MessageHandler>(SceneType.NetInner);
            MessageDispatcher.Instance.RegisterMessage<A2NetInner_RequestHandler>(SceneType.NetInner);
            MessageDispatcher.Instance.RegisterMessage<C2M_TestRobotCaseHandler>(SceneType.Map);
            MessageDispatcher.Instance.RegisterMessage<G2M_SessionDisconnectHandler>(SceneType.Map);
            MessageDispatcher.Instance.RegisterMessage<C2M_PathfindingResultHandler>(SceneType.Map);
            MessageDispatcher.Instance.RegisterMessage<C2M_TransferMapHandler>(SceneType.Map);
            MessageDispatcher.Instance.RegisterMessage<M2M_UnitTransferRequestHandler>(SceneType.Map);
            MessageDispatcher.Instance.RegisterMessage<C2M_StopHandler>(SceneType.Map);
#endif
        }

        public static void RegisterHttp()
        {
#if DOTNET
            HttpDispatcher.Instance.HttpRegister<HttpGetRouterHandler>(SceneType.RouterManager, "/get_router");
#endif
        }
    }
}