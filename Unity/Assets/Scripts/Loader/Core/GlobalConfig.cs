using UnityEngine;
using YooAsset;

namespace ET
{
    [CreateAssetMenu(menuName = "ET/CreateGlobalConfig", fileName = "GlobalConfig", order = 0)]
    public class GlobalConfig: ScriptableObject
    {
        public EPlayMode PlayMode;

        public string Version = "V1.0.0";

        public string PackageName = "DefaultPackage";
        
        public string SceneName;

        public string IPAddress;
        
        public string ResourcePath;
        
        public bool IsDevelop;
    }
}