using UnityEngine;
using UnityEngine.Serialization;

namespace ET
{
    /// <summary>
    /// Unit引用组件 - 挂载在GameObject上，用于物理检测时获取对应的Unit
    /// </summary>
    public class UnitReference : MonoBehaviour
    {
        private Unit _unit;

        public Unit Unit
        {
            get => _unit;
            set => _unit = value;
        }

        public GameObject target;
        public GameObject weaponPoint;
        public GameObject weapon;

        private void OnDestroy()
        {
            _unit = null;
        }
    }
}