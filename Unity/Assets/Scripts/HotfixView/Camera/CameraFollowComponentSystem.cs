using PrimeTween;
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
            self.InitializeCamera();
        }
        
        /// <summary>
        /// 初始化相机位置和旋转
        /// </summary>
        private static void InitializeCamera(this CameraFollowComponent self)
        {
            self.CameraFollowProxy = new GameObject("CameraFollowProxy");
            self.CameraFollowProxy.AddComponent<CameraFollowProxy>();
            self.Follow = self.VirtualCamera.GetComponent<CinemachineFollow>();
            if (self.Follow != null)
            {
                self.Follow.FollowOffset = new Vector3(0,self.FollowOffsetY,self.FollowOffsetZ);
                self.Follow.TrackerSettings.PositionDamping = new Vector3(0.6f, 1.0f, 0.6f);
            }
            self.CameraOffset = self.VirtualCamera.GetComponent<CinemachineCameraOffset>();
            if(self.CameraOffset != null)
            {
                self.CameraOffset.Offset = Vector3.zero;
            }

            self.BaseFov = self.VirtualCamera.Lens.FieldOfView;
        }
        
        [EntitySystem]
        private static void LateUpdate(this CameraFollowComponent self)
        {
            if (!self.EnableFollow || self.VirtualCamera == null)
            {
                return;
            }

            if (self.FollowTarget == null)
            {
                var goc = self.FollowUnit.GetComponent<GameObjectComponent>();
                if (goc == null) return;
                
                var proxy = self.CameraFollowProxy.GetComponent<CameraFollowProxy>();
                proxy.target = goc.Transform.Find("Target");
                proxy.fixedY = 0f;

                self.FollowTarget = proxy.transform;
                self.VirtualCamera.Target.TrackingTarget = self.FollowTarget;
            }
        }
        
        [EntitySystem]
        private static void Destroy(this CameraFollowComponent self)
        {
            // 清理引用
            if (self.FollowTarget != null)
            {
                Object.Destroy(self.FollowTarget.gameObject);
            }
            if (self.CameraFollowProxy != null)
            {
                Object.Destroy(self.CameraFollowProxy);
                self.CameraFollowProxy = null;
            }

            self.FollowTarget = null;
            self.VirtualCamera = null;
        }
        
        public static void SetCameraOffest(this CameraFollowComponent self,Vector3 offset)
        {
            self.CameraOffset.Offset = offset;
            Tween.Custom(self.CameraOffset.Offset, Vector3.zero, 0.25f, 
                value => self.CameraOffset.Offset = value);
        }

        public static void SetCameraFov(this CameraFollowComponent self, float fov)
        {
            self.VirtualCamera.Lens.FieldOfView = self.BaseFov + fov;
            Tween.Custom(self.VirtualCamera.Lens.FieldOfView,self.BaseFov, 0.25f,
                value => self.VirtualCamera.Lens.FieldOfView = value);
        }
    }
}

