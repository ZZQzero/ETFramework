using UnityEngine;

namespace ET
{
    public class GlobalConfigManager : Singleton<GlobalConfigManager>, ISingletonAwake
    {
        public GlobalConfig Config { set; get; }
        public void Awake()
        {
            Config = Resources.Load<GlobalConfig>("GlobalConfig");
        }

        protected override void Destroy()
        {
            base.Destroy();
            Config = null;
        }
    }
}