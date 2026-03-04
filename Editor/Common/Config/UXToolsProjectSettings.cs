#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UITool
{
    public class UXToolsProjectSettings : ScriptableObject
    {
        private const string SettingsAssetPath = "Assets/UXToolsData/EditorSettings/UXToolsProjectSettings.asset";

        [Tooltip("运行时资源存放目录，用于 Bundle 管理等。修改后需重新创建或迁移资源。")]
        public string runtimeAssetsPath = "Assets/UXToolsData/RuntimeAssets/";

        #region Singleton

        private static UXToolsProjectSettings _instance;

        public static UXToolsProjectSettings Instance
        {
            get
            {
                if (_instance != null) return _instance;

                _instance = AssetDatabase.LoadAssetAtPath<UXToolsProjectSettings>(SettingsAssetPath);
                if (_instance != null) return _instance;

                _instance = CreateInstance<UXToolsProjectSettings>();

                string dir = Path.GetDirectoryName(SettingsAssetPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                AssetDatabase.CreateAsset(_instance, SettingsAssetPath);
                AssetDatabase.SaveAssets();
                return _instance;
            }
        }

        #endregion

        #region Path Helpers

        public string EnsureTrailingSlash(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;
            if (!path.EndsWith("/") && !path.EndsWith("\\"))
                path += "/";
            return path;
        }

        public string GetRuntimeAssetsPath()
        {
            return EnsureTrailingSlash(runtimeAssetsPath);
        }

        #endregion

        #region Migration

        public static void MigrateRuntimeAssets(string oldPath, string newPath)
        {
            oldPath = Instance.EnsureTrailingSlash(oldPath);
            newPath = Instance.EnsureTrailingSlash(newPath);

            if (oldPath == newPath) return;
            if (!Directory.Exists(oldPath)) return;

            if (!Directory.Exists(newPath))
                Directory.CreateDirectory(newPath);

            string[] assetGuids = AssetDatabase.FindAssets("", new[] { oldPath.TrimEnd('/') });
            int moved = 0;

            foreach (string guid in assetGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(assetPath)) continue;
                if (AssetDatabase.IsValidFolder(assetPath)) continue;

                string relativeName = assetPath.Substring(oldPath.Length);
                string destPath = newPath + relativeName;

                string destDir = Path.GetDirectoryName(destPath);
                if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);

                string result = AssetDatabase.MoveAsset(assetPath, destPath);
                if (string.IsNullOrEmpty(result))
                    moved++;
                else
                    Debug.LogWarning($"[UXTools] 迁移失败: {assetPath} -> {destPath}: {result}");
            }

            if (moved > 0)
            {
                AssetDatabase.Refresh();
                Debug.Log($"[UXTools] 已迁移 {moved} 个运行时资源: {oldPath} -> {newPath}");
            }
        }

        #endregion

        #region Asset Creation

        public static void CreateSettingsAsset()
        {
            if (AssetDatabase.LoadAssetAtPath<UXToolsProjectSettings>(SettingsAssetPath) != null)
                return;
            var _ = Instance;
        }

        #endregion
    }
}
#endif
