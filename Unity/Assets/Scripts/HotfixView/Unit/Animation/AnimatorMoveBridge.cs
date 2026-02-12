using UnityEngine;

namespace ET
{
    [RequireComponent(typeof(Animator))]
    public class AnimatorMoveBridge : MonoBehaviour
    {
        private Animator cachedAnimator;
        private UnitReference unitReference;

        private void Awake()
        {
            this.cachedAnimator = this.GetComponent<Animator>();
            this.unitReference = this.GetComponent<UnitReference>();
        }

        private void OnAnimatorMove()
        {
            if (FiberManager.Instance != null)
            {
                long targetInstanceId = 0;
                int targetFiberId = 0;

                Unit unit = this.unitReference != null ? this.unitReference.Unit : null;
                if (unit != null && !unit.IsDisposed)
                {
                    // 定向路由要命中 CharacterControllerComponent 的 InstanceId，
                    // 不能使用 Unit.InstanceId（队列里是组件实体）。
                    CharacterControllerComponent characterController = unit.GetComponent<CharacterControllerComponent>();
                    if (characterController != null && !characterController.IsDisposed)
                    {
                        targetInstanceId = characterController.InstanceId;
                    }
                    targetFiberId = unit.Fiber().Id;
                }

                FiberManager.Instance.OnAnimatorMove(targetInstanceId, targetFiberId);
            }
        }
    }
}