namespace ET
{
	[MessageHandler(SceneType.StateSync)]
	public class M2C_CreateUnitsHandler: MessageHandler<Scene, M2C_CreateUnits>
	{
		protected override async ETTask Run(Scene root, M2C_CreateUnits message)
		{
			Scene currentScene = root.CurrentScene();
			UnitComponent component = currentScene.GetComponent<UnitComponent>();
			
			foreach (UnitInfo unitInfo in message.Units)
			{
				if (component.Get(unitInfo.UnitId) != null)
				{
					continue;
				}
				Unit unit = UnitFactory.Create(currentScene, unitInfo);
			}
			await ETTask.CompletedTask;
		}
	}
}
