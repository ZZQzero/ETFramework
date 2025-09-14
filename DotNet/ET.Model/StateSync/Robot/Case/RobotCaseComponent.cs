using System;
using System.Collections.Generic;

namespace ET
{
    
    [ComponentOf(typeof(Scene))]
    public class RobotCaseComponent: Entity, IAwake, IDestroy
    {
        public Dictionary<int, EntityRef<RobotCase>> RobotCases = new Dictionary<int, EntityRef<RobotCase>>();
        public int N = 10000;
    }
}