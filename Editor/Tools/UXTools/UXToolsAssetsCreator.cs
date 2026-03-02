#if UNITY_EDITOR
using UnityEditor;

namespace UITool
{
    public static class UXToolsAssetsCreator
    {
        /// <summary>
        /// 初始化所有的配置文件, 出包时使用
        /// </summary>
        [MenuItem(UIToolConfig.Menu_CreateAssets + "/Create All Assets", false, -99)]
        public static void CreateAllAssets()
        {
#if UXTOOLS_DEV
            //UXTool Localization
            LocalizationDecode.Decode();
            InspectorLocalizationDecode.Decode();
            LocalizationDecode.BuildUIScript();
            EditorLocalizationSettings.Create();
#endif

            WidgetLabelsSettings.Create();
            WidgetListSetting.Create();

            CreateLocationLinesData.Create();
            PrefabOpenedSetting.Create();
            RecentFilesSetting.Create();

            PrefabTabsData.Create();

            SwitchSetting.Create();

            JsonAssetManager.CreateAssets<UXToolCommonData>(UIToolConfig.UXToolCommonDataPath);
            JsonAssetManager.CreateAssets<TextPresetLibrary>(UIToolConfig.TextPresetLibraryPath);
            JsonAssetManager.CreateAssets<ColorPresetLibrary>(UIToolConfig.ColorPresetLibraryPath);
        }
    }
}

#endif