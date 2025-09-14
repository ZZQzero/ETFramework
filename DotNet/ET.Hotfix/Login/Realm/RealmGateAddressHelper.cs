using System.Collections.Generic;


namespace ET
{
	public static partial class RealmGateAddressHelper
	{
		public static StartSceneConfig GetGate(int zone, string account)
		{
			ulong hash = (ulong)account.GetLongHashCode();
			
			List<StartSceneConfig> zoneGates = StartSceneConfigManager.Instance.GetBySceneType(zone, SceneType.Gate);
			
			return zoneGates[(int)(hash % (ulong)zoneGates.Count)];
		}
	}
}
