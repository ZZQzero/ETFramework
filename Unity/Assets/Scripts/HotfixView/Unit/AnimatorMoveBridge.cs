using UnityEngine;

namespace ET
{
    [RequireComponent(typeof(Animator))]
    public class AnimatorMoveBridge : MonoBehaviour
    {
        private void OnAnimatorMove()
        {
            if (FiberManager.Instance != null)
            {
                FiberManager.Instance.OnAnimatorMove();
            }
        }
    }
}