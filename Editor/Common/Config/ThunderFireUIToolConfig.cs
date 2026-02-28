
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace ThunderFireUITool
{
    //UXTools中的路径和常量
    public partial class ThunderFireUIToolConfig
    {
        public static readonly string UXCommonPath = $"{AssetsRootPath}UX-GUI-Editor-Common/";
        public static readonly string UXToolsPath = $"{AssetsRootPath}UX-GUI-Editor-Tools/";
        public static readonly string UXGUIPath = $"{AssetsRootPath}UX-GUI/";

        #region Editor Res
        public static readonly string IconPath = UXToolsPath + "Assets/Editor/Res/Icon/";
        public static readonly string IconCursorPath = UXToolsPath + "Assets/Editor/Res/Cursor/";
        public static readonly string UIBuilderPath = UXToolsPath + "Assets/Editor/Window_uibuilder/";
        public static readonly string ScenePath = UXToolsPath + "Assets/Editor/Scene/";
        #endregion

        #region Widget Setting
        public static readonly string WidgetLibrarySettingsPath = UXToolsPath + "Assets/Editor/Settings/Widget/";
        //组件库-组件类型数据
        public static readonly string WidgetLabelsPath = WidgetLibrarySettingsPath + "WidgetLabels.json";
        //组件库-被认定为组件的Prefab信息
        public static readonly string WidgetListPath = WidgetLibrarySettingsPath + "WidgetList.json";

        public static readonly string WidgetLibraryDefaultLabel = "All";
        #endregion



        #region User Data
        public static readonly string UserDataPath = UXToolsPath + "UserDatas/Editor/";
        //Common数据 目前包括: 标题
        public static readonly string UXToolCommonDataPath = UserDataPath + "UXToolCommonData.asset";
        //辅助线数据
        public static readonly string LocationLinesDataPath = UserDataPath + "LocationLinesData.json";
        //最近打开的Prefab数据
        public static readonly string PrefabRecentOpenedPath = UserDataPath + "PrefabRecentlyOpenedData.json";
        //Scene窗口Tab页签数据
        public static readonly string PrefabTabsPath = UserDataPath + "PrefabTabsData.json";
        //快速背景图数据
        //功能开关数据
        public static readonly string SwitchSettingPath = UserDataPath + "SwitchSetting.json";
        //最近选中文件数据
        public static readonly string FilesRecentSelectedPath = UserDataPath + "FilesRecentlySelectedData.json";
        //工具全局数据
        public static readonly string GlobalDataPath = $"{UXCommonPath}Assets/Editor/ToolGlobalData/ToolGlobalData.json";
        #endregion

        #region MenuItem Name
        public const string MenuName = "ThunderFireUXTool/";
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

        public const string Menu_WidgetLibrary = MenuName + WidgetLibrary;  //51
        public const string Menu_Localization = MenuName + Localization;    //54
        public const string Menu_CreateBeginnerGuide = MenuName + CreateBeginnerGuide;  //55

        public const string Menu_ToolBar = MenuName + ToolBar;  //101

        public const string Menu_RecentlyOpened = MenuName + RecentlyOpened;    //153
        public const string Menu_RecentlySelected = MenuName + RecentlySelected; // 154

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
