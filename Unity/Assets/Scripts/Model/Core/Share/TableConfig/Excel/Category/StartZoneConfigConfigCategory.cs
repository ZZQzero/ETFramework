﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
using System.Collections.Generic;

namespace ET
{
    public class StartZoneConfigConfigCategory : TableConfigCategoryBase<StartZoneConfig>
    {
        private static StartZoneConfigConfigCategory instance;
        public static StartZoneConfigConfigCategory Instance { get { return instance ??= new StartZoneConfigConfigCategory(); } }
        private StartZoneConfigConfigCategory() { }

        public override StartZoneConfig Get(int id)
        {
            return TableConfigManager.Instance.ConfigTables.TbStartZoneConfig.Get(id);
        }
        public override StartZoneConfig GetOrDefault(int id)
        {
            return TableConfigManager.Instance.ConfigTables.TbStartZoneConfig.GetOrDefault(id);
        }
        public override List<StartZoneConfig> GetDataList()
        {
            return TableConfigManager.Instance.ConfigTables.TbStartZoneConfig.DataList;
        }
    }   
}