
using System.Collections.Generic;

namespace ET
{
    public class StartSceneConfigManager : Singleton<StartSceneConfigManager>,ISingletonAwake
    {
        private readonly MultiMap<int, StartSceneTable> processScenes = new();

        private readonly Dictionary<long, Dictionary<string, StartSceneTable>> zoneScenesByName = new();

        private readonly Dictionary<long, MultiMap<int, StartSceneTable>> zoneSceneByType = new();
        
        private readonly MultiMap<int, StartSceneTable> sceneByType = new();
        
        public List<StartSceneTable> GetByProcess(int process)
        {
            return this.processScenes[process];
        }
        
        public StartSceneTable GetBySceneName(int zone, string name)
        {
            return this.zoneScenesByName[zone][name];
        }
        
        public List<StartSceneTable> GetBySceneType(int zone, int type)
        {
            return this.zoneSceneByType[zone][type];
        }
        
        public List<StartSceneTable> GetBySceneType(int type)
        {
            return this.sceneByType[type];
        }
        
        public StartSceneTable GetOneBySceneType(int zone, int type)
        {
            return this.zoneSceneByType[zone][type][0];
        }

        private ActorId loginCenterActorId;
        public ActorId LoginCenterActorId => this.loginCenterActorId;

        public void Init()
        {
            foreach (StartSceneTable startSceneConfig in StartSceneConfig.Instance.GetDataList())
            {
                startSceneConfig.Init();
                sceneByType.Add(startSceneConfig.Type, startSceneConfig);
                this.processScenes.Add(startSceneConfig.Process, startSceneConfig);
                
                if (!this.zoneScenesByName.ContainsKey(startSceneConfig.Zone))
                {
                    this.zoneScenesByName.Add(startSceneConfig.Zone, new Dictionary<string, StartSceneTable>());
                }
                this.zoneScenesByName[startSceneConfig.Zone].Add(startSceneConfig.Name, startSceneConfig);
                
                if (!this.zoneSceneByType.ContainsKey(startSceneConfig.Zone))
                {
                    this.zoneSceneByType.Add(startSceneConfig.Zone, new MultiMap<int, StartSceneTable>());
                }
                this.zoneSceneByType[startSceneConfig.Zone].Add(startSceneConfig.Type, startSceneConfig);
            }
            
            var config = StartSceneConfig.Instance.Get(GlobalConstConfig.Data.LoginCenter);
            loginCenterActorId = config.ActorId;
        }

        public void Awake()
        {
            Init();
        }
    }
}
