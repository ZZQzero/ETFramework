using UnityEngine;

namespace ET
{
    
    public enum HitApplyMode : byte
    {
        Reject = 0,
        Replace = 1, // 直接覆盖当前受击（切状态/播新动画）
        Refresh = 2, // 不切状态，只刷新计时/力度（例如硬直延长）
        FeedbackOnly = 3, // 仅触发反馈（HitStop/震屏/慢动作等），不进入受击状态机
    }

    public readonly struct HitRulesResult
    {
        public readonly HitApplyMode Mode;
        public readonly HitReactionRequest Request; // 归一化后的 request（方向/数值 clamp）

        public bool Accepted => this.Mode != HitApplyMode.Reject;

        public HitRulesResult(HitApplyMode mode, HitReactionRequest request)
        {
            this.Mode = mode;
            this.Request = request;
        }

        public override string ToString()
        {
            return $"受击判定结果(接受={this.Accepted}, 模式={this.Mode}, {this.Request})";
        }
    }
}

