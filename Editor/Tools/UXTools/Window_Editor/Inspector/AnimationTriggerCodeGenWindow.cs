#if UNITY_EDITOR
using System;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace UIAnimation
{
    /// <summary>
    /// AnimationTriggerAuthoring 的程序员工具窗口。
    /// 隐藏方法名与代码生成配置，避免打扰动画师的主工作流。
    /// </summary>
    public class AnimationTriggerCodeGenWindow : EditorWindow
    {
        private AnimationTriggerAuthoring _authoring;
        private SerializedObject _serializedObject;
        private SerializedProperty _slotsProp;
        private SerializedProperty _generatedNamespaceProp;
        private SerializedProperty _generatedClassNameProp;
        private SerializedProperty _generatedBaseTypeNameProp;

        public static void Open(AnimationTriggerAuthoring authoring)
        {
            if (authoring == null)
                return;

            var window = CreateInstance<AnimationTriggerCodeGenWindow>();
            window.titleContent = new GUIContent("触发代码生成");
            window.minSize = new Vector2(560f, 460f);
            window.Initialize(authoring);
            window.ShowUtility();
        }

        private void Initialize(AnimationTriggerAuthoring authoring)
        {
            _authoring = authoring;
            _serializedObject = new SerializedObject(authoring);
            _slotsProp = _serializedObject.FindProperty("_triggerSlots");
            _generatedNamespaceProp = _serializedObject.FindProperty("_generatedNamespace");
            _generatedClassNameProp = _serializedObject.FindProperty("_generatedClassName");
            _generatedBaseTypeNameProp = _serializedObject.FindProperty("_generatedBaseTypeName");
        }

        private void OnGUI()
        {
            if (_authoring == null)
            {
                EditorGUILayout.HelpBox("目标 Authoring 组件已失效。", MessageType.Warning);
                return;
            }

            _serializedObject.Update();
            _authoring.EnsureSlotIds();

            EditorGUILayout.HelpBox(
                "这里只给程序员配置生成类名、基类脚本和方法名映射。动画师在主 Inspector 中只维护显示名与绑定即可。",
                MessageType.Info);

            EditorGUILayout.PropertyField(_generatedNamespaceProp, new GUIContent("命名空间"));
            EditorGUILayout.PropertyField(_generatedClassNameProp, new GUIContent("类名"));
            DrawBaseTypeField();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("方法名映射", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            for (var i = 0; i < _slotsProp.arraySize; i++)
            {
                var element = _slotsProp.GetArrayElementAtIndex(i);
                var displayNameProp = element.FindPropertyRelative("_displayName");
                var methodNameProp = element.FindPropertyRelative("_methodName");

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        using (new EditorGUI.DisabledScope(true))
                        {
                            EditorGUILayout.TextField("触发时机", string.IsNullOrWhiteSpace(displayNameProp.stringValue)
                                ? $"触发时机 {i + 1}"
                                : displayNameProp.stringValue);
                        }

                        if (GUILayout.Button("自动命名", GUILayout.Width(72)))
                            methodNameProp.stringValue = SuggestMethodName(displayNameProp.stringValue, i);
                    }

                    EditorGUILayout.PropertyField(methodNameProp, new GUIContent("方法名"));
                }
            }

            EditorGUILayout.Space(8);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("全部自动命名"))
                {
                    for (var i = 0; i < _slotsProp.arraySize; i++)
                    {
                        var element = _slotsProp.GetArrayElementAtIndex(i);
                        var displayNameProp = element.FindPropertyRelative("_displayName");
                        element.FindPropertyRelative("_methodName").stringValue =
                            SuggestMethodName(displayNameProp.stringValue, i);
                    }
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("生成代码", GUILayout.Width(88)))
                    GenerateCode(false);

                if (GUILayout.Button("生成并挂载", GUILayout.Width(104)))
                    GenerateCode(true);
            }

            _serializedObject.ApplyModifiedProperties();
        }

        private void GenerateCode(bool autoAttach)
        {
            _serializedObject.ApplyModifiedProperties();

            var defaultName = string.IsNullOrWhiteSpace(_authoring.GeneratedClassName)
                ? "NewAnimationTrigger.cs"
                : $"{_authoring.GeneratedClassName}.cs";

            var outputPath = EditorUtility.SaveFilePanelInProject(
                "生成触发代码",
                defaultName,
                "cs",
                "请选择生成脚本的保存位置");

            if (string.IsNullOrEmpty(outputPath))
                return;

            var baseType = ResolveType(_generatedBaseTypeNameProp.stringValue);

            if (AnimationTriggerCodeGenerator.Generate(_authoring, baseType, outputPath, out var message))
            {
                if (autoAttach)
                    AnimationTriggerGeneratedScriptAutoAttacher.Register(_authoring, outputPath);

                EditorUtility.DisplayDialog("生成完成", message, "确定");
                Close();
            }
            else
            {
                EditorUtility.DisplayDialog("生成失败", message, "确定");
            }
        }

        private void DrawBaseTypeField()
        {
            var currentBaseType = ResolveType(_generatedBaseTypeNameProp.stringValue);
            var currentScript = FindMonoScript(currentBaseType);

            EditorGUI.BeginChangeCheck();
            var newScript = EditorGUILayout.ObjectField("基类脚本", currentScript, typeof(MonoScript), false) as MonoScript;
            if (EditorGUI.EndChangeCheck())
            {
                var newType = newScript != null ? newScript.GetClass() : typeof(MonoBehaviour);
                _generatedBaseTypeNameProp.stringValue = newType?.FullName ?? typeof(MonoBehaviour).FullName;
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("基类类型", _generatedBaseTypeNameProp.stringValue);
            }
        }

        private static string SuggestMethodName(string displayName, int index)
        {
            var sanitized = Regex.Replace(displayName ?? string.Empty, @"[^\w\u4e00-\u9fff]", "_").Trim('_');
            if (string.IsNullOrWhiteSpace(sanitized))
                sanitized = $"Trigger_{index + 1}";

            if (char.IsDigit(sanitized[0]))
                sanitized = "_" + sanitized;

            return $"{sanitized}Trigger";
        }

        private static Type ResolveType(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return typeof(MonoBehaviour);

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullName);
                if (type != null)
                    return type;
            }

            return typeof(MonoBehaviour);
        }

        private static MonoScript FindMonoScript(Type type)
        {
            if (type == null)
                return null;

            var guids = AssetDatabase.FindAssets($"{type.Name} t:MonoScript");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script != null && script.GetClass() == type)
                    return script;
            }

            return null;
        }
    }
}
#endif
