
namespace ET
{
    public partial class GameRegister
    {
        public static void RegisterMessageSession()
        {
#if DOTNET
          MessageSessionDispatcher.Instance.RegisterMessageSession<C2G_LoginGateHandler>(SceneType.Gate); 
          MessageSessionDispatcher.Instance.RegisterMessageSession<C2G_PingHandler>(SceneType.Gate); 
          MessageSessionDispatcher.Instance.RegisterMessageSession<C2R_LoginHandler>(SceneType.Realm); 
          MessageSessionDispatcher.Instance.RegisterMessageSession<C2G_EnterMapHandler>(SceneType.Gate); 
#endif
        }
    }

}