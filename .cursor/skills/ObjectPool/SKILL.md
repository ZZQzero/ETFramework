---
name: object-pool
description: ET框架对象池系统，支持Entity、通用对象和Unity GameObject的池化管理，提供高性能的对象复用机制。
---

# ET Framework Object Pool System

ET框架提供三套对象池系统：通用对象池(ObjectPool)、Entity专用对象池(EntityObjectPool)和GameObject专用对象池(GameObjectPool)。

## 1. 通用对象池 (ObjectPool)

### 核心方法

#### Fetch<T>(bool isFromPool = true)
获取对象，支持选择是否使用池化。
```csharp
// 使用池化获取对象
var message = ObjectPool.Fetch<StateSyncOuter_C_10001>();

// 不使用池化，直接创建新对象
var freshObj = ObjectPool.Fetch<StateSyncOuter_C_10001>(isFromPool: false);
```

#### Recycle<T>(T obj)
回收对象到池中。
```csharp
ObjectPool.Recycle(message);
```

### 内部实现
- **两级缓存**：FastSlots（16个槽位，无锁）+ 环形缓冲区（256个容量，有锁）
- **接口要求**：对象必须实现IPool接口

## 2. Entity专用对象池 (EntityObjectPool)

### 核心方法

#### GetEntity<T>(long typeId, bool isFromPool, int maxPerType = 256)
获取Entity对象。
```csharp
// 获取Unit类型的Entity
var unit = EntityObjectPool.Instance.GetEntity<Unit>(TypeId<Unit>.Id, isFromPool: true);
```

#### RecycleEntity(Entity entity, int maxPerType = 256)
回收Entity对象。
```csharp
EntityObjectPool.Instance.RecycleEntity(unit);
```

#### ClearPool(long typeId)
清空指定类型的所有池化对象。
```csharp
EntityObjectPool.Instance.ClearPool(TypeId<Unit>.Id);
```

#### GetPoolCount(long typeId)
获取指定类型池中当前对象数量。
```csharp
int count = EntityObjectPool.Instance.GetPoolCount(TypeId<Unit>.Id);
```

### 内部实现
- **类型分组**：按TypeId分组管理
- **并发安全**：支持多线程环境
- **容量限制**：每个类型默认最多256个对象

## 3. GameObject专用对象池 (GameObjectPool)

### 池类型分类
```csharp
public enum PoolType
{
    Normal = 0,  // 普通对象
    Role,        // 角色对象
    UI,          // UI对象
    Effect,      // 特效对象
    Max          // 类型上限
}
```

### 初始化和配置

#### Init()
初始化对象池，创建类型分组的根节点。
```csharp
GameObjectPool.Instance.Init();
```

#### SetPackage(ResourcePackage package)
设置YooAsset资源包。
```csharp
var package = YooAssets.GetPackage("DefaultPackage");
GameObjectPool.Instance.SetPackage(package);
```

#### SetMaxPoolSize(string assetName, PoolType type, int maxSize)
配置特定资源的池容量限制。
```csharp
GameObjectPool.Instance.SetMaxPoolSize("EnemyPrefab", PoolType.Role, 20);
```

### 核心方法

#### GetObjectAsync(string assetName, PoolType poolType)
异步获取GameObject对象。
```csharp
var enemyObj = await GameObjectPool.Instance.GetObjectAsync("EnemyPrefab", PoolType.Role);
```

#### GetObjectSync(string assetName, PoolType poolType)
同步获取GameObject对象。
```csharp
var uiObj = GameObjectPool.Instance.GetObjectSync("UIPanel", PoolType.UI);
```

#### ReleaseObject(GameObject obj, PoolType type)
回收GameObject对象。
```csharp
GameObjectPool.Instance.ReleaseObject(enemyObj, PoolType.Role);
```

### 高级功能

#### ReleaseObjectByPoolType(PoolType type)
回收指定类型的所有活跃对象。
```csharp
GameObjectPool.Instance.ReleaseObjectByPoolType(PoolType.UI);
```

#### DestroyObjectPoolByType(PoolType type)
销毁指定类型的所有对象（包括池中和活跃的）。
```csharp
GameObjectPool.Instance.DestroyObjectPoolByType(PoolType.Effect);
```

#### DestroyAllObjectPool()
销毁所有对象池中的对象。
```csharp
GameObjectPool.Instance.DestroyAllObjectPool();
```

### 调试和查询接口

#### GetPoolDic()
获取对象池字典。
```csharp
var pools = GameObjectPool.Instance.GetPoolDic();
```

#### GetStats(string assetName, PoolType type)
获取指定资源的统计信息。
```csharp
var (pooled, active, total) = GameObjectPool.Instance.GetStats("EnemyPrefab", PoolType.Role);
```

#### GetPoolTypeNameDic()
获取按类型分组的资源名称字典。
```csharp
var typeNames = GameObjectPool.Instance.GetPoolTypeNameDic();
```

### 内部实现特性
- **延迟回收机制**：UI对象在布局重建时延迟回收，避免性能问题
- **三层复用策略**：延迟回收队列 → 常驻池 → 重新实例化
- **自动容量控制**：超过限制时自动销毁多余对象
- **资源引用计数**：自动管理YooAsset资源句柄的加载和释放

## 项目中的实际用法

### 消息协议对象池化
```csharp
// StateSyncOuter_C_10001.cs 中的用法
public static StateSyncOuter_C_10001 Create(bool isFromPool = true)
{
    return ObjectPool.Fetch<StateSyncOuter_C_10001>(isFromPool);
}

public void Recycle()
{
    ObjectPool.Recycle(this);
}
```

### Entity组件池化
```csharp
// Entity.cs 中的用法
public K AddComponent<K>(bool isFromPool = true) where K : Entity, IAwake
{
    K component = EntityObjectPool.Instance.GetEntity<K>(TypeId<K>.Id, isFromPool);
    // ... 初始化逻辑
    return component;
}

private void RemoveComponent(K component)
{
    EntityObjectPool.Instance.RecycleEntity(component);
}
```

### GameObject对象池化
```csharp
// UI管理器中的用法
public async UniTask<UIPanel> LoadUIPanel(string panelName)
{
    var uiObj = await GameObjectPool.Instance.GetObjectAsync(panelName, PoolType.UI);
    var panel = uiObj.GetComponent<UIPanel>();
    return panel;
}

public void UnloadUIPanel(GameObject uiObj)
{
    GameObjectPool.Instance.ReleaseObject(uiObj, PoolType.UI);
}
```

### 命中目标列表池化
```csharp
// AttackComponentSystem.cs 中的用法
ObjectPool.Recycle(hitTargets);
```