#if UNITY_EDITOR
using UITool;
using UnityEditor;
using UnityEngine;

namespace UITool
{
    /// <summary>
    /// UXColorBinding 自定义 Inspector，提供颜色预设 Popup 选择和拖拽接收
    /// </summary>
    [CustomEditor(typeof(UXColorBinding), true)]
    [CanEditMultipleObjects]
    public class UXColorBindingEditor : Editor
    {
        #region 序列化属性

        private SerializedProperty _colorPresetAssetProp;
        private SerializedProperty _colorPresetIdProp;
        private SerializedProperty _applyOnAwakeProp;

        private string[] _colorPresetNames;
        private string[] _colorPresetIds;
        private bool _showColorPresetDetails;

        #endregion

        private void OnEnable()
        {
            _colorPresetAssetProp = serializedObject.FindProperty("_colorPresetAsset");
            _colorPresetIdProp = serializedObject.FindProperty("_colorPresetId");
            _applyOnAwakeProp = serializedObject.FindProperty("_applyOnAwake");
            RefreshColorPresetList();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawColorPresetSection();
            serializedObject.ApplyModifiedProperties();
            HandleInspectorDrop();
        }

        #region 颜色预设区域

        private void DrawColorPresetSection()
        {
            var binding = target as UXColorBinding;
            if (binding != null && binding.HasConflict())
            {
                EditorGUILayout.HelpBox(
                    "当前 Graphic 组件（UXText / UXImage）已自带颜色预设功能，UXColorBinding 的设置将被忽略。\n" +
                    "请使用组件自身的颜色预设，或移除此 UXColorBinding。",
                    MessageType.Warning);
                EditorGUILayout.Space(4);
            }

            EditorGUILayout.LabelField("颜色预设", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(_colorPresetAssetProp, new GUIContent("颜色库"));
                if (GUILayout.Button("刷新", GUILayout.Width(42)))
                    RefreshColorPresetList();
            }

            var colorAsset = _colorPresetAssetProp.objectReferenceValue as ColorPresetAsset;
            if (colorAsset == null)
            {
                AutoFindColorPresetAsset();
                colorAsset = _colorPresetAssetProp.objectReferenceValue as ColorPresetAsset;
            }

            if (colorAsset == null || _colorPresetNames == null || _colorPresetNames.Length == 0)
            {
                EditorGUILayout.HelpBox("未找到颜色预设库。请先在设计库中创建颜色预设。", MessageType.Info);
                return;
            }

            var displayNames = new string[_colorPresetNames.Length + 1];
            var displayIds = new string[_colorPresetIds.Length + 1];
            displayNames[0] = "None";
            displayIds[0] = "";
            System.Array.Copy(_colorPresetNames, 0, displayNames, 1, _colorPresetNames.Length);
            System.Array.Copy(_colorPresetIds, 0, displayIds, 1, _colorPresetIds.Length);

            string currentId = _colorPresetIdProp.stringValue;
            int selectedIdx = string.IsNullOrEmpty(currentId) ? 0 : System.Array.IndexOf(displayIds, currentId);
            if (selectedIdx < 0) selectedIdx = 0;

            int newIdx = EditorGUILayout.Popup("颜色类型", selectedIdx, displayNames);
            if (newIdx != selectedIdx)
            {
                _colorPresetIdProp.stringValue = displayIds[newIdx];
                serializedObject.ApplyModifiedProperties();
                foreach (var t in targets)
                {
                    var cb = t as UXColorBinding;
                    if (cb != null)
                        cb.ApplyColorPreset();
                }
            }

            var colorEntry = colorAsset.FindById(_colorPresetIdProp.stringValue);
            if (colorEntry != null)
            {
                _showColorPresetDetails = EditorGUILayout.Foldout(_showColorPresetDetails, "颜色参数", true);
                if (_showColorPresetDetails)
                {
                    EditorGUI.indentLevel++;
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.ColorField("颜色", colorEntry.GetColor());
                        EditorGUILayout.TextField("色号", colorEntry.hex);
                        EditorGUILayout.IntField("不透明度", colorEntry.opacity);
                    }
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.PropertyField(_applyOnAwakeProp, new GUIContent("启动时应用颜色"));

            if (colorEntry != null && GUILayout.Button("应用颜色预设"))
            {
                foreach (var t in targets)
                {
                    var cb = t as UXColorBinding;
                    if (cb != null)
                    {
                        Undo.RecordObject(cb, "Apply Color Preset");
                        cb.ApplyColorPreset();
                        EditorUtility.SetDirty(cb);
                    }
                }
            }
        }

        #endregion

        #region 拖拽处理

        private void HandleInspectorDrop()
        {
            var evt = Event.current;
            if (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform)
                return;
            if (!ColorPresetDragHandler.HasPresetDragData())
                return;

            if (evt.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                evt.Use();
            }
            else if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                if (ColorPresetDragHandler.TryGetPresetDragData(out var colorAsset, out var colorId))
                {
                    foreach (var t in targets)
                    {
                        var binding = t as UXColorBinding;
                        if (binding != null)
                            ColorPresetDragHandler.ApplyColorPresetTo(binding, colorAsset, colorId);
                    }
                    RefreshColorPresetList();
                }
                evt.Use();
            }
        }

        #endregion

        #region 刷新与自动查找

        private void RefreshColorPresetList()
        {
            var colorAsset = _colorPresetAssetProp.objectReferenceValue as ColorPresetAsset;
            if (colorAsset == null)
            {
                AutoFindColorPresetAsset();
                colorAsset = _colorPresetAssetProp.objectReferenceValue as ColorPresetAsset;
            }

            if (colorAsset != null && colorAsset.presets.Count > 0)
            {
                _colorPresetNames = colorAsset.GetPresetNames();
                _colorPresetIds = colorAsset.GetPresetIds();
            }
            else
            {
                _colorPresetNames = new string[0];
                _colorPresetIds = new string[0];
            }
        }

        private void AutoFindColorPresetAsset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<ColorPresetAsset>(UIToolConfig.ColorPresetAssetPath);
            if (asset != null)
            {
                _colorPresetAssetProp.objectReferenceValue = asset;
                serializedObject.ApplyModifiedProperties();
                RefreshColorPresetList();
            }
        }

        #endregion
    }
}
#endif
