# UXTools — AI 开发指南

> 供 AI 工具读取的功能拆解、架构规范和开发约束文档

---

## 包概览

- **包名**：`com.ys4fun.uxtools`
- **命名空间**：`UITool`
- **Unity 版本**：6000.0+
- **程序集**：
  - `UXTools.Runtime` — 运行时组件和数据模型
  - `UXTools.Editor` — 编辑器工具（依赖 Runtime + Editor.Common）
  - `UXTools.Editor.Common` — 编辑器公共工具（Config、Utils、Localization）
- **条件编译**：`TMP_PRESENT`（TextMeshPro 可用时）、`UXTOOLS_DEV`（内部开发）

---

## 目录结构

```
Assets/UXTools/
├── Runtime/
│   ├── Components/          UXText.cs, UXImage.cs
│   ├── ColorPreset/         ColorPresetAsset, ColorPresetEntry, IColorPresetTarget, IUXColorKey, UXColorKey, UXColorBinding
│   └── TextPreset/          TextPresetAsset, TextPresetEntry, IUXTextStyleKey, UXTextStyleKey
├── Editor/
│   ├── Common/
│   │   ├── Config/          UIToolConfig.cs (partial), Config.cs (partial), UXToolsProjectSettings.cs
│   │   ├── Utils/           JsonAssetManager, Utils, UnityExtensions, PrefabStageUtils, JsonUtilityEx
│   │   └── EditorLocalization/
│   └── Tools/UXTools/
│       ├── CodeGen/         PresetCodeGenerator.cs
│       ├── Logic/           EditorLogic, FindContainerLogic, AlignLogic, CombineWidgetLogic, LocationLineLogic, SnapLogic, PrefabActiveLogic
│       ├── Settings/        SwitchSetting, WidgetLabelsSettings, WidgetListSetting, PrefabOpenedSetting, RecentFilesSetting, PrefabTabsData
│       ├── WidgetGenerator/ WidgetGenerator.cs
│       ├── Window_Custom/
│       │   ├── Configuration/   ConfigurationWindow.cs
│       │   ├── DesignLibrary/   ColorPresetWindow, TextPresetWindow, DesignLibraryWindowBase, ColorPresetDragHandler, TextPresetDragHandler
│       │   └── PrefabRepository/ WidgetRepositoryWindow, PrefabCreateWindow, PrefabRecentWindow 等
│       └── Window_Editor/
│           ├── Inspector/   UXTextEditor, UXImageEditor, RectTransformCustomInspector
│           ├── SceneView/   SceneViewToolBar, PrefabTabs, locationLine/LocationLine
│           └── Hierarchy/   HierarchyContextMenuEx
├── Res/                     UXML, USS, 图标等包内资源
└── package.json
```

---

## 核心架构模式

### 预设系统（颜色 + 文字）

```
ScriptableObject (ColorPresetAsset / TextPresetAsset)
  └── List<Entry> (ColorPresetEntry / TextPresetEntry)
        ├── IUXColorKey / IUXTextStyleKey     ← 类型安全的预设键接口
        ├── UXColorKey / UXTextStyleKey       ← 轻量 struct（代码生成用）
        └── 扩展方法 SetColor / SetColorByName / SetTextStyle / SetTextStyleByName

组件层：
  UXText : TextMeshProUGUI, IColorPresetTarget
  UXImage : Image, IColorPresetTarget
  UXColorBinding : MonoBehaviour, IColorPresetTarget

编辑器层：
  PresetWindow → 三栏布局（分类/列表/详情）继承 DesignLibraryWindowBase
  DragHandler → Hierarchy 拖放 + 自动组件转换
  Inspector → Popup 选择 + 拖拽接收
  CodeGenerator → 生成 C# 常量到 UXToolsCustomRuntime/Generated/
```

### 路径配置系统

```
UXToolsProjectSettings (ScriptableObject, 单例)
  ├── configParentPath   → {parent}/UXToolsCustomConfig/   (始终)
  ├── editorParentPath   → {parent}/UXToolsCustomEditor/   (可选，含 asmdef)
  └── runtimeParentPath  → {parent}/UXToolsCustomRuntime/  (可选，含 asmdef)

UIToolConfig (partial static class)
  ├── ProjectDataPath         固定 "Assets/UXToolsData/"
  ├── CustomConfigPath        → UXToolsProjectSettings.GetCustomConfigPath()
  ├── TextPresetPath          → CustomConfigPath + "TextPreset/"
  ├── ColorPresetPath         → CustomConfigPath + "ColorPreset/"
  ├── CustomEditorPath        → 可选
  └── CustomRuntimePath       → 可选
```

### 编辑器窗口层级

```
DesignLibraryWindowBase (EditorWindow)
  ├── ColorPresetWindow
  └── TextPresetWindow
  共享：ThreeColumnLayout(), AddCategoryButton(), ListHeaderRow(), DetailLabel(), ShowPresetContextMenu()

ConfigurationWindow (EditorWindow)
  ├── 通用选项
  ├── 功能开关
  └── 路径设置
```

---

## 编码规范

### 命名

| 类别 | 格式 | 示例 |
|------|------|------|
| 私有字段 | `_camelCase` | `_colorPresetAsset` |
| 常量 | `UPPER_CASE` 或 `PascalCase` | `SETTINGS_ASSET_PATH`、`CONFIG_DIR_NAME` |
| 公有属性 | `PascalCase` | `ColorPresetAsset` |
| 方法 | `PascalCase` | `ApplyColorPreset()` |
| 接口 | `I` 前缀 | `IColorPresetTarget`、`IUXColorKey` |

### 注释语言

- 所有注释使用**中文**
- XML 文档注释 `<summary>` 描述功能和适用场景
- 不添加显而易见的叙述性注释和 `<code>` 使用示例
- 方法内部根据复杂度决定是否加注释

### 文件组织

- 使用 `#region` 组织代码段
- Editor-only 代码用 `#if UNITY_EDITOR` 包裹
- TMP 依赖代码用 `#if TMP_PRESENT` 包裹
- `[SerializeField]` 暴露私有字段到 Inspector
- `[NonSerialized]` 标记 EditorWindow 中不应跨域重载保留的字段

### 通用逻辑放置

- 跨组件的通用方法写为**接口扩展方法**（如 `ColorPresetTargetExtensions.SetColor`）
- 组件专用方法写为该组件类型的扩展方法（如 `UXTextStyleExtensions.SetTextStyle`）
- 不在组件类内部重复实现通用逻辑

---

## 新增功能的开发模式

### 新增预设类型（参考颜色预设的完整模式）

1. **Runtime 数据模型**：
   - `XxxPresetEntry`（实现 `IUXXxxKey`）
   - `XxxPresetAsset`（ScriptableObject，含缓存、分类、增删、查找）
   - `IUXXxxKey` 接口 + `UXXxxKey` struct

2. **Runtime 组件绑定**：
   - 在目标组件中添加 `_xxxPresetAsset` + `_xxxPresetId` 字段
   - 实现对应接口
   - 添加 `ApplyXxxPreset()` 方法

3. **Editor 窗口**：
   - 继承 `DesignLibraryWindowBase`
   - 三栏布局：分类 / 列表 / 详情
   - 工具栏：生成代码、重建索引、强制同步

4. **Editor Inspector**：
   - `CustomEditor` 增加预设选择 Popup + 拖拽接收

5. **Editor 拖拽**：
   - `[InitializeOnLoad]` 的 DragHandler，监听 Hierarchy 拖放

6. **代码生成**：
   - 在 `PresetCodeGenerator` 中添加生成方法
   - 输出到 `UXToolsCustomRuntime/Generated/`

### 新增编辑器窗口

- 继承 `EditorWindow`，在 `OnEnable` 中初始化 UI
- EditorWindow 中所有运行时数据字段标记 `[NonSerialized]` 防止域重载残留
- 使用 UIElements 构建 UI（非 IMGUI）
- 菜单注册在 `UIToolConfig` 中定义常量

### 修改路径体系

- 所有路径通过 `UIToolConfig` 的静态属性访问
- 路径计算依赖 `UXToolsProjectSettings.Instance`
- 新增路径需同步更新 `UXToolsAssetsCreator.ValidateDirectories()`

---

## 关键接口说明

### IColorPresetTarget

```csharp
public interface IColorPresetTarget
{
    ColorPresetAsset ColorPresetAsset { get; set; }
    string ColorPresetId { get; set; }
    void ApplyColorPreset();          // 各组件自行实现（访问不同的 color 属性）
    string GetColorPresetName();
}

// 扩展方法（所有实现者自动获得）
public static void SetColor(this IColorPresetTarget target, IUXColorKey key);
public static void SetColorByName(this IColorPresetTarget target, string presetName);
```

### IUXColorKey / IUXTextStyleKey

```csharp
public interface IUXColorKey { string PresetId { get; } }
public interface IUXTextStyleKey { string PresetId { get; } }

// 实现者：
// - ColorPresetEntry（显式接口实现）
// - UXColorKey struct（代码生成用）
// - 外部枚举扩展（用户自定义）
```

### 代码生成器

- `PresetCodeGenerator.GenerateColorDef(ColorPresetAsset)` — 从窗口传入资产实例
- `PresetCodeGenerator.GenerateTextStyleDef(TextPresetAsset)` — 同上
- 只生成 `entry.generateCode == true` 的条目
- 生成前自动启用 `enableCustomRuntime` 并创建 asmdef
- 生成的文件需要 `using UITool;`

---

## 已知约束

1. **TextPreset 依赖 TMP**：所有文字预设相关代码在 `#if TMP_PRESENT` 下
2. **颜色预设存储迁移**：旧版 JSON 路径为 `ProjectDataPath + "DesignLibrary/ColorPresetLibrary.json"`，新版为 ScriptableObject
3. **序列化字段兼容**：`UXToolsProjectSettings` 使用多个 `[FormerlySerializedAs]` 确保旧版数据迁移
4. **EditorWindow 域重载**：所有 `[Serializable]` 类型的窗口字段必须标记 `[NonSerialized]`
5. **辅助线坐标系**：横线使用 `style.top` 定位 + `m_DragOffsetY` 补偿 `evt.position` 与 `style.top` 的偏移
6. **快捷创建尺寸**：使用 `GUIPointToCanvasPlane` 投射到 Canvas 平面，尺寸限制在 Canvas 范围内
7. **废弃 API**：`TryGetGUIDAndLocalFileIdentifier(int)`、`AddDropHandler`、`InstanceIDToObject` 使用 `#pragma warning disable CS0618` 抑制
