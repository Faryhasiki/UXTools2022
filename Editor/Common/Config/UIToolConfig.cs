
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace UITool
{
    //UXTools中的路径和常量
    public partial class UIToolConfig
    {
        public static readonly string UXCommonPath = $"{AssetsRootPath}UX-GUI-Editor-Common/";
        public static readonly string UXToolsPath = $"{AssetsRootPath}UX-GUI-Editor-Tools/";
        public static readonly string UXGUIPath = $"{AssetsRootPath}UX-GUI/";

        #region Editor Res
        public static readonly string IconPath = UXToolsPath + "Assets/Editor/Res/Icon/";
        public static readonly string IconCursorPath = UXToolsPath + "Assets/Editor/Res/Cursor/";
        public static readonly string UIBuilderPath = UXToolsPath + "Assets/Editor/Window_uibuilder/";
        #endregion

        #region Package Default Templates (包内只读，仅作为默认模板参考)
        public static readonly string WidgetDefaultTemplatePath = UXToolsPath + "Assets/Editor/Settings/Widget/";
        public static readonly string WidgetLibraryDefaultLabel = "All";
        #endregion

        #region Editor Data (Git同步，固定在Assets下，纯编辑器配置)
        public static readonly string ProjectDataPath = "Assets/UXToolsData/";

        //编辑器设置
        public static readonly string EditorSettingsPath = ProjectDataPath + "EditorSettings/";
        //通用设置
        public static readonly string UXToolCommonDataPath = EditorSettingsPath + "UXToolCommonData.json";
        //辅助线数据（已移至 Library，见 User Local Data 区块）
        //功能开关数据
        public static readonly string SwitchSettingPath = EditorSettingsPath + "SwitchSetting.json";

        //组件库
        public static readonly string WidgetLibraryPath = ProjectDataPath + "WidgetLibrary/";
        //组件库-组件类型数据
        public static readonly string WidgetLabelsPath = WidgetLibraryPath + "WidgetLabels.json";
        //组件库-被认定为组件的Prefab信息
        public static readonly string WidgetListPath = WidgetLibraryPath + "WidgetList.json";

        //设计库-颜色预设旧 JSON 路径（仅用于数据迁移）
        public static readonly string ColorPresetLegacyJsonPath = ProjectDataPath + "DesignLibrary/ColorPresetLibrary.json";
        #endregion

        #region Custom Directories (用户配置父级目录，子目录名称由包规定)
        //自定义配置目录：{customRootPath}/UXToolCustomConfig/
        public static string CustomConfigPath => UXToolsProjectSettings.Instance.GetCustomConfigPath();
        //文字预设目录
        public static string TextPresetPath => CustomConfigPath + "TextPreset/";
        //文字预设资产
        public static string TextPresetAssetPath => TextPresetPath + "TextPresetAsset.asset";
        //颜色预设目录
        public static string ColorPresetPath => CustomConfigPath + "ColorPreset/";
        //颜色预设资产
        public static string ColorPresetAssetPath => ColorPresetPath + "ColorPresetAsset.asset";

        //Editor 扩展目录（可选）：{customRootPath}/UXToolCustomEditor/
        public static string CustomEditorPath => UXToolsProjectSettings.Instance.GetCustomEditorPath();
        //Runtime 扩展目录（可选）：{customRootPath}/UXToolCustomRuntime/
        public static string CustomRuntimePath => UXToolsProjectSettings.Instance.GetCustomRuntimePath();
        #endregion

        #region User Local Data (不入Git，存储在Library中)
        public static readonly string LibraryDataPath = "Library/UXTools/";
        /// <summary>最近打开的 Prefab 列表，仅本地使用。</summary>
        public static readonly string PrefabRecentOpenedPath = LibraryDataPath + "PrefabRecentlyOpenedData.json";
        /// <summary>最近选中的文件列表，仅本地使用。</summary>
        public static readonly string FilesRecentSelectedPath = LibraryDataPath + "FilesRecentlySelectedData.json";
        /// <summary>Scene 窗口 Prefab 多开页签记录，仅本地使用，不应提交至版本控制。</summary>
        public static readonly string PrefabTabsPath = LibraryDataPath + "PrefabTabsData.json";
        /// <summary>SceneView 辅助线布局数据，仅本地使用，不应提交至版本控制。</summary>
        public static readonly string LocationLinesDataPath = LibraryDataPath + "LocationLinesData.json";
        #endregion

        #region MenuItem Name
        public const string MenuName = "UXTool/";
        public const string ToolBar = "工具栏 (Toolbar)";

        public const string Setting = "设置 (Setting)";
        public const string CreateAssets = "新建配置文件 (Create Assets)";

        public const string CommonData = "通用数据 (Common Data)";
        public const string WidgetLibrary = "组件库 (Widget Library)";
        public const string CreateBeginnerGuide = "创建新手引导(Create BeginnerGuide)";
        public const string Localization = "本地化 (Localization)";

        public const string RecentlySelected = "最近选中文件 (Recently Selected)";
        public const string RecentlyOpened = "最近打开 (Recently Opened)";
        public const string PrefabTabs = "Prefab页签 (Prefab Tabs)";


        public const string Menu_Setting = MenuName + Setting;  //-100
        public const string Menu_CreateAssets = MenuName + CreateAssets;    //-99  -50到-1留给配置文件进行排序

        public const string DesignLibrary = "设计库 (Design Library)";
        public const string Menu_DesignLibrary = MenuName + DesignLibrary;

        public const string Menu_WidgetLibrary = Menu_DesignLibrary + "/" + WidgetLibrary;
        public const string Menu_TextPresets = Menu_DesignLibrary + "/文字预设 (Text Presets)";
        public const string Menu_ColorPresets = Menu_DesignLibrary + "/颜色预设 (Color Presets)";
        public const string Menu_Localization = MenuName + Localization;    //54
        public const string Menu_CreateBeginnerGuide = MenuName + CreateBeginnerGuide;  //55

        public const string Menu_ToolBar = MenuName + ToolBar;  //101

        public const string Menu_RecentlyOpened = MenuName + RecentlyOpened;    //153
        public const string Menu_RecentlySelected = MenuName + RecentlySelected; // 154

        public const string Menu_OpenDocumentation = MenuName + "文档 (Documentation)";  // 200

        public const string Menu_UXToolLocalization = "UXToolLocalization";
        public const string Menu_ReferenceLine = "辅助线 (Reference Line)";
        #endregion

        #region EditorPref Name
        //用于存储需要在Play状态前后保持，但是又没有重要到需要持久化的Editor数据
        //其实就是持久化数据的简便做法
        #endregion

        #region Const Color
        // RGBA(60, 60, 60, 1)
        public static Color disableColor = new Color(0.235f, 0.235f, 0.235f, 1f);
        // RGBA(65, 65, 65, 1)
        public static Color hoverColor = new Color(0.255f, 0.255f, 0.255f, 1f);
        // RGBA(51, 51, 51, 1)
        public static Color normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);

        #endregion

        #region Prefab
        public static readonly int m_maxCharacters = 20;
        public static readonly int m_minCharacters = 13;
        public static readonly int m_maxWidth = 150;
        public static readonly int m_minWidth = 100;
        #endregion
    }
}
