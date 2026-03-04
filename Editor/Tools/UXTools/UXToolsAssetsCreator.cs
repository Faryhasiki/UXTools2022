#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

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

            UXToolsProjectSettings.CreateSettingsAsset();

            JsonAssetManager.CreateAssets<UXToolCommonData>(UIToolConfig.UXToolCommonDataPath);
            JsonAssetManager.CreateAssets<ColorPresetLibrary>(UIToolConfig.ColorPresetLibraryPath);

#if TMP_PRESENT
            CreateTextPresetAsset();
#endif
        }

#if TMP_PRESENT
        private static void CreateTextPresetAsset()
        {
            string path = UIToolConfig.TextPresetAssetPath;
            if (AssetDatabase.LoadAssetAtPath<TextPresetAsset>(path) != null) return;

            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var asset = ScriptableObject.CreateInstance<TextPresetAsset>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
        }
#endif
    }
}

#endif