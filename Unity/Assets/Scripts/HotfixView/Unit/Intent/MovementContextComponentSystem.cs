namespace ET
{
    public static partial class MovementContextComponentSystem
    {
        [EntitySystem]
        public static void Awake(this MovementContextComponent self)
        {
            var unit = self.GetParent<Unit>();
            self.Init(unit);
        }
    }
}