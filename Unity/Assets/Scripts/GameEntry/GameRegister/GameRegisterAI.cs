using ET;

namespace ET
{
    public static partial class GameRegister
    {
        public static void RegisterAI()
        {
#if UNITY
            AIDispatcherSingle.Instance.RegisterAI<AI_Attack>();
            AIDispatcherSingle.Instance.RegisterAI<AI_XunLuo>();
#endif
            
        }
    }
}