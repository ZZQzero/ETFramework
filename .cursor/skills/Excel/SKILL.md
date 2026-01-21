---
name: Excel-Config
description: 基于Luban工具的Excel配置表系统，支持将Excel表格转换为C#代码和二进制数据，通过TableConfigManager统一加载，以xxxConfig.Instance.Get()方式读取运行时配置数据。
---
# 配置表（Luban）用法与数据读取（以 MonsterConfig 为例）

## 目标

本项目使用 **Luban + Excel** 生成“配置表代码（C#）+ 二进制数据（.bytes）”，运行时通过 `TableConfigManager` 统一加载后，以 `xxxConfig.Instance.Get(...)` 的方式读取数据。

本文档整理：
- **表怎么加**
- **怎么生成**
- **运行时怎么加载**
- **代码里怎么读**
- **常见坑（Unit/Monster 的关联）**

---

## 1. 表放在哪里？（源表）

- **表数据目录**：`Config/Excel/Tables/`
- **表注册（Schema）**：`Config/Excel/__tables__.xlsx`
- （可选）bean/enum 定义：`Config/Excel/__beans__.xlsx`、`Config/Excel/__enums__.xlsx`

### 表头规范（必须遵守）

每个 `Tables/*.xlsx` 的第一个 Sheet 通常包含 3 行表头：
- `##var`：字段名（决定生成的 C# 字段名）
- `##type`：字段类型（决定二进制序列化/反序列化）
- `##`：注释/说明（生成到 C# 的注释里）

示例可参考：`Config/Excel/Tables/Unit表.xlsx`、`Config/Excel/Tables/AI机器人表.xlsx`

---

## 2. 怎么注册一张新表？（__tables__.xlsx）

在 `Config/Excel/__tables__.xlsx` 追加一行（可参考 `TbUnit` 这一行）：
- **full_name**：表容器名（例如：`TbMonster`）
- **value_type**：行结构类型（例如：`MonsterTable`）
- **read_schema_from_file**：`True`（让 Luban 从 Excel 表头读取 schema）
- **input**：表路径（例如：`Tables/怪物表.xlsx`）

注册完成后，Luban 会生成：
- `Unity/Assets/Scripts/Model/Core/Share/TableConfig/Excel/Generated/MonsterTable.cs`
- `Unity/Assets/Scripts/Model/Core/Share/TableConfig/Excel/Generated/TbMonster.cs`
- `Unity/Assets/Scripts/Model/Core/Share/TableConfig/Excel/Generated/Tables.cs`（新增 `TbMonster` 属性）
- 二进制数据：`Unity/Assets/Config/Excel/Gen/Bytes/tbmonster.bytes`

---

## 3. 怎么生成代码与 bytes？

在仓库根目录执行：

```bat
cd Config
gen.bat
```

生成脚本：`Config/gen.bat`  
关键输出路径：
- **输出代码**：`Unity/Assets/Scripts/Model/Core/Share/TableConfig/Excel/Generated`
- **输出数据**：`Unity/Assets/Config/Excel/Gen/Bytes`

Luban 配置：`Config/luban.conf`

---

## 4. 运行时是怎么加载配置的？

### 4.1 初始化入口

游戏启动注册单例时会创建 `TableConfigManager`：

```8:20:Unity/Assets/Scripts/GameEntry/GameRegister/Share/GameRegister.cs
public static void RegisterSingleton()
{
    World.Instance.AddSingleton<TableConfigManager>();
    // ...
}
```

### 4.2 加载实现（DOTNET/UNITY 分支）

`TableConfigManager` 会在 `Awake()` 中构造 `Tables` 并通过 loader 加载每个 `tb*.bytes`：

```6:23:Unity/Assets/Scripts/Model/Core/Share/TableConfig/TableConfigManager.cs
public void Awake()
{
    ConfigTables = new ET.Tables(LoadByteBuf);
}

private ByteBuf LoadByteBuf(string file)
{
#if DOTNET
    string configFilePath = $"Unity/Assets/Config/Excel/Gen/Bytes/{file}.bytes";
    return new ByteBuf(File.ReadAllBytes(configFilePath));
#endif
}
```

因此：
- **服务端（DOTNET）**直接从仓库路径读取 `Unity/Assets/Config/Excel/Gen/Bytes/*.bytes`
- **客户端（UNITY）**走 `ResourcesLoadManager`（细节在 UNITY 分支里）

---

## 5. 在代码里怎么读表？（MonsterConfig / UnitConfig）

### 5.1 读取一条

`MonsterConfig` 是生成的单例封装（同理还有 `UnitConfig` 等）：

```12:28:Unity/Assets/Scripts/Model/Core/Share/TableConfig/Excel/Category/MonsterConfig.cs
public override MonsterTable Get(int id)
{
    return TableConfigManager.Instance.ConfigTables.TbMonster.Get(id);
}
```

用法：
- `MonsterConfig.Instance.Get(id)`：必须存在，否则抛异常
- `MonsterConfig.Instance.GetOrDefault(id)`：不存在返回 `default`

### 5.2 遍历全表

- `MonsterConfig.Instance.GetDataList()`：返回 `List<MonsterTable>`

---

## 6. Unit / Monster 的关键关联（常见坑）

### 6.1 `Unit.Type()` 的来源

`Unit.Type()` 并不是 `MonsterConfig` 里的字段，而是由 **Unit 的 ConfigId** 反查 `UnitConfig` 决定：

```12:20:Unity/Assets/Scripts/Hotfix/Unit/Share/UnitSystem.cs
public static int Type(this Unit self)
{
    return self.Config().Type; // UnitConfig.Instance.Get(self.ConfigId).Type
}
```

### 6.2 MonsterConfig 里 “UnitId/UnitConfigId” 必须指向 Unit表

当前刷怪实现是：
- `MonsterConfig`（表）提供怪物数值/AI 等
- `MonsterTable.UnitId` 用来当作 `Unit.ConfigId`（也就是指向 `Unit表.Id`）

因此务必保证：
- `MonsterTable.UnitId` 指向 `Unit表` 的某一行 `Id`
- 且该 `Unit表` 行的 `Type == UnitType.Monster (5002)`

否则会出现：
- 怪物被当成 Player/NPC
- AOI/客户端显示/逻辑分支异常

---

## 7. 示例：用 MonsterConfig 生成怪物 Unit（当前实现）

当前 `UnitFactory.CreateMonster` 会：
- `MonsterConfig.Instance.Get(monsterConfigId)` 拿到 `MonsterTable`
- `GenerateIdManager.Instance.GenerateId()` 生成怪物 UnitId
- 用 `monster.UnitId` 创建 `Unit`（即指向 Unit表的那行）
- 给怪物挂 `NumericComponent + AOIEntity`（确保能同步到客户端）

代码见：`DotNet/ET.Hotfix/Map/Unit/UnitFactory.cs`

---

## 8. 约定建议（后续扩展）

- **表字段命名**：统一 PascalCase（与现有 `UnitTable/MonsterTable` 一致）
- **跨表关联**：尽量用 `xxxId`（int）关联，并在注释里写清楚“关联哪张表的 Id”
- **新增表流程**：
  - 在 `Config/Excel/Tables` 新建 xlsx
  - 在 `__tables__.xlsx` 注册
  - 运行 `Config/gen.bat`
  - 代码里通过 `XxxConfig.Instance` 读取

