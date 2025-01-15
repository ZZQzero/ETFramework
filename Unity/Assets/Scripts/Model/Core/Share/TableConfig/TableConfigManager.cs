using System.IO;
using Luban;

namespace ET
{
    public class TableConfigManager : Singleton<TableConfigManager>,ISingletonAwake
    {
        public Tables ConfigTables;
        public void Awake()
        {
            ConfigTables = new ET.Tables(LoadByteBuf);
        }
        
        private ByteBuf LoadByteBuf(string file)
        {
#if UNITY
            return ResourcesComponent.Instance.LoadConfigByte(file);
#endif
#if DOTNET
            string configFilePath = $"Unity/Assets/Config/Excel/Gen/Bytes/{file}.bytes";
            return new ByteBuf(File.ReadAllBytes(configFilePath));
#endif
        }
    }
}