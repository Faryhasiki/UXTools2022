#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace UIAnimation
{
    /// <summary>
    /// AnimationTriggerAuthoring Inspector。
    /// 主面板面向动画师，只展示触发时机显示名与绑定配置。
    /// 程序员相关的代码生成与方法名配置放到单独窗口中。
    /// </summary>
    [CustomEditor(typeof(AnimationTriggerAuthoring), true)]
    public class AnimationTriggerAuthoringEditor : Editor
    {
        private const float REORDER_HANDLE_WIDTH = 18f;

        private SerializedProperty _triggerSlotsProp;
        private SerializedProperty _triggerBindingsProp;

        private ReorderableList _slotsList;
        private ReorderableList _bindingsList;

        private void OnEnable()
        {
            _triggerSlotsProp = serializedObject.FindProperty("_triggerSlots");
            _triggerBindingsProp = serializedObject.FindProperty("_triggerBindings");

            BuildSlotsList();
            BuildBindingsList();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var authoring = target as AnimationTriggerAuthoring;
            authoring?.EnsureSlotIds();

            DrawToolbar(authoring);
            EditorGUILayout.Space(4);

            EditorGUILayout.HelpBox(
                "先由动画师配置触发时机与动画绑定。程序员需要生成正式代码时，点击右上角“程序员工具”。\n触发时机仅维护显示名；方法名在程序员工具里单独配置。",
                MessageType.Info);

            _slotsList.DoLayoutList();
            EditorGUILayout.Space(4);
            _bindingsList.DoLayoutList();

            DrawValidation(authoring);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawToolbar(AnimationTriggerAuthoring authoring)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("UI Animation Trigger Authoring", EditorStyles.boldLabel);

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("程序员工具", GUILayout.Width(88)))
                {
                    serializedObject.ApplyModifiedProperties();
                    AnimationTriggerCodeGenWindow.Open(authoring);
                }
            }
        }

        private void BuildSlotsList()
        {
            _slotsList = new ReorderableList(serializedObject, _triggerSlotsProp, true, true, true, true);
            _slotsList.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, "触发时机");
            };

            _slotsList.elementHeight = EditorGUIUtility.singleLineHeight + 6f;

            _slotsList.drawElementCallback = (rect, index, active, focused) =>
            {
                var element = _triggerSlotsProp.GetArrayElementAtIndex(index);
                var displayNameProp = element.FindPropertyRelative("_displayName");
                var idProp = element.FindPropertyRelative("_id");

                rect.y += 2f;
                rect.xMin += REORDER_HANDLE_WIDTH;
                rect.xMax -= 2f;

                var labelRect = new Rect(rect.x, rect.y, rect.width - 64f, EditorGUIUtility.singleLineHeight);
                var previewRect = new Rect(rect.xMax - 58f, rect.y, 58f, EditorGUIUtility.singleLineHeight);

                EditorGUI.PropertyField(labelRect, displayNameProp, GUIContent.none);

                using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(idProp.stringValue)))
                {
                    if (GUI.Button(previewRect, "触发"))
                    {
                        serializedObject.ApplyModifiedProperties();
                        (target as AnimationTriggerAuthoring)?.FireById(idProp.stringValue);
                    }
                }
            };

            _slotsList.onAddCallback = list =>
            {
                var index = _triggerSlotsProp.arraySize;
                _triggerSlotsProp.InsertArrayElementAtIndex(index);
                var element = _triggerSlotsProp.GetArrayElementAtIndex(index);
                element.FindPropertyRelative("_id").stringValue = System.Guid.NewGuid().ToString("N");
                element.FindPropertyRelative("_displayName").stringValue = $"新触发时机 {index + 1}";
                element.FindPropertyRelative("_methodName").stringValue = string.Empty;
                serializedObject.ApplyModifiedProperties();
            };
        }

        private void BuildBindingsList()
        {
            _bindingsList = new ReorderableList(serializedObject, _triggerBindingsProp, true, true, true, true);
            _bindingsList.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, "触发绑定");
            };

            _bindingsList.elementHeightCallback = index =>
            {
                var element = _triggerBindingsProp.GetArrayElementAtIndex(index);
                return EditorGUI.GetPropertyHeight(element, true) + 4f;
            };

            _bindingsList.drawElementCallback = (rect, index, active, focused) =>
            {
                var element = _triggerBindingsProp.GetArrayElementAtIndex(index);
                rect.y += 2f;
                rect.xMin += REORDER_HANDLE_WIDTH;
                rect.xMax -= 2f;
                rect.height = EditorGUI.GetPropertyHeight(element, true);
                EditorGUI.PropertyField(rect, element, new GUIContent(BuildBindingSummary(element)), true);
            };
        }

        private void DrawValidation(AnimationTriggerAuthoring authoring)
        {
            if (authoring == null)
                return;

            var issues = AnimationTriggerValidationUtility.Validate(authoring);
            if (issues.Count == 0)
                return;

            var sb = new StringBuilder();
            foreach (var issue in issues)
                sb.AppendLine(issue);

            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(sb.ToString().TrimEnd(), MessageType.Warning);
        }

        private string BuildBindingSummary(SerializedProperty bindingProp)
        {
            var triggerPoint = bindingProp.FindPropertyRelative("_triggerPointName").stringValue;
            var target = bindingProp.FindPropertyRelative("_target").objectReferenceValue as Component;
            var animationName = bindingProp.FindPropertyRelative("_animationName").stringValue;
            var delay = bindingProp.FindPropertyRelative("_delay").floatValue;

            var triggerLabel = GetTriggerLabel(triggerPoint);
            var targetName = target != null ? target.name : "未指定目标";
            var animLabel = string.IsNullOrWhiteSpace(animationName) ? "未指定动画" : animationName;

            return $"{triggerLabel} -> {targetName} -> {animLabel} ({delay:0.##}s)";
        }

        private string GetTriggerLabel(string triggerId)
        {
            var authoring = target as AnimationTriggerAuthoring;
            var slot = authoring?.FindSlotById(triggerId);
            if (slot == null || string.IsNullOrWhiteSpace(slot.DisplayName))
                return string.IsNullOrWhiteSpace(triggerId) ? "未指定触发时机" : triggerId;

            return slot.DisplayName;
        }
    }
}
#endif
