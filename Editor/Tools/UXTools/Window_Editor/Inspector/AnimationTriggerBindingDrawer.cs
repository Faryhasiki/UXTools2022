#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UIAnimation
{
    /// <summary>
    /// AnimationTriggerBinding 的 PropertyDrawer。
    /// 提供触发点下拉选择、目标组件严格校验（仅允许 IAnimationTriggerable）、
    /// 动画名称下拉选择和延迟时间配置。
    /// </summary>
    [CustomPropertyDrawer(typeof(AnimationTriggerBinding))]
    public class AnimationTriggerBindingDrawer : PropertyDrawer
    {
        private const float LINE_HEIGHT = 20f;
        private const float VERTICAL_SPACING = 2f;
        private const float LINES_COUNT = 4f;
        private const float PREVIEW_BUTTON_WIDTH = 52f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return EditorGUIUtility.singleLineHeight;

            return EditorGUIUtility.singleLineHeight
                   + (LINE_HEIGHT + VERTICAL_SPACING) * LINES_COUNT
                   + VERTICAL_SPACING * 2;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var triggerPointProp = property.FindPropertyRelative("_triggerPointName");
            var targetProp = property.FindPropertyRelative("_target");
            var animNameProp = property.FindPropertyRelative("_animationName");
            var delayProp = property.FindPropertyRelative("_delay");

            var headerRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            var foldoutRect = new Rect(headerRect.x, headerRect.y, headerRect.width - PREVIEW_BUTTON_WIDTH - 4f,
                headerRect.height);
            var buttonRect = new Rect(headerRect.xMax - PREVIEW_BUTTON_WIDTH, headerRect.y,
                PREVIEW_BUTTON_WIDTH, headerRect.height);

            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

            using (new EditorGUI.DisabledScope(!CanPreviewBinding(targetProp, animNameProp)))
            {
                if (GUI.Button(buttonRect, "触发"))
                    PreviewBinding(targetProp, animNameProp);
            }

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                float y = position.y + EditorGUIUtility.singleLineHeight + VERTICAL_SPACING;

                DrawTriggerPointField(ref y, position, triggerPointProp, property);
                DrawTargetField(ref y, position, targetProp);
                DrawAnimationNameField(ref y, position, animNameProp, targetProp);
                DrawDelayField(ref y, position, delayProp);

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        #region 触发点下拉

        private void DrawTriggerPointField(ref float y, Rect position, SerializedProperty triggerPointProp,
            SerializedProperty parentProp)
        {
            var rect = GetLineRect(ref y, position);
            var source = parentProp.serializedObject.targetObject as IAnimationTriggerSource;

            if (source != null)
            {
                // triggerKeys = 运行时匹配键。
                // Code-First 模式通常是方法名；Authoring-First 模式通常是稳定 Id。
                var triggerKeys = source.GetTriggerPointNames();
                if (triggerKeys != null && triggerKeys.Length > 0)
                {
                    var displayLabels = new string[triggerKeys.Length];
                    for (var i = 0; i < triggerKeys.Length; i++)
                        displayLabels[i] = GetTriggerPointDisplayName(source, triggerKeys[i]);

                    var currentKey = triggerPointProp.stringValue;
                    int selectedIdx = Array.IndexOf(triggerKeys, currentKey);
                    if (selectedIdx < 0) selectedIdx = 0;

                    // 下拉显示 displayLabels，但存储 triggerKeys（运行时匹配键）
                    int newIdx = EditorGUI.Popup(rect, "触发点", selectedIdx, displayLabels);
                    if (newIdx != selectedIdx || string.IsNullOrEmpty(currentKey))
                        triggerPointProp.stringValue = triggerKeys[newIdx];
                }
                else
                {
                    EditorGUI.LabelField(rect, "触发点", "（未定义触发点）");
                }
            }
            else
            {
                EditorGUI.PropertyField(rect, triggerPointProp, new GUIContent("触发点"));
            }
        }

        #endregion

        #region 目标组件（严格校验 IAnimationTriggerable）

        private void DrawTargetField(ref float y, Rect position, SerializedProperty targetProp)
        {
            var rect = GetLineRect(ref y, position);

            EditorGUI.BeginChangeCheck();
            var newTarget = EditorGUI.ObjectField(rect, "目标组件", targetProp.objectReferenceValue,
                typeof(Component), true);

            if (EditorGUI.EndChangeCheck())
            {
                if (newTarget == null)
                {
                    targetProp.objectReferenceValue = null;
                }
                else
                {
                    var validTarget = FindTriggerableComponent(newTarget);
                    if (validTarget != null)
                    {
                        targetProp.objectReferenceValue = validTarget;
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"[AnimationMiddleware] 目标 \"{newTarget.name}\" 上没有实现 IAnimationTriggerable 的组件，已拒绝赋值。");
                        targetProp.objectReferenceValue = null;
                    }
                }
            }

            var current = targetProp.objectReferenceValue;
            if (current != null && !(current is IAnimationTriggerable))
            {
                var valid = FindTriggerableComponent(current);
                if (valid != null)
                    targetProp.objectReferenceValue = valid;
                else
                    targetProp.objectReferenceValue = null;
            }
        }

        /// <summary>
        /// 从拖入的对象上查找实现了 IAnimationTriggerable 的组件。
        /// 支持拖入 GameObject（自动搜索其上的组件）或直接拖入 Component。
        /// </summary>
        private static Component FindTriggerableComponent(UnityEngine.Object obj)
        {
            if (obj is Component comp)
            {
                if (comp is IAnimationTriggerable)
                    return comp;

                return comp.GetComponents<Component>()
                    .FirstOrDefault(c => c is IAnimationTriggerable);
            }

            if (obj is GameObject go)
            {
                return go.GetComponents<Component>()
                    .FirstOrDefault(c => c is IAnimationTriggerable);
            }

            return null;
        }

        #endregion

        #region 动画名称下拉

        private void DrawAnimationNameField(ref float y, Rect position, SerializedProperty animNameProp,
            SerializedProperty targetProp)
        {
            var rect = GetLineRect(ref y, position);
            var target = targetProp.objectReferenceValue;

            if (target is IAnimationTriggerable triggerable)
            {
                var animNames = triggerable.GetAnimationNames();
                if (animNames != null && animNames.Length > 0)
                {
                    var currentValue = animNameProp.stringValue;
                    int selectedIdx = Array.IndexOf(animNames, currentValue);
                    if (selectedIdx < 0) selectedIdx = 0;

                    int newIdx = EditorGUI.Popup(rect, "动画名称", selectedIdx, animNames);
                    if (newIdx != selectedIdx || string.IsNullOrEmpty(currentValue))
                        animNameProp.stringValue = animNames[newIdx];
                }
                else
                {
                    EditorGUI.LabelField(rect, "动画名称", "（目标未定义动画）");
                }
            }
            else
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUI.LabelField(rect, "动画名称", "（请先指定目标组件）");
                }
            }
        }

        #endregion

        #region 延迟时间

        private void DrawDelayField(ref float y, Rect position, SerializedProperty delayProp)
        {
            var rect = GetLineRect(ref y, position);
            var newValue = EditorGUI.FloatField(rect, "延迟（秒）", delayProp.floatValue);
            delayProp.floatValue = Mathf.Max(0f, newValue);
        }

        #endregion

        #region 工具方法

        private static Rect GetLineRect(ref float y, Rect position)
        {
            var rect = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
            y += LINE_HEIGHT + VERTICAL_SPACING;
            return rect;
        }

        private static string GetTriggerPointDisplayName(IAnimationTriggerSource source, string triggerKey)
        {
            if (source is ITriggerPointLabelProvider labelProvider)
            {
                var label = labelProvider.GetTriggerPointLabel(triggerKey);
                if (!string.IsNullOrWhiteSpace(label))
                    return label;
            }

            return TriggerPointAttribute.GetLabel(source.GetType(), triggerKey);
        }

        private static bool CanPreviewBinding(SerializedProperty targetProp, SerializedProperty animNameProp)
        {
            return targetProp.objectReferenceValue is IAnimationTriggerable
                   && !string.IsNullOrWhiteSpace(animNameProp.stringValue);
        }

        private static void PreviewBinding(SerializedProperty targetProp, SerializedProperty animNameProp)
        {
            if (targetProp.objectReferenceValue is not IAnimationTriggerable triggerable)
                return;

            var animationName = animNameProp.stringValue;
            if (string.IsNullOrWhiteSpace(animationName))
                return;

            triggerable.PlayAnimation(animationName);

            if (targetProp.objectReferenceValue is UnityEngine.Object unityObject)
                EditorUtility.SetDirty(unityObject);

            SceneView.RepaintAll();
            EditorApplication.QueuePlayerLoopUpdate();
        }

        #endregion
    }
}
#endif
