﻿namespace ET
{
	[MessageHandler(SceneType.StateSync)]
	public class M2C_PathfindingResultHandler : MessageHandler<Scene, M2C_PathfindingResult>
	{
		protected override async ETTask Run(Scene root, M2C_PathfindingResult message)
		{
			Log.Error($"M2C_PathfindingResultHandler {message.Position}  {message.Points.Count}");
			Unit unit = root.CurrentScene().GetComponent<UnitComponent>().Get(message.Id);

			float speed = unit.GetComponent<NumericComponent>().GetAsFloat(NumericType.Speed);

			await unit.GetComponent<MoveComponent>().MoveToAsync(message.Points, speed);
			await ETTask.CompletedTask;
		}
	}
}
