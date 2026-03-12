#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace UIAnimation
{
    /// <summary>
    /// IAnimationTriggerable 组件 Inspector 的静态绘制工具。
    /// 在任意 CustomEditor 的 OnInspectorGUI 末尾调用 DrawAnimationNames，
    /// 即可在 Inspector 底部追加只读的"可触发动画"标签区域。
    ///
    /// 用法：
    ///   public override void OnInspectorGUI()
    ///   {
    ///       base.OnInspectorGUI();
    ///       AnimatableInspectorUtils.DrawAnimationNames(target as IAnimationTriggerable);
    ///   }
    /// </summary>
    public static class AnimatableInspectorUtils
    {
        private static GUIStyle s_tagStyle;
        private static GUIStyle s_sectionStyle;

        private static GUIStyle TagStyle => s_tagStyle ??= new GUIStyle(EditorStyles.miniButton)
        {
            alignment   = TextAnchor.MiddleCenter,
            fixedHeight = 0,
            padding     = new RectOffset(8, 8, 3, 3),
            margin      = new RectOffset(0, 4, 2, 2),
            normal      = { textColor = new Color(0.75f, 0.92f, 1f, 1f) },
        };

        private static GUIStyle SectionLabelStyle => s_sectionStyle ??= new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 11,
        };

        /// <summary>
        /// 在当前 Inspector 位置绘制可触发动画标签区域。
        /// <paramref name="triggerable"/> 为 null 或未定义动画时静默跳过。
        /// </summary>
        public static void DrawAnimationNames(IAnimationTriggerable triggerable)
        {
            if (triggerable == null) return;

            var names = triggerable.GetAnimationNames();

            EditorGUILayout.Space(6);

            var lineRect = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(lineRect, new Color(0.5f, 0.5f, 0.5f, 0.3f));

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("可触发动画", SectionLabelStyle);
            EditorGUILayout.Space(2);

            if (names == null || names.Length == 0)
            {
                EditorGUILayout.LabelField("（未定义动画）", EditorStyles.miniLabel);
                return;
            }

            DrawTags(names);
            EditorGUILayout.Space(2);
        }

        private static void DrawTags(string[] names)
        {
            float maxLineWidth = EditorGUIUtility.currentViewWidth - 32f;
            float lineWidth    = 0f;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(2);

            foreach (var name in names)
            {
                var content  = new GUIContent(name);
                var tagWidth = TagStyle.CalcSize(content).x;

                if (lineWidth > 0 && lineWidth + tagWidth > maxLineWidth)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(2);
                    lineWidth = 0f;
                }

                GUILayout.Label(content, TagStyle, GUILayout.Width(tagWidth));
                lineWidth += tagWidth + TagStyle.margin.right;
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
