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
            ValidateDirectories();

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

            CreateColorPresetAsset();

#if TMP_PRESENT
            CreateTextPresetAsset();
#endif

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[UXTools] 配置文件创建/校验完成。");
        }

        private static void ValidateDirectories()
        {
            string[] requiredDirs = new[]
            {
                UIToolConfig.EditorSettingsPath,
                UIToolConfig.WidgetLibraryPath,
                UIToolConfig.CustomConfigPath,
                UIToolConfig.TextPresetPath,
                UIToolConfig.ColorPresetPath,
            };

            foreach (string dir in requiredDirs)
            {
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
            }

            UXToolsProjectSettings.Instance.EnsureOptionalDirectories();
        }

        /// <summary>
        /// 创建颜色预设 ScriptableObject，若存在旧 JSON 数据则自动迁移
        /// </summary>
        private static void CreateColorPresetAsset()
        {
            string assetPath = UIToolConfig.ColorPresetAssetPath;
            var existing = AssetDatabase.LoadAssetAtPath<ColorPresetAsset>(assetPath);
            if (existing != null) return;

            string dir = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var asset = ScriptableObject.CreateInstance<ColorPresetAsset>();

            string legacyJson = UIToolConfig.ColorPresetLegacyJsonPath;
            if (File.Exists(legacyJson))
            {
                string json = File.ReadAllText(legacyJson);
                JsonUtility.FromJsonOverwrite(json, asset);
                Debug.Log($"[UXTools] 已从旧 JSON 迁移 {asset.presets.Count} 条颜色预设。");
            }

            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
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