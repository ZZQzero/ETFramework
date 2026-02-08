using System.Diagnostics;
using Unity.Mathematics;

namespace ET
{
    [ChildOf(typeof(UnitComponent))]
    [DebuggerDisplay("ViewName,nq")]
    public partial class Unit: Entity, IAwake<int>
    {
        public UnitTable UnitTable { get; set; }

        public string UnitName;

        private float3 position; //坐标
        public float3 Position
        {
            get => this.position;
            set => this.position = value;
            /*float3 oldPos = this.position;
                this.position = value;
                EventSystem.Instance.Publish(this.Scene(), new ChangePosition() { Unit = this, OldPos = oldPos });*/
        }

        public float3 Forward
        {
            get => math.mul(this.Rotation, math.forward());
            set => this.Rotation = quaternion.LookRotation(value, math.up());
        }
        
        private quaternion rotation;
        
        public quaternion Rotation
        {
            get => this.rotation;
            set => this.rotation = value;
            //EventSystem.Instance.Publish(this.Scene(), new ChangeRotation() { Unit = this });
        }
    }
}