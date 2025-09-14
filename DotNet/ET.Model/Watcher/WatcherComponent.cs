using System.Collections.Generic;
using System.Diagnostics;

namespace ET
{
    [ComponentOf(typeof(Scene))]
    public class WatcherComponent: Entity, IAwake
    {
        public readonly Dictionary<int, System.Diagnostics.Process> Processes = new Dictionary<int, System.Diagnostics.Process>();
    }
}