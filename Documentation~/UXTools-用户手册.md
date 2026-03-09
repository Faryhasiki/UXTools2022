# UXTools 用户手册

> 面向策划和 UE 的使用教程

---

## 一、工具面板介绍

### 1.1 ToolBar（Scene 底部工具栏）

通过菜单 **UXTool → 工具栏 (Toolbar)** 启用或关闭，位于 Scene 面板底部。

| 按钮 | 功能 |
|------|------|
| 最近打开 | 打开最近打开的 Prefab 面板 |
| 创建文字 | 在 Scene 中拖拽创建 UXText |
| 创建图片 | 在 Scene 中拖拽创建 UXImage |
| 辅助线 | 创建横向/纵向参考辅助线 |
| 设计库 | 同时打开组件库、颜色预设、文字预设三个面板（合并为 Tab 页签） |
| 新建组件 | 将选中节点收纳为组件 |
| 更多 | 打开设置面板 |

点击工具栏左侧三角按钮可折叠/展开。

### 1.2 快捷创建 UI 元素

1. 点击工具栏「创建图片」或「创建文字」按钮
2. 在 Scene 中**按住左键拖拽**绘制矩形区域
3. 松开鼠标后自动创建对应的 UXImage 或 UXText
4. 如果场景中没有 Canvas，会自动创建一个标准的 ScreenSpaceOverlay Canvas

> 注意：请在 Canvas 范围内拖拽，在 Canvas 外部创建的元素会使用默认尺寸。

### 1.3 辅助线

通过工具栏「辅助线」按钮创建：
- **横向辅助线**：水平参考线
- **纵向辅助线**：垂直参考线

操作：
- **拖拽**辅助线可移动位置
- 靠近 UI 元素边缘时会**自动吸附**（支持 ScreenSpaceOverlay / ScreenSpace-Camera / WorldSpace 三种 Canvas 模式）
- **右键**辅助线可删除
- 工具栏「辅助线」下拉菜单可一键**清除所有辅助线**

> 辅助线布局数据保存在本机 `Library/UXTools/LocationLinesData.json`，不会提交到版本控制。

### 1.4 Prefab 页签

Scene 面板上方显示当前打开的所有 Prefab 页签，点击可快速切换，点击 × 关闭。

> 页签记录保存在本机 `Library/UXTools/PrefabTabsData.json`，不会提交到版本控制。

### 1.5 设计库面板

菜单路径：**UXTool → 设计库 (Design Library)**，或点击工具栏「设计库」按钮。

点击后会**同时打开**以下三个窗口，在 Unity 编辑器中合并为 Tab 页签：

| 子面板 | 说明 |
|--------|------|
| 组件库 (Widget Library) | 管理可复用的 UI Prefab |
| 颜色预设 (Color Presets) | 管理颜色预设，支持拖拽绑定与代码生成 |
| 文字预设 (Text Presets) | 管理字体样式预设，支持拖拽绑定与代码生成 |

各子面板的详细说明见第二章「设计库」。

### 1.6 最近打开面板

菜单路径：**UXTool → 最近打开 (Recently Opened)**，或点击工具栏「最近打开」按钮。

- 列出最近打开过的 Prefab，点击任意一项可快速在 Prefab 编辑模式中打开
- 列表最大数量可在 **设置 → 通用 → 最近打开模板数上限** 中配置
- 需在 **设置 → 功能开关** 中开启「最近打开面板记录」才会记录历史

### 1.7 最近选中文件面板

菜单路径：**UXTool → 最近选中文件 (Recently Selected)**

- 列出最近在 Project 窗口中选中过的资产，点击任意一项可快速定位并选中该资产
- 列表最大数量可在 **设置 → 通用 → 最近选中文件数上限** 中配置
- 需在 **设置 → 功能开关** 中开启「最近选中面板记录」才会记录历史

### 1.8 文档

菜单路径：**UXTool → 文档 (Documentation)**

- 若在 **设置 → 通用 → 文档链接 URL** 中填写了地址，则在浏览器中打开对应 URL
- 未填写时自动打开本包内的 `Documentation~/UXTools-用户手册.md` 本地文档

---

## 二、设计库

### 2.1 颜色预设

菜单路径：**UXTool → 设计库 → 颜色预设 (Color Presets)**

三栏布局：
- **左栏**：分类管理（点击 + 添加分类，右键删除分类）
- **中栏**：预设列表（点击 + 添加预设，右键可复制 ID 或删除）
- **右栏**：详情编辑（名称、描述、颜色值、不透明度）

#### 使用预设

1. **Inspector 选择**：在 UXText / UXImage 的 Inspector 中，从「颜色类型」下拉列表选择
2. **拖拽绑定**：从颜色预设窗口的列表项拖拽到 Hierarchy 中的 UI 元素上
3. 拖拽到普通 Image 上时会自动转换为 UXImage

#### 工具栏按钮

| 按钮 | 功能 |
|------|------|
| 生成代码 | 为勾选了「生成代码」的预设生成 C# 常量定义 |
| 重建索引 | 扫描项目中所有引用此预设库的预制体和场景 |
| 强制同步全部 | 对所有追踪资产重新应用预设值 |

### 2.2 文字预设

菜单路径：**UXTool → 设计库 → 文字预设 (Text Presets)**

与颜色预设窗口结构一致，管理以下属性：
- 字体资产（TMP_FontAsset）
- 字体样式（粗体、斜体、下划线、删除线、大写、小写、小型大写）
- 字号
- 行间距、字符间距

使用方式与颜色预设相同（Inspector 选择 / 拖拽绑定）。

### 2.3 代码生成

颜色和文字预设都支持代码生成功能：

1. 在预设详情面板勾选「生成代码」
2. （可选）填写「代码别名」，用于生成更符合程序命名风格的常量名
3. 点击窗口工具栏的「生成代码」按钮
4. 生成的 C# 文件位于 `UXToolsCustomRuntime/Generated/` 目录下

代码别名说明：
- 预设名称由 UE/美术定义，如 `渐变蓝-悬浮态`
- 代码别名由程序定义，如 `HoverBlue`
- 填写别名后，生成的字段名使用别名，不填则使用预设名称
- 别名输入框仅在勾选「生成代码」时显示

生成的代码示例：
```csharp
// UXColorDef.cs
public static class UXColorDef
{
    /// <summary>渐变蓝-悬浮态 (#4488FF, 100%)</summary>
    public static readonly UXColorKey HoverBlue = new UXColorKey("guid", "渐变蓝-悬浮态");
}
```

### 2.4 组件库

菜单路径：**UXTool → 设计库 → 组件库 (Widget Library)**

管理可复用的 UI Prefab：
- 按标签分类筛选
- 拖拽到 Scene 中使用
- 通过工具栏「新建组件」或右键 Assets 中的 Prefab → **设置为组件** 添加

---

## 三、组件说明

### 3.1 UXText

创建方式：
- 菜单 **GameObject → UI (Canvas) → UXUI → UXText**
- 工具栏「创建文字」拖拽创建

Inspector 面板：
- **文字预设**：选择预设后自动应用字体、字号、样式、间距
- **颜色预设**：选择预设后自动应用文字颜色
- 保留 TextMeshPro 的全部原始功能

### 3.2 UXImage

创建方式：
- 菜单 **GameObject → UI (Canvas) → UXUI → UXImage**
- 工具栏「创建图片」拖拽创建

Inspector 面板：
- **颜色预设**：选择预设后自动应用图片颜色
- 保留 Image 的全部原始功能（Sprite、填充模式等）
- 普通 Image 可通过 Inspector 顶部的「点击转换为 UXImage」按钮一键转换

### 3.3 UXColorBinding

通用颜色绑定组件，可挂到任何有 Graphic 的 GameObject 上（RawImage、第三方组件等）。

添加方式：**Add Component → UI → UXColorBinding**

---

## 四、设置

菜单路径：**UXTool → 设置 (Setting)**，文档可通过 **UXTool → 文档 (Documentation)** 直接打开。

### 4.1 通用

- **文档链接 URL**：自定义文档地址。留空时点击菜单「文档」打开本地 md 文档；填写 URL 后将直接在浏览器中打开对应链接。
- 最近打开模板数上限
- 最近选中文件数上限

### 4.2 功能开关

| 开关 | 说明 |
|------|------|
| 最近打开面板记录 | 启用后记录打开过的 Prefab |
| 最近选中面板记录 | 启用后记录选中过的文件 |
| 对齐吸附 | 辅助线吸附功能 |
| 右键选择列表 | Scene 中右键显示 UI 选择列表 |
| 快速复制 | 快捷复制功能 |
| 移动快捷键 | 移动快捷键功能 |
| Prefab 多开 | 支持多个 Prefab 同时打开 |

### 4.3 路径设置

配置 UXTools 的外部目录结构：

| 目录 | 说明 | 默认父级路径 |
|------|------|-------------|
| **UXToolsCustomConfig** | 颜色/文字预设等配置资产（始终创建） | Assets/UXToolsData/ |
| **UXToolsCustomEditor** | Editor 扩展脚本（可选，启用后创建） | Assets/UXToolsData/ |
| **UXToolsCustomRuntime** | Runtime 脚本和生成代码（可选，启用后创建） | Assets/UXToolsData/ |

每个目录可独立配置父级路径。修改后会提示迁移已有资产。启用 Editor / Runtime 目录时会自动生成默认的 `.asmdef` 程序集定义文件。

---

## 五、RectTransform 布局工具

选中 UI 元素后，在 Inspector 的 RectTransform 面板中可使用：

**对齐**：上对齐、下对齐、左对齐、右对齐、水平居中、垂直居中

**阵列**：横向排列、纵向排列、环形排列

**组合**：多选 UI 元素后右键 → Combine，将它们包裹到一个父节点中

---

## 六、初始化

首次使用或更新包后，执行菜单 **UXTool → 新建配置文件 → Create All Assets** 初始化所有配置文件。
