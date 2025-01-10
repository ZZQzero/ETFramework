using UnityEngine;

namespace ET
{
    [ComponentOf(typeof(Scene))]
    public class GlobalComponent: Entity, IAwake
    {
        /*public Transform Global;
        public Transform UI;*/
        /*public Transform Unit { get; set; }*/

        public GlobalConfig GlobalConfig { get; set; }
    }
}