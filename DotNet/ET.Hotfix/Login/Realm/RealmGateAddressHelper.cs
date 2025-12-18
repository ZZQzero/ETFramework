using System.Collections.Generic;


namespace ET
{
	public static partial class RealmGateAddressHelper
	{
		public static StartSceneTable GetGate(int zone, string account)
		{
			ulong hash = (ulong)account.GetLongHashCode();
			
			List<StartSceneTable> zoneGates = StartSceneConfigManager.Instance.GetBySceneType(zone, SceneType.Gate);
			
			return zoneGates[(int)(hash % (ulong)zoneGates.Count)];
		}
		
		public static StartSceneTable GetGate(int zone, long userId)
		{
			List<StartSceneTable> zoneGates = StartSceneConfigManager.Instance.GetBySceneType(zone, SceneType.Gate);
			var index = (int)(userId % zoneGates.Count);
			return zoneGates[index];
		}
	}
}
