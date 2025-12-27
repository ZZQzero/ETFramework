using System;
using UnityEngine;

namespace ET
{
    #region 数据结构
    
    public enum GroundState : byte
    {
        Grounded,         // 稳定站立
        OnStableSlope,    // 可站立斜坡
        OnUnstableSlope,  // 滑落斜坡
        OnEdge,           // 平台边缘
        Airborne,         // 空中（上升或平飞）
        Falling,          // 下落
        Landing           // 落地缓冲
    }

    public enum AirborneReason : byte
    {
        None,
        Jump,
        WalkOff,
        Launched,
        Juggled,
        Knockdown
    }

    public enum GroundSurfaceType : byte
    {
        Default,
        Ice,
        Sand,
        Water,
        Mud,
        Bounce
    }

    [Serializable]
    public struct GroundHitInfo
    {
        public bool HasGround;
        public bool IsStable;
        public Vector3 Point;
        public Vector3 Normal;
        public float Distance;
        public float SlopeAngle;
        public float SupportRatio;
        public bool IsOnEdge;
        public Vector3 EdgeDirection;
        
        // 引用数据（非序列化）
        [NonSerialized] public Collider Collider;
        [NonSerialized] public Transform GroundTransform;
        
        public int GroundLayer;
        public GroundSurfaceType SurfaceType;
        
        // 移动平台
        public Vector3 LocalPositionOnGround;
        public Vector3 PrevGroundPosition;
        public Quaternion PrevGroundRotation;

        public void Reset()
        {
            HasGround = false;
            IsStable = false;
            Point = Vector3.zero;
            Normal = Vector3.up;
            Distance = float.MaxValue;
            SlopeAngle = 0;
            SupportRatio = 1f;
            IsOnEdge = false;
            EdgeDirection = Vector3.zero;
            Collider = null;
            GroundTransform = null;
            GroundLayer = 0;
            SurfaceType = GroundSurfaceType.Default;
        }
    }

    [Serializable]
    public class GroundDetectorConfig
    {
        [Header("=== 基础 ===")]
        public LayerMask GroundMask = ~0;
        public LayerMask PlatformMask;  // 可穿透平台
        
        [Range(20f, 60f)]
        public float MaxSlopeAngle = 50f;
        public float StepHeight = 0.3f;

        [Header("=== 检测距离 ===")]
        public float GroundCheckDistance = 0.15f;
        public float AirborneCheckDistance = 0.5f;
        public float GroundSnapDistance = 0.1f;

        [Header("=== 检测精度 ===")]
        [Range(0.8f, 0.99f)]
        public float RadiusScale = 0.9f;
        [Range(0.01f, 0.05f)]
        public float SkinWidth = 0.02f;

        [Header("=== 稳定性 ===")]
        [Range(1, 5)]
        public int StateChangeFrameThreshold = 2;
        public float CoyoteTime = 0.1f;
        public float LandingBufferTime = 0.1f;

        [Header("=== 边缘检测 ===")]
        public bool EnableEdgeDetection = true;
        [Range(4, 12)]
        public int EdgeRayCount = 8;
        [Range(0.3f, 0.7f)]
        public float EdgeSupportThreshold = 0.5f;
        [Range(1, 5)]
        public int EdgeCheckInterval = 3;

        [Header("=== 性能 ===")]
        public bool ReduceAirborneCheckFrequency = true;
        [Range(2, 4)]
        public int AirborneCheckInterval = 2;

        [Header("=== 2.5D 模式 ===")]
        public bool Enable2_5DMode = false;
        
        // 预计算值
        [NonSerialized] public float CosMaxSlopeAngle;
        
        public void Initialize()
        {
            CosMaxSlopeAngle = Mathf.Cos(MaxSlopeAngle * Mathf.Deg2Rad);
        }

        public GroundDetectorConfig Clone()
        {
            var clone = (GroundDetectorConfig)MemberwiseClone();
            clone.Initialize();
            return clone;
        }
    }
    
    #endregion

    #region 组件
    
    public class CheckGroundedComponent : Entity, IAwake<GameObject>, IDestroy
    {
        // 引用
        public Transform Transform;
        public CapsuleCollider Capsule;
        public Rigidbody Rigidbody;
        
        // 配置
        public GroundDetectorConfig Config;
        
        // 状态
        public GroundState State;
        public GroundState PrevState;
        public AirborneReason AirborneReason;
        public GroundHitInfo GroundHit;
        
        // 时间
        public float TimeLeftGround;
        public float TimeLanded;
        public float GroundedDuration;
        public float AirborneDuration;
        public bool InCoyoteTime;
        
        // 下跳
        public Collider IgnoredPlatform;
        public float IgnorePlatformUntil;
        
        // 内部状态
        public float CapsuleRadius;
        public float CapsuleHeight;
        public Vector3 LastGroundedPosition;
        public int ConsecutiveAirborneFrames;
        public int ConsecutiveGroundedFrames;
        public int EdgeCheckCounter;
        public int FrameCounter;
        
        // 预分配缓存
        public readonly Collider[] OverlapBuffer = new Collider[8];
        public readonly RaycastHit[] SphereCastBuffer = new RaycastHit[8];
        public readonly RaycastHit[] RaycastBuffer = new RaycastHit[8];
        public readonly Vector3[] RayOriginBuffer = new Vector3[5];
        
        // 事件
        public event Action OnLanded;
        public event Action OnLeftGround;
        public event Action<float> OnFallDamage;
        
        public void InvokeLanded() => OnLanded?.Invoke();
        public void InvokeLeftGround() => OnLeftGround?.Invoke();
        public void InvokeFallDamage(float h) => OnFallDamage?.Invoke(h);
        
        public void ClearEvents()
        {
            OnLanded = null;
            OnLeftGround = null;
            OnFallDamage = null;
        }
    }
    
    #endregion
}