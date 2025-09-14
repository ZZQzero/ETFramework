using System.Collections.Generic;

namespace ET
{
    [ComponentOf(typeof(Scene))]
    public class RobotManagerComponent: Entity, IAwake, IDestroy
    {
        public HashSet<int> robots = new();
    }
}