namespace ET
{
    [MessageHandler(SceneType.Main)]
    public class M2C_PathfindingResultHandler : MessageHandler<Scene, M2C_PathfindingResult>
    {
        protected override async ETTask Run(Scene root, M2C_PathfindingResult message)
        {
            Unit unit = root.CurrentScene().GetComponent<UnitComponent>().Get(message.Id);
            if (unit == null)
            {
                return;
            }

            float speed = unit.GetComponent<NumericComponent>().GetAsFloat(NumericType.Speed);

            MoveComponent move = unit.GetComponent<MoveComponent>();
            if (move == null)
            {
                // 运动系统重构后：部分单位可能不再挂载 MoveComponent（改用 Intent → Motor）。
                // 为避免旧消息链路导致空引用，这里直接忽略。
                return;
            }

            await move.MoveToAsync(message.Points, speed);
            await ETTask.CompletedTask;
        }
    }
}