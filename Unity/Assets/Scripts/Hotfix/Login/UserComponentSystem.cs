using System.Collections.Generic;

namespace ET
{
    [EntitySystemOf(typeof(UserComponent))]
    public static partial class UserComponentSystem
    {
        [EntitySystem]
        private static void Awake(this UserComponent self)
        {
            self.RoleList = new List<RoleData>();
        }
        
        /// <summary>
        /// 从Proto的UserInfo设置用户信息
        /// </summary>
        public static void SetUserInfo(this UserComponent self, UserInfo userInfo)
        {
            self.Account = userInfo.Account;
            self.UserId = userInfo.UserId;
            self.Username = userInfo.Username;
            self.VipLevel = userInfo.VipLevel;
            self.TotalRecharge = userInfo.TotalRecharge;
            
            // 安全地复制角色ID列表
            self.RoleList.Clear();
            if (userInfo.RoleInfoList != null)
            {
                foreach (var roleInfo in userInfo.RoleInfoList)
                {
                    RoleData roleData = new RoleData();
                    roleData.RoleId = roleInfo.RoleId;
                    roleData.RoleConfigId = roleInfo.RoleConfigId;
                    self.RoleList.Add(roleData);
                }
            }
            if(userInfo.LastRoleInfo != null)
            {
                RoleData roleData = new RoleData();
                roleData.RoleId = userInfo.LastRoleInfo.RoleId;
                roleData.RoleConfigId = userInfo.LastRoleInfo.RoleConfigId;
                self.CurrentRole = roleData;
            }
        }
        
        /// <summary>
        /// 检查是否有角色
        /// </summary>
        public static bool HasRole(this UserComponent self)
        {
            return self.RoleList != null && self.RoleList.Count > 0;
        }
        
        /// <summary>
        /// 获取显示信息（用于UI和日志）
        /// </summary>
        public static string GetDisplayInfo(this UserComponent self)
        {
            return $"[{self.Username}] UserId={self.UserId}, VIP={self.VipLevel}, 角色数={self.RoleList?.Count ?? 0}";
        }
        
        /// <summary>
        /// 是否已进入游戏（CurrentRoleId被设置）
        /// </summary>
        public static bool IsInGame(this UserComponent self)
        {
            return self.CurrentRole != null;
        }
    }
}