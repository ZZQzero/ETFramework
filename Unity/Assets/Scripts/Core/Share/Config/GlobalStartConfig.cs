using System;
using Nino.Core;

namespace ET
{
    
    [Serializable]
    [NinoType]
    public class GlobalStartConfig
    {
        public string Version;
        public string ResourcePath;
        public bool IsDevelop;
    }
}