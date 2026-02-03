namespace ET
{
    public class CombatContextComponent : Entity,IAwake
    {
        private ComponentRef<AttackComponent> attackRef;
        private ComponentRef<AttackCommandComponent> attackCommandRef;
        private ComponentRef<AttackCatalogComponent> attackCatalogRef;
        private ComponentRef<HitReactionComponent> hitReactionComponent;
        private ComponentRef<HitStopComponent> hitStopRef;
        private ComponentRef<AirComboComponent> airComboRef;

        public void Init(Unit unit)
        {
            attackRef = new ComponentRef<AttackComponent>(unit);
            attackCommandRef = new ComponentRef<AttackCommandComponent>(unit);
            attackCatalogRef = new ComponentRef<AttackCatalogComponent>(unit);
            hitReactionComponent = new ComponentRef<HitReactionComponent>(unit);
            hitStopRef = new ComponentRef<HitStopComponent>(unit);
            airComboRef = new ComponentRef<AirComboComponent>(unit);
        }
        
        public AttackComponent Attack => this.attackRef.Get();
        public AttackCommandComponent AttackCommand => this.attackCommandRef.Get();
        public AttackCatalogComponent AttackCatalog => this.attackCatalogRef.Get();
        public HitStopComponent HitStop => this.hitStopRef.Get();
        public AirComboComponent AirCombo => this.airComboRef.Get();
        public HitReactionComponent HitReaction => this.hitReactionComponent.Get();
    }
}