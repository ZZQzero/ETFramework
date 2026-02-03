namespace ET
{
    public static partial class CombatContextComponentSystem
    {
        [EntitySystem]
        public static void Awake(this CombatContextComponent self)
        {
            var unit = self.GetParent<Unit>();
            self.Init(unit);
        }
    }
}