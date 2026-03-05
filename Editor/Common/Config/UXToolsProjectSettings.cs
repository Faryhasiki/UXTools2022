#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace UITool
{
    /// <summary>
    /// UXTools 项目级设置，持久化为 ScriptableObject。
    /// 三个固定名称的子目录各自可独立配置父级路径：
    /// - UXToolsCustomConfig/  — 颜色/文字预设等配置资产（始终创建）
    /// - UXToolsCustomEditor/  — Editor 扩展脚本（可选）
    /// - UXToolsCustomRuntime/ — Runtime 扩展脚本（可选）
    /// </summary>
    public class UXToolsProjectSettings : ScriptableObject
    {
        private const string SETTINGS_ASSET_PATH = "Assets/UXToolsData/EditorSettings/UXToolsProjectSettings.asset";

        #region 固定子目录名称

        /// <summary>
        /// 配置资产目录名称（存放颜色预设、文字预设等）
        /// </summary>
        public const string CONFIG_DIR_NAME = "UXToolsCustomConfig";

        /// <summary>
        /// Editor 扩展目录名称
        /// </summary>
        public const string EDITOR_DIR_NAME = "UXToolsCustomEditor";

        /// <summary>
        /// Runtime 扩展目录名称
        /// </summary>
        public const string RUNTIME_DIR_NAME = "UXToolsCustomRuntime";

        #endregion

        #region 可配置字段

        /// <summary>
        /// UXToolCustomConfig 的父级路径。
        /// 最终配置目录 = {configParentPath}/UXToolCustomConfig/
        /// </summary>
        [Tooltip("UXToolCustomConfig 的父级路径")]
        [FormerlySerializedAs("runtimeAssetsPath")]
        [FormerlySerializedAs("customConfigPath")]
        [FormerlySerializedAs("customRootPath")]
        public string configParentPath = "Assets/UXToolsData/";

        /// <summary>
        /// UXToolCustomEditor 的父级路径（启用后生效）
        /// </summary>
        [Tooltip("UXToolCustomEditor 的父级路径")]
        public string editorParentPath = "Assets/UXToolsData/";

        /// <summary>
        /// 是否启用 Editor 扩展目录
        /// </summary>
        public bool enableCustomEditor = false;

        /// <summary>
        /// UXToolCustomRuntime 的父级路径（启用后生效）
        /// </summary>
        [Tooltip("UXToolCustomRuntime 的父级路径")]
        public string runtimeParentPath = "Assets/UXToolsData/";

        /// <summary>
        /// 是否启用 Runtime 扩展目录
        /// </summary>
        public bool enableCustomRuntime = false;

        #endregion

        #region Singleton

        private static UXToolsProjectSettings _instance;

        /// <summary>
        /// 全局单例，自动从 Asset 加载或创建
        /// </summary>
        public static UXToolsProjectSettings Instance
        {
            get
            {
                if (_instance != null) return _instance;

                _instance = AssetDatabase.LoadAssetAtPath<UXToolsProjectSettings>(SETTINGS_ASSET_PATH);
                if (_instance != null) return _instance;

                _instance = CreateInstance<UXToolsProjectSettings>();

                string dir = Path.GetDirectoryName(SETTINGS_ASSET_PATH);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                AssetDatabase.CreateAsset(_instance, SETTINGS_ASSET_PATH);
                AssetDatabase.SaveAssets();
                return _instance;
            }
        }

        #endregion

        #region Path Helpers

        /// <summary>
        /// 确保路径以 / 结尾
        /// </summary>
        public static string EnsureTrailingSlash(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;
            if (!path.EndsWith("/") && !path.EndsWith("\\"))
                path += "/";
            return path;
        }

        /// <summary>
        /// 获取配置目录完整路径：{configParentPath}/UXToolCustomConfig/
        /// </summary>
        public string GetCustomConfigPath()
        {
            return EnsureTrailingSlash(configParentPath) + CONFIG_DIR_NAME + "/";
        }

        /// <summary>
        /// 获取 Editor 扩展目录完整路径，未启用则返回空
        /// </summary>
        public string GetCustomEditorPath()
        {
            if (!enableCustomEditor) return "";
            return EnsureTrailingSlash(editorParentPath) + EDITOR_DIR_NAME + "/";
        }

        /// <summary>
        /// 获取 Runtime 扩展目录完整路径，未启用则返回空
        /// </summary>
        public string GetCustomRuntimePath()
        {
            if (!enableCustomRuntime) return "";
            return EnsureTrailingSlash(runtimeParentPath) + RUNTIME_DIR_NAME + "/";
        }

        /// <summary>
        /// 兼容旧版 API
        /// </summary>
        public string GetRuntimeAssetsPath()
        {
            return GetCustomConfigPath();
        }

        #endregion

        #region Migration

        /// <summary>
        /// 移动整个文件夹到新的父级目录下。
        /// 使用 AssetDatabase.MoveAsset 直接移动文件夹，保留所有 GUID 引用。
        /// </summary>
        /// <param name="dirName">固定文件夹名称（如 UXToolCustomConfig）</param>
        /// <param name="oldParent">旧父级路径（如 Assets/）</param>
        /// <param name="newParent">新父级路径（如 Assets/MyProject/）</param>
        /// <returns>是否成功移动</returns>
        public static bool MoveDirectory(string dirName, string oldParent, string newParent)
        {
            oldParent = EnsureTrailingSlash(oldParent);
            newParent = EnsureTrailingSlash(newParent);

            if (oldParent == newParent) return false;

            string oldFullPath = oldParent + dirName;
            string newFullPath = newParent + dirName;

            if (!AssetDatabase.IsValidFolder(oldFullPath))
                return false;

            if (!Directory.Exists(newParent))
                Directory.CreateDirectory(newParent);

            string result = AssetDatabase.MoveAsset(oldFullPath, newFullPath);
            if (string.IsNullOrEmpty(result))
            {
                AssetDatabase.Refresh();
                Debug.Log($"[UXTools] 已移动目录: {oldFullPath} → {newFullPath}");
                return true;
            }

            Debug.LogWarning($"[UXTools] 移动目录失败: {oldFullPath} → {newFullPath}: {result}");
            return false;
        }

        /// <summary>
        /// 确保可选目录存在（已启用且路径不为空时创建），并生成默认程序集定义
        /// </summary>
        public void EnsureOptionalDirectories()
        {
            string editorPath = GetCustomEditorPath();
            if (!string.IsNullOrEmpty(editorPath))
            {
                if (!Directory.Exists(editorPath))
                    Directory.CreateDirectory(editorPath);
                CreateAsmdef(editorPath, EDITOR_DIR_NAME, true);
            }

            string runtimePath = GetCustomRuntimePath();
            if (!string.IsNullOrEmpty(runtimePath))
            {
                if (!Directory.Exists(runtimePath))
                    Directory.CreateDirectory(runtimePath);
                CreateAsmdef(runtimePath, RUNTIME_DIR_NAME, false);
            }
        }

        /// <summary>
        /// 在指定目录下创建默认 .asmdef 文件
        /// </summary>
        /// <param name="dirPath">目标目录路径</param>
        /// <param name="asmName">程序集名称</param>
        /// <param name="editorOnly">是否限定为 Editor 平台</param>
        private static void CreateAsmdef(string dirPath, string asmName, bool editorOnly)
        {
            string asmdefPath = EnsureTrailingSlash(dirPath) + asmName + ".asmdef";
            if (File.Exists(asmdefPath)) return;

            string platformsJson = editorOnly
                ? "\n    \"includePlatforms\": [\"Editor\"],"
                : "\n    \"includePlatforms\": [],";

            string referencesJson = editorOnly
                ? $"\n    \"references\": [\n        \"GUID:6f708d21f7f40eb458bbbe31a2e60f78\",\n        \"GUID:df5879f3e3513d54b91782afbac61c1f\"\n    ],"
                : $"\n    \"references\": [\n        \"GUID:6f708d21f7f40eb458bbbe31a2e60f78\"\n    ],";

            string content = "{"
                + $"\n    \"name\": \"{asmName}\","
                + $"\n    \"rootNamespace\": \"\","
                + referencesJson
                + platformsJson
                + "\n    \"excludePlatforms\": [],"
                + "\n    \"allowUnsafeCode\": false,"
                + "\n    \"overrideReferences\": false,"
                + "\n    \"precompiledReferences\": [],"
                + "\n    \"autoReferenced\": true,"
                + "\n    \"defineConstraints\": [],"
                + "\n    \"versionDefines\": [],"
                + "\n    \"noEngineReferences\": false"
                + "\n}";

            File.WriteAllText(asmdefPath, content, System.Text.Encoding.UTF8);
            AssetDatabase.Refresh();
            Debug.Log($"[UXTools] 已创建程序集定义: {asmdefPath}");
        }

        #endregion

        #region Asset Creation

        /// <summary>
        /// 确保设置资产已创建
        /// </summary>
        public static void CreateSettingsAsset()
        {
            if (AssetDatabase.LoadAssetAtPath<UXToolsProjectSettings>(SETTINGS_ASSET_PATH) != null)
                return;
            var _ = Instance;
        }

        #endregion
    }
}
#endif
