using Unity.Cinemachine;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// 相机跟随组件
    /// 使用Cinemachine虚拟相机
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class CameraFollowComponent: Entity, IAwake, ILateUpdate, IDestroy
    {
        /// <summary>
        /// Cinemachine虚拟相机引用
        /// </summary>
        public CinemachineCamera VirtualCamera { get; set; }
        
        public CinemachineFollow Follow { get; set; }
        public CharacterControllerComponent CharacterController { get; set; }

        public CinemachineCameraOffset CameraOffset { get; set; }
        public Unit FollowUnit { get; set; }
        /// <summary>
        /// Follow目标（用于Cinemachine）
        /// </summary>
        public Transform FollowTarget { get; set; }
        public GameObject CameraFollowProxy { get; set; }
        
        public float BaseFov { get; set; } = 60f;
        
        /// <summary>
        /// 相机X轴偏移
        /// </summary>
        public float FollowOffsetY { get; set; } = 12f;
        
        /// <summary>
        /// 相机Z轴偏移
        /// </summary>
        public float FollowOffsetZ { get; set; } = -12f;
        
        /// <summary>
        /// 是否启用跟随
        /// </summary>
        public bool EnableFollow { get; set; } = true;
    }
}