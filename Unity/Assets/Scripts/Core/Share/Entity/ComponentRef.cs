namespace ET
{
    /// <summary>
    /// 组件延迟解析引用结构。
    /// 用于避免 Awake 时序问题和减少重复 GetComponent 调用。
    /// </summary>
    /// <typeparam name="T">组件类型，必须继承自 Entity</typeparam>
    public struct ComponentRef<T> where T : Entity
    {
        private Entity _unit;
        private T _cached;
        private bool _resolved;

        public ComponentRef(Entity unit)
        {
            _unit = unit;
            _cached = null;
            _resolved = false;
        }

        /// <summary>
        /// 重设归属 Unit（会清空缓存）。
        /// </summary>
        public void SetOwner(Entity unit)
        {
            _unit = unit;
            _cached = null;
            _resolved = false;
        }

        /// <summary>
        /// 直接设置缓存值。
        /// </summary>
        public void SetCached(T component)
        {
            _cached = component;
            _resolved = true;
        }

        /// <summary>
        /// 获取组件（延迟解析并缓存）。
        /// </summary>
        public T Get()
        {
            // 已有有效缓存：直接返回
            if (_cached != null)
            {
                return _cached;
            }

            // 缓存为空时，允许重试解析：
            // - 解决 Awake/添加组件时序导致的“首次解析为 null 后永久卡死”问题
            // - 对于不存在的可选组件，Get() 可能会重复查询；调用方应避免在热路径频繁读取可选组件
            _cached = _unit?.GetComponent<T>();
            _resolved = true;
            return _cached;
        }

        /// <summary>
        /// 是否已解析过。
        /// </summary>
        public bool IsResolved => _resolved;

        /// <summary>
        /// 是否已有有效缓存（不触发解析）。
        /// </summary>
        public bool HasValue => _resolved && _cached != null;

        /// <summary>
        /// 获取组件并返回是否成功（会触发解析）。
        /// </summary>
        public bool TryGet(out T component)
        {
            component = Get();
            return component != null;
        }

        /// <summary>
        /// 重置缓存（用于组件可能被添加/移除的场景）。
        /// </summary>
        public void Invalidate()
        {
            _cached = null;
            _resolved = false;
        }

        public static implicit operator T(ComponentRef<T> r) => r.Get();
    }
}
