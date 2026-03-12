#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace UIAnimation
{
    /// <summary>
    /// 动画触发代码生成器。
    /// 根据 AnimationTriggerAuthoring 中预先声明的触发槽位，生成正式的触发组件代码。
    /// </summary>
    public static class AnimationTriggerCodeGenerator
    {
        public static bool Generate(AnimationTriggerAuthoring authoring, Type baseType, string outputPath, out string message)
        {
            message = string.Empty;

            if (authoring == null)
            {
                message = "未找到 AnimationTriggerAuthoring 实例。";
                return false;
            }

            if (string.IsNullOrWhiteSpace(authoring.GeneratedClassName))
            {
                message = "请先填写生成类名。";
                return false;
            }

            if (baseType == null)
            {
                message = "请先选择有效的基类脚本。";
                return false;
            }

            if (!typeof(MonoBehaviour).IsAssignableFrom(baseType))
            {
                message = "基类必须继承 MonoBehaviour。";
                return false;
            }

            var slots = CollectValidSlots(authoring);
            if (slots.Count == 0)
            {
                message = "至少需要配置一个有效触发点方法名。";
                return false;
            }

            try
            {
                var absolutePath = ToAbsolutePath(outputPath);
                File.WriteAllText(absolutePath, BuildCode(authoring, baseType, slots), Encoding.UTF8);
                AssetDatabase.Refresh();
                message = $"已生成触发代码：{outputPath}";
                return true;
            }
            catch (Exception ex)
            {
                message = $"生成失败：{ex.Message}";
                return false;
            }
        }

        private static List<AnimationTriggerSlot> CollectValidSlots(AnimationTriggerAuthoring authoring)
        {
            var list = new List<AnimationTriggerSlot>();
            var usedNames = new HashSet<string>();

            foreach (var slot in authoring.TriggerSlots)
            {
                if (slot == null
                    || string.IsNullOrWhiteSpace(slot.Id)
                    || string.IsNullOrWhiteSpace(slot.CodeMethodName))
                    continue;

                if (usedNames.Add(slot.CodeMethodName))
                    list.Add(slot);
            }

            return list;
        }

        private static string BuildCode(AnimationTriggerAuthoring authoring, Type baseType, List<AnimationTriggerSlot> slots)
        {
            var sb = new StringBuilder();
            var className = SanitizeIdentifier(authoring.GeneratedClassName);
            var namespaceName = authoring.GeneratedNamespace?.Trim();
            var baseTypeName = GetTypeName(baseType);

            sb.AppendLine("// 此文件由 UIAnimation 自动生成，请按需扩展。");
            sb.AppendLine("using UIAnimation;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(namespaceName))
            {
                sb.AppendLine($"namespace {namespaceName}");
                sb.AppendLine("{");
            }

            var indent = string.IsNullOrWhiteSpace(namespaceName) ? string.Empty : "    ";

            sb.AppendLine($"{indent}/// <summary>");
            sb.AppendLine($"{indent}/// 基于 AnimationTriggerAuthoring 自动生成的触发桥接组件。");
            sb.AppendLine($"{indent}/// 通过组合引用 Authoring 组件，避免业务类继承作者工具组件。");
            sb.AppendLine($"{indent}/// </summary>");
            sb.AppendLine($"{indent}[RequireComponent(typeof(AnimationTriggerAuthoring))]");
            sb.AppendLine($"{indent}public class {className} : {baseTypeName}");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    [SerializeField] private AnimationTriggerAuthoring _animationTriggerAuthoring;");
            sb.AppendLine();
            sb.AppendLine($"{indent}    private AnimationTriggerAuthoring TriggerAuthoring");
            sb.AppendLine($"{indent}    {{");
            sb.AppendLine($"{indent}        get");
            sb.AppendLine($"{indent}        {{");
            sb.AppendLine($"{indent}            if (_animationTriggerAuthoring == null)");
            sb.AppendLine($"{indent}                _animationTriggerAuthoring = GetComponent<AnimationTriggerAuthoring>();");
            sb.AppendLine();
            sb.AppendLine($"{indent}            return _animationTriggerAuthoring;");
            sb.AppendLine($"{indent}        }}");
            sb.AppendLine($"{indent}    }}");
            sb.AppendLine();

            var usedMethodNames = new HashSet<string>();
            foreach (var slot in slots)
            {
                var methodName = UniqueIdentifier(SanitizeIdentifier(slot.CodeMethodName), usedMethodNames);
                var displayName = EscapeCSharpString(string.IsNullOrWhiteSpace(slot.DisplayName)
                    ? slot.CodeMethodName
                    : slot.DisplayName);
                var triggerId = EscapeCSharpString(slot.Id);

                sb.AppendLine($"{indent}    /// <summary>");
                sb.AppendLine($"{indent}    /// 触发时机：{displayName}");
                sb.AppendLine($"{indent}    /// </summary>");
                sb.AppendLine($"{indent}    public void {methodName}()");
                sb.AppendLine($"{indent}    {{");
                sb.AppendLine($"{indent}        if (TriggerAuthoring != null)");
                sb.AppendLine($"{indent}            TriggerAuthoring.FireById(\"{triggerId}\");");
                sb.AppendLine($"{indent}    }}");
                sb.AppendLine();
            }

            sb.AppendLine($"{indent}}}");

            if (!string.IsNullOrWhiteSpace(namespaceName))
                sb.AppendLine("}");

            return sb.ToString();
        }

        private static string SanitizeIdentifier(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "_Unnamed";

            var sanitized = Regex.Replace(name.Trim(), @"[^\w\u4e00-\u9fff]", "_");
            if (char.IsDigit(sanitized[0]))
                sanitized = "_" + sanitized;

            return sanitized;
        }

        private static string GetTypeName(Type type)
        {
            return string.IsNullOrWhiteSpace(type.Namespace)
                ? type.Name
                : $"{type.Namespace}.{type.Name}";
        }

        private static string EscapeCSharpString(string value)
        {
            return value?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? string.Empty;
        }

        private static string UniqueIdentifier(string name, HashSet<string> used)
        {
            var result = name;
            var suffix = 2;

            while (!used.Add(result))
                result = $"{name}_{suffix++}";

            return result;
        }

        private static string ToAbsolutePath(string assetPath)
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        }
    }
}
#endif
