
namespace ET
{
	[MessageHandler(SceneType.Map)]
	public class C2M_PathfindingResultHandler : MessageLocationHandler<Unit, C2M_PathfindingResult>
	{
		protected override async ETTask Run(Unit unit, C2M_PathfindingResult message)
		{
			Log.Debug($"C2M_PathfindingResultHandler {message.Position}");
			unit.FindPathMoveToAsync(message.Position).NoContext();
			await ETTask.CompletedTask;
		}
	}
}