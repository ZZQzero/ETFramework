namespace ET
{
    public static partial class AirComboComponentSystem
    {
        [EntitySystem]
        private static void Awake(this AirComboComponent self)
        {
            self.Active = false;
            self.IsExiting = false;
            self.AirEndCombatMs = 0;
        }

        public static void Enter(
            this AirComboComponent self,
            long hitStunEndTimeMs,
            in HitAirComboProfile comboProfile)
        {
            if (self == null || self.IsDisposed)
                return;

            if (!comboProfile.Enable)
                return;

            self.Active = true;
            self.IsExiting = false;
            self.AirEndCombatMs = hitStunEndTimeMs + comboProfile.MaxAirOffsetMs;
        }

        public static void OnHit(
            this AirComboComponent self,
            long hitStunEndTimeMs,
            in HitAirComboProfile comboProfile)
        {
            if (self == null || self.IsDisposed || !self.Active)
                return;

            // 命中续期：取消退出
            if (self.IsExiting)
            {
                self.IsExiting = false;
            }

            self.AirEndCombatMs = hitStunEndTimeMs + comboProfile.MaxAirOffsetMs;
        }

        public static void BeginExit(this AirComboComponent self, long nowCombatMs)
        {
            if (self == null || self.IsDisposed || !self.Active || self.IsExiting)
                return;

            self.IsExiting = true;
        }

        public static void ForceEnd(this AirComboComponent self)
        {
            if (self == null || self.IsDisposed || !self.Active)
                return;

            self.Active = false;
            self.IsExiting = false;
            self.AirEndCombatMs = 0;
        }
    }
}