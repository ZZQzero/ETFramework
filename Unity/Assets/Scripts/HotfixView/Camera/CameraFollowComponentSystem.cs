using UnityEngine;
using Unity.Mathematics;
using Unity.Cinemachine;

namespace ET
{
    [EntitySystemOf(typeof(CameraFollowComponent))]
    public static partial class CameraFollowComponentSystem
    {
        [EntitySystem]
        private static void Awake(this CameraFollowComponent self)
        {
            // 查找场景中的Cinemachine虚拟相机
            var virtualCam = ResourcesLoadManager.Instance.LoadGameObjectSync("PlayerCamera");
            self.VirtualCamera = virtualCam.GetComponent<CinemachineCamera>();
            if (self.VirtualCamera == null)
            {
                Log.Error("场景中未找到CinemachineVirtualCamera，请确保场景中存在虚拟相机");
                return;
            }
            self.EnableFollow = true;
            // 初始化相机配置
            self.InitializeCamera();
        }
        
        /// <summary>
        /// 初始化相机位置和旋转
        /// </summary>
        private static void InitializeCamera(this CameraFollowComponent self)
        {
            if (self.VirtualCamera == null)
            {
                return;
            }
            self.Follow = self.VirtualCamera.GetComponent<CinemachineFollow>();
            self.RotationComposer = self.VirtualCamera.GetComponent<CinemachineRotationComposer>();
            if (self.Follow != null)
            {
                self.Follow.FollowOffset = new Vector3(0,self.FollowOffsetY,self.FollowOffsetZ);
            }
            if (self.RotationComposer != null)
            {
                self.RotationComposer.Damping = Vector2.one * 0.2f;
            }
        }
        
        [EntitySystem]
        private static void LateUpdate(this CameraFollowComponent self)
        {
            if (!self.EnableFollow || self.VirtualCamera == null)
            {
                return;
            }
            
            if(self.FollowTarget == null)
            {
                GameObjectComponent gameObjectComponent = self.FollowUnit.GetComponent<GameObjectComponent>();
                if (gameObjectComponent == null || gameObjectComponent.Transform == null)
                {
                    return;
                }
                self.FollowTarget = gameObjectComponent.Transform;
                if(self.FollowTarget == null)
                {
                    return;
                }

                self.VirtualCamera.Target.LookAtTarget = self.FollowTarget;
                self.VirtualCamera.Target.TrackingTarget = self.FollowTarget;
            }

            if (self.CharacterController != null)
            {
                // 检查是否有输入或正在移动（更精确的移动检测）
                bool isActive = (self.CharacterController.Input != null && self.CharacterController.Input.HasMoveInput()) ||
                               self.CharacterController.GetNormalizedAnimationSpeed() > 0.01f;

                if (isActive)
                {
                    self.Follow.TrackerSettings.PositionDamping = Vector3.one * 2.5f;
                    self.RotationComposer.Composition.DeadZone.Enabled = true;
                    self.RotationComposer.Composition.HardLimits.Enabled = true;
                }
                else
                {
                    // 角色停止时：降低阻尼，禁用Lookahead以确保正确归位
                    if (self.Follow.TrackerSettings.PositionDamping.x > 0.5f)
                    {
                        self.Follow.TrackerSettings.PositionDamping -= Vector3.one * (Time.deltaTime * self.DampingSmoothness);
                    }
                    if(self.Follow.TrackerSettings.PositionDamping.x < 0.5f)
                    {
                        self.Follow.TrackerSettings.PositionDamping = Vector3.one * 0.5f;
                    }
                }
            }
            else
            {
                self.CharacterController = self.FollowUnit.GetComponent<CharacterControllerComponent>();
            }
        }
        
        [EntitySystem]
        private static void Destroy(this CameraFollowComponent self)
        {
            // 清理引用
            if (self.FollowTarget != null)
            {
                Object.Destroy(self.FollowTarget.gameObject);
                self.FollowTarget = null;
            }
            self.VirtualCamera = null;
        }
    }
}

