
using System.Collections.Generic;

namespace ET
{
    public class StartSceneConfigManager : Singleton<StartSceneConfigManager>,ISingletonAwake
    {
        private readonly MultiMap<int, StartSceneConfig> processScenes = new();

        private readonly Dictionary<long, Dictionary<string, StartSceneConfig>> zoneScenesByName = new();

        private readonly Dictionary<long, MultiMap<int, StartSceneConfig>> zoneSceneByType = new();
        
        private readonly MultiMap<int, StartSceneConfig> sceneByType = new();
        
        public List<StartSceneConfig> GetByProcess(int process)
        {
            return this.processScenes[process];
        }
        
        public StartSceneConfig GetBySceneName(int zone, string name)
        {
            return this.zoneScenesByName[zone][name];
        }
        
        public List<StartSceneConfig> GetBySceneType(int zone, int type)
        {
            return this.zoneSceneByType[zone][type];
        }
        
        public List<StartSceneConfig> GetBySceneType(int type)
        {
            return this.sceneByType[type];
        }
        
        public StartSceneConfig GetOneBySceneType(int zone, int type)
        {
            return this.zoneSceneByType[zone][type][0];
        }
        
        public void Init()
        {
            foreach (StartSceneConfig startSceneConfig in StartSceneConfigConfigCategory.Instance.GetDataList())
            {
                startSceneConfig.Init();
                sceneByType.Add(startSceneConfig.Type, startSceneConfig);
                this.processScenes.Add(startSceneConfig.Process, startSceneConfig);
                
                if (!this.zoneScenesByName.ContainsKey(startSceneConfig.Zone))
                {
                    this.zoneScenesByName.Add(startSceneConfig.Zone, new Dictionary<string, StartSceneConfig>());
                }
                this.zoneScenesByName[startSceneConfig.Zone].Add(startSceneConfig.Name, startSceneConfig);
                
                if (!this.zoneSceneByType.ContainsKey(startSceneConfig.Zone))
                {
                    this.zoneSceneByType.Add(startSceneConfig.Zone, new MultiMap<int, StartSceneConfig>());
                }
                this.zoneSceneByType[startSceneConfig.Zone].Add(startSceneConfig.Type, startSceneConfig);
            }
        }

        public void Awake()
        {
            Init();
        }
    }
}
