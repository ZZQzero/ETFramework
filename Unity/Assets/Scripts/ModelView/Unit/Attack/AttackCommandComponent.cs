using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 攻击命令队列（Command）
    /// - 上层（玩家输入/AI/回放/网络）写入命令
    /// - AttackComponentSystem 消费命令并执行（底层不关心来源）
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public sealed class AttackCommandComponent : Entity, IAwake
    {
        public struct AttackCommand
        {
            /// <summary>
            /// 技能ID：0 表示使用 AttackCatalog.BasicAttackSkillId
            /// </summary>
            public int SkillId;

            /// <summary>
            /// 输入类型：用于连段分支（轻/重/上挑/下砸...）
            /// </summary>
            public ComboInputType InputType;

            /// <summary>
            /// 可选：目标单位ID（未来用于锁定/指向性技能）。
            /// 当前近战判定仍以 Physics 扫描为准，可不填（0）。
            /// </summary>
            public long TargetUnitId;
        }

        private readonly Queue<AttackCommand> _queue = new Queue<AttackCommand>(8);

        public void Enqueue(in AttackCommand cmd)
        {
            _queue.Enqueue(cmd);
        }

        public bool TryDequeue(out AttackCommand cmd)
        {
            if (_queue.Count <= 0)
            {
                cmd = default;
                return false;
            }

            cmd = _queue.Dequeue();
            return true;
        }
    }
}