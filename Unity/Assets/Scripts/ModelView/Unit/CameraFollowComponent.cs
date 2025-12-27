using Unity.Cinemachine;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// 相机跟随组件
    /// 使用Cinemachine虚拟相机实现类似王者荣耀的俯视角跟随
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class CameraFollowComponent: Entity, IAwake, ILateUpdate, IDestroy
    {
        /// <summary>
        /// Cinemachine虚拟相机引用
        /// </summary>
        public CinemachineCamera VirtualCamera { get; set; }
        
        public CinemachineFollow Follow { get; set; }
        
        public CinemachineRotationComposer RotationComposer { get; set; }
        
        public CharacterControllerComponent CharacterController { get; set; }

        public Unit FollowUnit { get; set; }
        /// <summary>
        /// Follow目标（用于Cinemachine）
        /// </summary>
        public Transform FollowTarget { get; set; }
        
        /// <summary>
        /// 相机X轴偏移
        /// </summary>
        public float FollowOffsetY { get; set; } = 10f;
        
        /// <summary>
        /// 相机Z轴偏移
        /// </summary>
        public float FollowOffsetZ { get; set; } = -10f;
        
        public float DampingSmoothness {get; set; } = 5f;
        
        /// <summary>
        /// 是否启用跟随
        /// </summary>
        public bool EnableFollow { get; set; } = true;
    }
}