using UnityEngine;
using YooAsset;

namespace ET
{
    public enum CodeMode
    {
        Client,
        Server,
        ClientServer,
    }
    
    public enum BuildType
    {
        Debug,
        Release,
    }
    
    [CreateAssetMenu(menuName = "ET/CreateGlobalConfig", fileName = "GlobalConfig", order = 0)]
    public class GlobalConfig: ScriptableObject
    {
        public CodeMode CodeMode;
        
        public EPlayMode PlayMode;

        public string Version;

        public string PackageName = "DefaultPackage";
        
        public string SceneName;

        public string IPAddress;
        
        public string ResourcePath;
    }
}