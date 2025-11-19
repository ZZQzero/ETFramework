namespace ET
{
    [ComponentOf(typeof(Scene))]
    public class ClientSenderComponent: Entity, IAwake<Fiber>, IDestroy
    {
        public int fiberId;

        public ActorId netClientActorId;
    }
}