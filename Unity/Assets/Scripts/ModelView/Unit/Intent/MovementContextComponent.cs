namespace ET
{
    public class MovementContextComponent : Entity, IAwake
    {
        private ComponentRef<CharacterControllerComponent> motorRef;
        private ComponentRef<CheckGroundedComponent> groundRef;
        private ComponentRef<LocomotionIntentComponent> locomotionIntentRef;

        public void Init(Unit unit)
        {
            motorRef = new ComponentRef<CharacterControllerComponent>(unit);
            groundRef = new ComponentRef<CheckGroundedComponent>(unit);
            locomotionIntentRef = new ComponentRef<LocomotionIntentComponent>(unit);
        }

        public CharacterControllerComponent Motor => this.motorRef.Get();
        public CheckGroundedComponent Ground => this.groundRef.Get();
        public LocomotionIntentComponent LocomotionIntent => this.locomotionIntentRef.Get();
    }
}