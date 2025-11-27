using System;
using System.Collections.Generic;
using System.Text;

namespace ET
{
    /// <summary>
    /// Entity调试工具类
    /// 用于查看Entity的组件信息、子Entity信息等，方便开发和调试
    /// </summary>
    public static class EntityDebugHelper
    {
        /// <summary>
        /// 打印Entity的所有组件信息到日志
        /// </summary>
        /// <param name="entity">要查看的Entity</param>
        /// <param name="includeChildren">是否包含子Entity</param>
        /// <param name="includeComponentChildren">是否包含组件的子Entity（例如：PlayerComponent下的Player）</param>
        /// <param name="maxDepth">最大递归深度（-1表示无限制）</param>
        public static void LogComponents(this Entity entity, bool includeChildren = false, bool includeComponentChildren = false, int maxDepth = -1)
        {
            if (entity == null || entity.IsDisposed)
            {
                Log.Warning($"Entity is null or disposed");
                return;
            }

            string info = GetEntityInfo(entity, includeChildren, includeComponentChildren, maxDepth, 0);
            Log.Info($"\n{info}");
        }

        /// <summary>
        /// 获取Entity的详细信息字符串
        /// </summary>
        /// <param name="entity">要查看的Entity</param>
        /// <param name="includeChildren">是否包含子Entity</param>
        /// <param name="includeComponentChildren">是否包含组件的子Entity（例如：PlayerComponent下的Player）</param>
        /// <param name="maxDepth">最大递归深度（-1表示无限制）</param>
        /// <param name="currentDepth">当前递归深度（内部使用）</param>
        /// <returns>格式化的字符串</returns>
        public static string GetEntityInfo(this Entity entity, bool includeChildren = false, bool includeComponentChildren = false, int maxDepth = -1, int currentDepth = 0)
        {
            if (entity == null || entity.IsDisposed)
            {
                return "Entity is null or disposed";
            }

            // maxDepth为-1表示无限制，否则检查深度
            if (maxDepth >= 0 && currentDepth > maxDepth)
            {
                return "... (max depth reached)";
            }

            StringBuilder sb = new StringBuilder();
            string indent = new string(' ', currentDepth * 2);

            // Entity基本信息
            sb.AppendLine($"{indent}┌─ Entity: {entity.GetType().Name}");
            sb.AppendLine($"{indent}│  InstanceId: {entity.InstanceId}");
            sb.AppendLine($"{indent}│  Id: {entity.Id}");
            sb.AppendLine($"{indent}│  TypeId: {entity.TypeId}");
            sb.AppendLine($"{indent}│  IsDisposed: {entity.IsDisposed}");
            sb.AppendLine($"{indent}│  IsFromPool: {entity.IsFromPool}");
            sb.AppendLine($"{indent}│  IsNew: {entity.IsNew}");

            // 父Entity信息
            if (entity.Parent != null)
            {
                sb.AppendLine($"{indent}│  Parent: {entity.Parent.GetType().Name} (Id: {entity.Parent.Id}, InstanceId: {entity.Parent.InstanceId})");
            }

            // Scene信息
            if (entity.IScene != null)
            {
                sb.AppendLine($"{indent}│  Scene: {entity.IScene.GetType().Name}|{SceneTypeSingleton.Instance.GetSceneName(entity.IScene.SceneType)}");
            }

            // 组件信息
            if (entity.Components != null && entity.Components.Count > 0)
            {
                sb.AppendLine($"{indent}│  Components ({entity.Components.Count}):");
                int componentIndex = 0;
                int componentCount = entity.Components.Count;
                foreach (var kv in entity.Components)
                {
                    Entity component = kv.Value;
                    bool isLastComponent = (++componentIndex == componentCount);
                    string componentPrefix = isLastComponent ? "└─" : "├─";
                    
                    if (component != null && !component.IsDisposed)
                    {
                        // 如果包含组件的子Entity，递归打印组件信息
                        if (includeComponentChildren && component.Children != null && component.Children.Count > 0)
                        {
                            // 先打印组件基本信息
                            sb.AppendLine($"{indent}│    {componentPrefix} {component.GetType().Name} (Id: {component.Id}, InstanceId: {component.InstanceId})");
                            sb.AppendLine($"{indent}│    {(isLastComponent ? " " : "│")}    Children ({component.Children.Count}):");
                            
                            // 打印组件的子Entity
                            int childIndex = 0;
                            int childCount = component.Children.Count;
                            foreach (var childKv in component.Children)
                            {
                                Entity child = childKv.Value;
                                if (child != null && !child.IsDisposed)
                                {
                                    bool isLastChild = (++childIndex == childCount);
                                    string childConnector = isLastComponent ? " " : "│";
                                    string childPrefix = isLastChild ? "└─" : "├─";
                                    
                                    // 递归获取子Entity信息
                                    string childInfo = GetEntityInfo(child, true, includeComponentChildren, maxDepth, currentDepth + 2);
                                    string[] childLines = childInfo.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                                    
                                    // 计算子Entity应该有的基础缩进（基于currentDepth+2，因为组件本身增加了一层）
                                    string childBaseIndent = new string(' ', (currentDepth + 2) * 2);
                                    
                                    // 处理每一行
                                    for (int i = 0; i < childLines.Length; i++)
                                    {
                                        string line = childLines[i].TrimEnd();
                                        if (string.IsNullOrWhiteSpace(line))
                                        {
                                            continue;
                                        }
                                        
                                        // 移除子Entity自己的缩进
                                        string trimmedLine = line;
                                        if (trimmedLine.StartsWith(childBaseIndent))
                                        {
                                            trimmedLine = trimmedLine.Substring(childBaseIndent.Length);
                                        }
                                        
                                        // 第一行：添加子Entity的前缀
                                        if (i == 0)
                                        {
                                            // 移除子Entity自己的前缀（如果有）
                                            if (trimmedLine.StartsWith("┌─"))
                                            {
                                                trimmedLine = trimmedLine.Substring(2).TrimStart();
                                            }
                                            sb.AppendLine($"{indent}│    {childConnector}    {childPrefix} {trimmedLine}");
                                        }
                                        else
                                        {
                                            // 后续行：添加父级的连接符
                                            sb.AppendLine($"{indent}│    {childConnector}    {(isLastChild ? " " : "│")}    {trimmedLine}");
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            // 不包含组件的子Entity，只打印组件基本信息
                            string componentInfo = $"{component.GetType().Name} (Id: {component.Id}, InstanceId: {component.InstanceId})";
                            sb.AppendLine($"{indent}│    {componentPrefix} {componentInfo}");
                        }
                    }
                    else
                    {
                        sb.AppendLine($"{indent}│    {componentPrefix} null or disposed");
                    }
                }
            }
            else
            {
                sb.AppendLine($"{indent}│  Components: None");
            }

            // 子Entity信息
            if (includeChildren && entity.Children != null && entity.Children.Count > 0)
            {
                sb.AppendLine($"{indent}│  Children ({entity.Children.Count}):");
                int childIndex = 0;
                int childCount = entity.Children.Count;
                foreach (var kv in entity.Children)
                {
                    Entity child = kv.Value;
                    if (child != null && !child.IsDisposed)
                    {
                        bool isLast = (++childIndex == childCount);
                        string connector = isLast ? " " : "│";
                        
                        // 递归获取子Entity信息（增加深度）
                        string childInfo = GetEntityInfo(child, true, includeComponentChildren, maxDepth, currentDepth + 1);
                        string[] childLines = childInfo.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                        
                        // 计算子Entity应该有的基础缩进（基于currentDepth+1）
                        string childBaseIndent = new string(' ', (currentDepth + 1) * 2);
                        
                        // 处理每一行
                        for (int i = 0; i < childLines.Length; i++)
                        {
                            string line = childLines[i].TrimEnd();
                            if (string.IsNullOrWhiteSpace(line))
                            {
                                continue;
                            }
                            
                            // 移除子Entity自己的缩进（基于currentDepth+1）
                            string trimmedLine = line;
                            if (trimmedLine.StartsWith(childBaseIndent))
                            {
                                trimmedLine = trimmedLine.Substring(childBaseIndent.Length);
                            }
                            
                            // 第一行：添加子Entity的前缀
                            if (i == 0)
                            {
                                string prefix = isLast ? "└─" : "├─";
                                // 移除子Entity自己的前缀（如果有）
                                if (trimmedLine.StartsWith("┌─"))
                                {
                                    trimmedLine = trimmedLine.Substring(2).TrimStart();
                                }
                                sb.AppendLine($"{indent}│    {prefix} {trimmedLine}");
                            }
                            else
                            {
                                // 后续行：添加父级的连接符
                                sb.AppendLine($"{indent}{connector}    {trimmedLine}");
                            }
                        }
                    }
                }
            }

            sb.Append($"{indent}└─");

            return sb.ToString();
        }

        /// <summary>
        /// 获取Entity的所有组件类型列表
        /// </summary>
        /// <param name="entity">要查看的Entity</param>
        /// <returns>组件类型名称列表</returns>
        public static List<string> GetComponentTypes(this Entity entity)
        {
            List<string> types = new List<string>();
            
            if (entity == null || entity.IsDisposed || entity.Components == null)
            {
                return types;
            }

            foreach (var kv in entity.Components)
            {
                Entity component = kv.Value;
                if (component != null && !component.IsDisposed)
                {
                    types.Add(component.GetType().Name);
                }
            }

            return types;
        }

        /// <summary>
        /// 检查Entity是否包含指定类型的组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="entity">要检查的Entity</param>
        /// <returns>是否包含该组件</returns>
        public static bool HasComponent<T>(this Entity entity) where T : Entity
        {
            if (entity == null || entity.IsDisposed)
            {
                return false;
            }

            return entity.GetComponent<T>() != null;
        }

        /// <summary>
        /// 打印Entity的组件统计信息
        /// </summary>
        /// <param name="entity">要查看的Entity</param>
        public static void LogComponentStats(this Entity entity)
        {
            if (entity == null || entity.IsDisposed)
            {
                Log.Warning($"Entity is null or disposed");
                return;
            }

            int componentCount = entity.Components?.Count ?? 0;
            int childrenCount = entity.Children?.Count ?? 0;
            int disposedComponentCount = 0;

            if (entity.Components != null)
            {
                foreach (var kv in entity.Components)
                {
                    if (kv.Value == null || kv.Value.IsDisposed)
                    {
                        disposedComponentCount++;
                    }
                }
            }

            Log.Info($"Entity: {entity.GetType().Name} (Id: {entity.Id}, InstanceId: {entity.InstanceId})");
            Log.Info($"  Components: {componentCount} (Disposed: {disposedComponentCount})");
            Log.Info($"  Children: {childrenCount}");
        }

        /// <summary>
        /// 查找Entity树中所有包含指定组件的Entity
        /// </summary>
        /// <typeparam name="T">要查找的组件类型</typeparam>
        /// <param name="entity">根Entity</param>
        /// <param name="includeSelf">是否包含自身</param>
        /// <param name="maxDepth">最大递归深度（-1表示无限制）</param>
        /// <returns>找到的Entity列表</returns>
        public static List<Entity> FindEntitiesWithComponent<T>(this Entity entity, bool includeSelf = true, int maxDepth = -1) 
            where T : Entity
        {
            List<Entity> results = new List<Entity>();
            
            if (entity == null || entity.IsDisposed)
            {
                return results;
            }

            FindEntitiesWithComponentRecursive<T>(entity, results, includeSelf, maxDepth, 0);
            return results;
        }

        private static void FindEntitiesWithComponentRecursive<T>(
            Entity entity, 
            List<Entity> results, 
            bool includeSelf, 
            int maxDepth, 
            int currentDepth) 
            where T : Entity
        {
            if (entity == null || entity.IsDisposed)
            {
                return;
            }
            
            // maxDepth为-1表示无限制，否则检查深度
            if (maxDepth >= 0 && currentDepth > maxDepth)
            {
                return;
            }

            if (includeSelf && entity.HasComponent<T>())
            {
                results.Add(entity);
            }

            if (entity.Children != null)
            {
                foreach (var kv in entity.Children)
                {
                    Entity child = kv.Value;
                    if (child != null && !child.IsDisposed)
                    {
                        FindEntitiesWithComponentRecursive<T>(child, results, true, maxDepth, currentDepth + 1);
                    }
                }
            }
        }

        /// <summary>
        /// 打印Entity的完整树形结构（包含所有子Entity和组件，无深度限制）
        /// </summary>
        /// <param name="entity">根Entity</param>
        public static void LogEntityTree(this Entity entity)
        {
            entity.LogComponents(includeChildren: true, includeComponentChildren: true, maxDepth: -1);
        }
    }
}