
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using Luban;


namespace ET
{
public partial class TbStartZoneConfig
{
    private readonly System.Collections.Generic.Dictionary<int, StartZoneConfig> _dataMap;
    private readonly System.Collections.Generic.List<StartZoneConfig> _dataList;
    
    public TbStartZoneConfig(ByteBuf _buf)
    {
        _dataMap = new System.Collections.Generic.Dictionary<int, StartZoneConfig>();
        _dataList = new System.Collections.Generic.List<StartZoneConfig>();
        
        for(int n = _buf.ReadSize() ; n > 0 ; --n)
        {
            StartZoneConfig _v;
            _v = StartZoneConfig.DeserializeStartZoneConfig(_buf);
            _dataList.Add(_v);
            _dataMap.Add(_v.Id, _v);
        }
    }

    public System.Collections.Generic.Dictionary<int, StartZoneConfig> DataMap => _dataMap;
    public System.Collections.Generic.List<StartZoneConfig> DataList => _dataList;

    public StartZoneConfig GetOrDefault(int key) => _dataMap.TryGetValue(key, out var v) ? v : null;
    public StartZoneConfig Get(int key) => _dataMap[key];
    public StartZoneConfig this[int key] => _dataMap[key];

    public void ResolveRef(Tables tables)
    {
        foreach(var _v in _dataList)
        {
            _v.ResolveRef(tables);
        }
    }

}

}

