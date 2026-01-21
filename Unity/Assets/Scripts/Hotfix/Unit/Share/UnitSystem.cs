namespace ET
{
    [EntitySystemOf(typeof(Unit))]
    public static partial class UnitSystem
    {
        [EntitySystem]
        private static void Awake(this Unit self, int configId)
        {
            self.UnitTable = UnitConfig.Instance.Get(configId);
        }
        
        public static int Type(this Unit self)
        {
            return self.UnitTable.UnitType;
        }
    }
}