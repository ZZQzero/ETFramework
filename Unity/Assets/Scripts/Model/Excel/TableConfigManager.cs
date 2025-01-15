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
            //return new ByteBuf(textAsset.bytes);
            return ResourcesComponent.Instance.LoadConfigByte(file);
        }
    }
}