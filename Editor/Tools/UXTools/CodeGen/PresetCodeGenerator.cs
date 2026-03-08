#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace UITool
{
    /// <summary>
    /// 预设代码生成器。
    /// 从 ColorPresetAsset / TextPresetAsset 自动生成 C# 常量定义文件，
    /// 使程序可以通过编译安全的方式引用预设，避免字符串硬编码。
    /// 
    /// 生成的代码输出到 UXToolsCustomConfig 目录下，作为运行时可用的定义。
    /// </summary>
    public static class PresetCodeGenerator
    {
        /// <summary>
        /// 生成颜色预设定义文件 UXColorDef.cs
        /// </summary>
        /// <summary>
        /// 从磁盘加载资产后生成（菜单栏调用）
        /// </summary>
        [MenuItem(UIToolConfig.Menu_DesignLibrary + "/生成颜色预设代码 (Generate Color Def)", false, 60)]
        public static void GenerateColorDef()
        {
            AssetDatabase.SaveAssets();
            var asset = AssetDatabase.LoadAssetAtPath<ColorPresetAsset>(UIToolConfig.ColorPresetAssetPath);
            GenerateColorDef(asset);
        }

        /// <summary>
        /// 使用指定资产实例生成（窗口按钮调用，保证使用同一内存实例）
        /// </summary>
        public static void GenerateColorDef(ColorPresetAsset asset)
        {
            if (asset == null)
            {
                EditorUtility.DisplayDialog("生成失败", "未找到颜色预设库资产，请先创建颜色预设。", "确定");
                return;
            }

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);

            string outputDir = GetGeneratedDir();
            string outputPath = outputDir + "UXColorDef.cs";
            string code = BuildColorDefCode(asset);

            File.WriteAllText(outputPath, code, Encoding.UTF8);
            AssetDatabase.Refresh();

            int count = asset.presets.FindAll(e => e.generateCode).Count;
            Debug.Log($"[UXTools] 已生成颜色预设代码: {outputPath}（{count}/{asset.presets.Count} 条）");
            EditorUtility.DisplayDialog("生成完成",
                $"已生成 {count}/{asset.presets.Count} 条颜色预设定义\n（仅生成已勾选「生成代码」的预设）\n路径: {outputPath}", "确定");
        }

#if TMP_PRESENT
        /// <summary>
        /// 生成文字预设定义文件 UXTextStyleDef.cs
        /// </summary>
        [MenuItem(UIToolConfig.Menu_DesignLibrary + "/生成文字预设代码 (Generate TextStyle Def)", false, 61)]
        public static void GenerateTextStyleDef()
        {
            AssetDatabase.SaveAssets();
            var asset = AssetDatabase.LoadAssetAtPath<TextPresetAsset>(UIToolConfig.TextPresetAssetPath);
            GenerateTextStyleDef(asset);
        }

        public static void GenerateTextStyleDef(TextPresetAsset asset)
        {
            if (asset == null)
            {
                EditorUtility.DisplayDialog("生成失败", "未找到文字预设库资产，请先创建文字预设。", "确定");
                return;
            }

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);

            string outputDir = GetGeneratedDir();
            string outputPath = outputDir + "UXTextStyleDef.cs";
            string code = BuildTextStyleDefCode(asset);

            File.WriteAllText(outputPath, code, Encoding.UTF8);
            AssetDatabase.Refresh();

            int count = asset.presets.FindAll(e => e.generateCode).Count;
            Debug.Log($"[UXTools] 已生成文字预设代码: {outputPath}（{count}/{asset.presets.Count} 条）");
            EditorUtility.DisplayDialog("生成完成",
                $"已生成 {count}/{asset.presets.Count} 条文字预设定义\n（仅生成已勾选「生成代码」的预设）\n路径: {outputPath}", "确定");
        }
#endif

        /// <summary>
        /// 获取代码生成输出目录，位于 UXToolsCustomRuntime/Generated/ 下。
        /// 无论 Runtime 扩展目录是否启用，都强制计算并创建路径。
        /// </summary>
        private static string GetGeneratedDir()
        {
            var settings = UXToolsProjectSettings.Instance;

            if (!settings.enableCustomRuntime)
            {
                settings.enableCustomRuntime = true;
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssetIfDirty(settings);
            }

            settings.EnsureOptionalDirectories();

            string runtimePath = UXToolsProjectSettings.EnsureTrailingSlash(settings.runtimeParentPath)
                                 + UXToolsProjectSettings.RUNTIME_DIR_NAME + "/";
            string outputDir = runtimePath + "Generated/";

            string generatedFolder = outputDir.TrimEnd('/');
            if (!AssetDatabase.IsValidFolder(generatedFolder))
            {
                string runtimeFolder = runtimePath.TrimEnd('/');
                if (!AssetDatabase.IsValidFolder(runtimeFolder))
                {
                    string parent = settings.runtimeParentPath.TrimEnd('/');
                    if (!AssetDatabase.IsValidFolder(parent))
                        AssetDatabase.CreateFolder(
                            System.IO.Path.GetDirectoryName(parent)?.Replace('\\', '/') ?? "Assets",
                            System.IO.Path.GetFileName(parent));
                    AssetDatabase.CreateFolder(parent, UXToolsProjectSettings.RUNTIME_DIR_NAME);
                }
                AssetDatabase.CreateFolder(runtimeFolder, "Generated");
            }

            return outputDir;
        }

        #region Code Building

        private static string BuildColorDefCode(ColorPresetAsset asset)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// 此文件由 UXTools 自动生成，请勿手动修改。");
            sb.AppendLine("using UITool;");
            sb.AppendLine();
            sb.AppendLine("namespace UITool.Generated");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// 颜色预设定义常量。");
            sb.AppendLine("    /// 通过 UXText.SetColor() / UXImage.SetColor() 使用。");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    public static class UXColorDef");
            sb.AppendLine("    {");

            var filtered = asset.presets.FindAll(e => e.generateCode);
            var usedNames = new HashSet<string>();

            string lastCategory = null;
            foreach (var entry in filtered)
            {
                if (!string.IsNullOrEmpty(entry.category) && entry.category != lastCategory)
                {
                    if (lastCategory != null) sb.AppendLine();
                    sb.AppendLine($"        #region {entry.category}");
                    sb.AppendLine();
                    lastCategory = entry.category;
                }

                string rawName = !string.IsNullOrEmpty(entry.codeAlias) ? entry.codeAlias : entry.presetName;
                string fieldName = UniqueIdentifier(SanitizeIdentifier(rawName), usedNames);
                string colorHex = $"#{entry.hex}";
                sb.AppendLine($"        /// <summary>{EscapeXml(entry.presetName)} ({colorHex}, {entry.opacity}%)</summary>");
                sb.AppendLine($"        public static readonly UXColorKey {fieldName} = new UXColorKey(\"{entry.id}\", \"{EscapeCSharpString(entry.presetName)}\");");

                if (!string.IsNullOrEmpty(entry.category) &&
                    entry == filtered.FindLast(e => e.category == entry.category))
                {
                    sb.AppendLine();
                    sb.AppendLine($"        #endregion");
                }
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

#if TMP_PRESENT
        private static string BuildTextStyleDefCode(TextPresetAsset asset)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// 此文件由 UXTools 自动生成，请勿手动修改。");
            sb.AppendLine("using UITool;");
            sb.AppendLine();
            sb.AppendLine("namespace UITool.Generated");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// 文字预设定义常量。");
            sb.AppendLine("    /// 通过 UXText.SetTextStyle() 使用。");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    public static class UXTextStyleDef");
            sb.AppendLine("    {");

            var filtered = asset.presets.FindAll(e => e.generateCode);
            var usedNames = new HashSet<string>();

            string lastCategory = null;
            foreach (var entry in filtered)
            {
                if (!string.IsNullOrEmpty(entry.category) && entry.category != lastCategory)
                {
                    if (lastCategory != null) sb.AppendLine();
                    sb.AppendLine($"        #region {entry.category}");
                    sb.AppendLine();
                    lastCategory = entry.category;
                }

                string rawName = !string.IsNullOrEmpty(entry.codeAlias) ? entry.codeAlias : entry.presetName;
                string fieldName = UniqueIdentifier(SanitizeIdentifier(rawName), usedNames);
                sb.AppendLine($"        /// <summary>{EscapeXml(entry.presetName)} (字号:{entry.fontSize})</summary>");
                sb.AppendLine($"        public static readonly UXTextStyleKey {fieldName} = new UXTextStyleKey(\"{entry.id}\", \"{EscapeCSharpString(entry.presetName)}\");");

                if (!string.IsNullOrEmpty(entry.category) &&
                    entry == filtered.FindLast(e => e.category == entry.category))
                {
                    sb.AppendLine();
                    sb.AppendLine($"        #endregion");
                }
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }
#endif

        #endregion

        #region String Utilities

        /// <summary>
        /// 将预设名称转换为合法的 C# 标识符。
        /// 支持中文、英文、数字，非法字符替换为下划线。
        /// </summary>
        private static string SanitizeIdentifier(string name)
        {
            if (string.IsNullOrEmpty(name)) return "_Unnamed";

            string sanitized = Regex.Replace(name, @"[^\w\u4e00-\u9fff]", "_");

            if (char.IsDigit(sanitized[0]))
                sanitized = "_" + sanitized;

            if (string.IsNullOrEmpty(sanitized) || sanitized == "_")
                sanitized = "_Unnamed";

            return sanitized;
        }

        /// <summary>
        /// 确保标识符在集合中唯一，重复时追加 _2、_3 等后缀
        /// </summary>
        private static string UniqueIdentifier(string name, HashSet<string> used)
        {
            string result = name;
            int suffix = 2;
            while (!used.Add(result))
                result = $"{name}_{suffix++}";
            return result;
        }

        private static string EscapeCSharpString(string s)
        {
            return s?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? "";
        }

        private static string EscapeXml(string s)
        {
            return s?.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;") ?? "";
        }

        #endregion
    }
}
#endif
