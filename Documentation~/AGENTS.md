# UXTools — AI 开发指南

> 供 AI 工具读取的功能拆解、架构规范和开发约束文档

---

## 包概览

- **包名**：`com.ys4fun.uxtools`
- **版本**：`1.4.0`
- **命名空间**：`UITool`、`UIAnimation`
- **Unity 版本**：6000.0+
- **程序集**：
  - `UXTools.Runtime` — 运行时组件和数据模型
  - `UXTools.Animation.Runtime` — UIAnimation 运行时触发系统
  - `UXTools.Editor` — 编辑器工具（依赖 Runtime + Editor.Common）
  - `UXTools.Editor.Common` — 编辑器公共工具（Config、Utils、Localization）
- **条件编译**：`TMP_PRESENT`（TextMeshPro 可用时）、`UXTOOLS_DEV`（内部开发）

---

## 目录结构

```
Assets/UXTools/
├── Runtime/
│   ├── UIAnimation/         AnimationTriggerAuthoring, AnimationTriggerSlot, AnimationTriggerBinding, TriggerPointAttribute
│   ├── Components/          UXText.cs, UXImage.cs
│   ├── ColorPreset/         ColorPresetAsset, ColorPresetEntry, IColorPresetTarget, IUXColorKey, UXColorKey, UXColorBinding
│   └── TextPreset/          TextPresetAsset, TextPresetEntry, IUXTextStyleKey, UXTextStyleKey
├── Editor/
│   ├── Common/
│   │   ├── Config/          UIToolConfig.cs (partial), Config.cs (partial), UXToolsProjectSettings.cs
│   │   ├── Utils/           JsonAssetManager, Utils, UnityExtensions, PrefabStageUtils, JsonUtilityEx
│   │   └── EditorLocalization/
│   └── Tools/UXTools/
│       ├── CodeGen/         PresetCodeGenerator.cs, AnimationTriggerCodeGenerator.cs, AnimationTriggerGeneratedScriptAutoAttacher.cs
│       ├── Logic/           EditorLogic, FindContainerLogic, AlignLogic, CombineWidgetLogic, LocationLineLogic, SnapLogic, PrefabActiveLogic
│       ├── Settings/        SwitchSetting, WidgetLabelsSettings, WidgetListSetting, PrefabOpenedSetting, RecentFilesSetting, PrefabTabsData
│       ├── WidgetGenerator/ WidgetGenerator.cs
│       ├── Window_Custom/
│       │   ├── Configuration/   ConfigurationWindow.cs
│       │   ├── DesignLibrary/   ColorPresetWindow, TextPresetWindow, DesignLibraryWindowBase, ColorPresetDragHandler, TextPresetDragHandler
│       │   └── PrefabRepository/ WidgetRepositoryWindow, PrefabCreateWindow, PrefabRecentWindow 等
│       └── Window_Editor/
│           ├── Inspector/   UXTextEditor, UXImageEditor, RectTransformCustomInspector, AnimationTriggerAuthoringEditor, AnimationTriggerBindingDrawer
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

### UIAnimation 触发系统（双模式）

```
Code-First：
IAnimationTriggerSource
  ├── TriggerBindings : List<AnimationTriggerBinding>
  ├── 默认 GetTriggerPointNames() → 扫描 [TriggerPoint("标签")] 方法
  └── FireTrigger(nameof(Method)) 直接按键值触发

延迟触发行为：
- 延迟绑定目前使用协程实现，不依赖 UniTask
- `WaitForSeconds` 按 delay 值缓存，减少重复分配
- 对同一触发源上的同一条延迟绑定，新的触发会取消旧的待执行协程，只保留最后一次，避免协程堆积

Authoring-First：
AnimationTriggerAuthoring : MonoBehaviour, IAnimationTriggerSource, ITriggerPointLabelProvider
  ├── _triggerSlots    : List<AnimationTriggerSlot>
  │     ├── Id              运行时稳定键
  │     ├── DisplayName     动画师可见名称
  │     └── CodeMethodName  程序员工具中填写的方法名
  ├── _triggerBindings : List<AnimationTriggerBinding>
  ├── FireById(id)     → 按稳定键触发
  └── Editor：
        ├── AnimationTriggerAuthoringEditor  动画师主视图（触发时机 + 绑定）
        └── AnimationTriggerCodeGenWindow    程序员工具（方法名 + 生成代码）

代码生成：
AnimationTriggerCodeGenerator
  └── 生成组合式桥接脚本：
        public class XxxTrigger : YourBaseType
        {
            [SerializeField] private AnimationTriggerAuthoring _animationTriggerAuthoring;
            public void OnClickTrigger() => _animationTriggerAuthoring.FireById("slot-id");
        }

实现取舍：
- 当前处于开发阶段，UIAnimation 不保留 `AnimationTriggerProxy` / `AnimationTriggerDefinition` 兼容类
- 仅保留迁移标记（如 `[FormerlySerializedAs]`、`[MovedFrom]`）用于已有场景/资源数据迁移
- 新功能、新文档、新代码一律只使用 `AnimationTriggerAuthoring` / `AnimationTriggerSlot`

程序员工具增强：
- AnimationTriggerCodeGenWindow 支持选择基类脚本、批量自动命名方法名
- 支持「生成并挂载」：生成脚本后，等待 Unity 编译完成后自动把桥接组件挂到当前对象，并自动回填 `_animationTriggerAuthoring`

作者视图增强：
- Binding 列表折叠态显示摘要：`触发时机 -> 目标 -> 动画名 (delay s)`
- AnimationTriggerValidationUtility 统一给出基础校验提示（空显示名、重复显示名、无效绑定等）
```

### 编辑器窗口层级

```
DesignLibraryWindowBase (EditorWindow)
  ├── ColorPresetWindow
  └── TextPresetWindow
  共享：ThreeColumnLayout(), AddCategoryButton(), ListHeaderRow(), DetailLabel(), ShowPresetContextMenu()

ConfigurationWindow (EditorWindow)
  ├── 通用选项（含 DocumentationUrl 字段）
  ├── 功能开关
  └── 路径设置
```

---

## 项目依赖

### Odin Inspector

项目已安装 Odin Inspector（`Assets/Plugins/Sirenix/Odin Inspector/`），**可辅助使用**以减少手写 IMGUI 代码量、提升 Inspector UX。

| 场景 | 推荐 Odin 方式 |
|------|----------------|
| 只读信息展示 | `[ShowInInspector, ReadOnly]` |
| 列表增强 | `[ListDrawerSettings(ShowFoldout = false, DraggableItems = true)]` |
| 字段分组 | `[BoxGroup]` / `[TitleGroup]` / `[FoldoutGroup]`（代替 `[Header]`） |
| 说明文字 | `[InfoBox("提示内容")]` |
| 字段重命名 | `[LabelText("中文标签")]` |
| 条件显示 | `[ShowIf]` / `[HideIf]` |
| Inspector 按钮 | `[Button("操作名称")]` |
| 字段校验 | `[Required]` / `[ValidateInput]` |

**注意事项：**
- 自定义 `.asmdef` 程序集中使用 Odin 特性，需在 `.asmdef` 的 `references` 中添加对 `Sirenix.OdinInspector.Attributes`（Runtime）或 `Sirenix.OdinInspector.Editor`（Editor）的 GUID 引用
- Runtime 代码只依赖 `Sirenix.OdinInspector.Attributes`，不依赖 Editor 程序集
- Odin 本质是 IMGUI 封装，与项目"禁止 UIToolkit"规范不冲突，可放心使用

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

- 继承 `EditorWindow`，在 `OnEnable` 中初始化 UI，`OnDisable` 中保存脏数据
- EditorWindow 中所有运行时数据字段标记 `[NonSerialized]` 防止域重载残留
- **禁止使用 UIToolkit/UIElements 构建新的编辑器界面**，统一使用 IMGUI（`OnGUI`、`EditorGUILayout`、`GUILayout`）。原因：UIToolkit 代码冗长、对 AI Agent 不友好、样式难以维护、视觉效果不理想。已有 UIToolkit 窗口保持现状不做迁移，但新增功能一律用 IMGUI
- 菜单注册在 `UIToolConfig` 中定义常量
- ColorField 等高频变更控件：ValueChanged 只更新内存和 UI 预览，FocusOut 时保存落盘 + RebuildContent

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
- 字段名优先使用 `entry.codeAlias`（代码别名），为空时回退到 `entry.presetName`
- 生成前自动启用 `enableCustomRuntime` 并创建 asmdef
- 生成的文件需要 `using UITool;`

---

## 已知约束

1. **TextPreset 依赖 TMP**：所有文字预设相关代码在 `#if TMP_PRESENT` 下
2. **颜色预设存储迁移**：旧版 JSON 路径为 `ProjectDataPath + "DesignLibrary/ColorPresetLibrary.json"`，新版为 ScriptableObject
3. **序列化字段兼容**：`UXToolsProjectSettings` 使用多个 `[FormerlySerializedAs]` 确保旧版数据迁移
4. **EditorWindow 域重载**：所有 `[Serializable]` 类型的窗口字段必须标记 `[NonSerialized]`
5. **辅助线坐标系**：横线用 `style.top` + `m_DragOffsetY`，竖线用 `style.left` + `m_DragOffsetX`，均在 `OnMouseDown` 时记录光标到元素中心的偏移量，拖拽中用 `correctedX/Y` 消除抓取偏差
6. **快捷创建尺寸**：使用 `GUIPointToCanvasPlane` 投射到 Canvas 平面，尺寸限制在 Canvas 范围内
7. **废弃 API**：`TryGetGUIDAndLocalFileIdentifier(int)`、`AddDropHandler`、`InstanceIDToObject` 使用 `#pragma warning disable CS0618` 抑制
8. **生成代码**必须包含 `using UITool;`，字段名优先使用 `codeAlias`（代码别名），为空时回退到 `presetName`
9. **UXColorBinding 冲突**：如果 Graphic 本身是 IColorPresetTarget（UXText/UXImage），UXColorBinding 的 ApplyColorPreset 会跳过，Inspector 显示警告
10. **工具栏域重载**：`SceneViewToolBar.InitFunction` 中调用 `TryOpenToolbar()` 确保重编译后工具栏恢复
11. **ColorField 性能**：ValueChanged 只更新内存/UI，FocusOut 时落盘 + 刷新列表，OnDisable 兜底保存
12. **AlignLogic Y 方向必须用 lossyScale**：`GetRealPostionY`、`GetTopWithoutScaleAndPivot`、`GetBottomWithoutScaleAndPivot` 及对齐 case 中高度计算均应使用 `lossyScale.y`，不能用 `localScale.y`。ScreenSpace-Camera 模式下 Canvas 的 lossyScale ≠ localScale（如 0.01），混用会导致 Y 方向分布偏差数百倍。X 方向原本已正确使用 `lossyScale.x`
13. **环形阵列圆心**：`middleY` 应为 `bottom + (top - bottom) / 2`，不能用 `left`（历史 Bug 已修复，勿回退）
14. **文档菜单入口**：`UIToolConfig.Menu_OpenDocumentation`（priority 200）。逻辑读取 `UXToolCommonData.DocumentationUrl`；非空时 `Application.OpenURL`；空时 `FindLocalDocPath()` 按序搜索 `Assets/UXTools/Documentation~/` → `Packages/com.ys4fun.uxtools/Documentation~/`，用 `EditorUtility.OpenWithDefaultApp` 打开
15. **组件库拖入 SetParent**：Hierarchy 和 Scene 拖入均须 `SetParent(parent, false)`（worldPositionStays=false），否则全拉伸 RectTransform 在 ScreenSpace-Camera 下会产生 left/top/right/bottom 偏移
16. **组件库 Scene 拖入坐标**：不能用 `GUIPointToWorldRay().GetPoint(0)`（返回相机近裁面点，z 为相机 Z 导致偏差）。应以 `canvas.transform.forward` 为平面法线做射线-Canvas 平面求交，结果 `localPos.z` 强制归零。Plane.Raycast 失败时 fallback 为相机到 Canvas 的距离而非固定值
17. **FindContainerLogic 空引用**：`Selection.gameObjects` 可能含已销毁对象的 null 引用，进入前须 `Array.FindAll` 过滤；prefabStage 分支判断 `transform.parent != null` 后再取 `.parent`
