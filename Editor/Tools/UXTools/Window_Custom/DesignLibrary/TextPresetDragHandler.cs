#if UNITY_EDITOR && TMP_PRESENT
using TMPro;
using TMPro.EditorUtilities;
using UnityEditor;
using UnityEngine;

namespace UITool
{
    [InitializeOnLoad]
    static class TextPresetDragHandler
    {
        private const string DragDataKey = "TextPresetId";

        static TextPresetDragHandler()
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyItemGUI;
        }

        #region Drag Data Helpers

        public static bool HasPresetDragData()
        {
            return DragAndDrop.GetGenericData(DragDataKey) is string id && !string.IsNullOrEmpty(id);
        }

        public static bool TryGetPresetDragData(out TextPresetAsset asset, out string presetId)
        {
            asset = null;
            presetId = null;
            if (DragAndDrop.GetGenericData(DragDataKey) is string id && !string.IsNullOrEmpty(id))
            {
                presetId = id;
                var refs = DragAndDrop.objectReferences;
                if (refs != null && refs.Length > 0)
                    asset = refs[0] as TextPresetAsset;
            }
            return asset != null && !string.IsNullOrEmpty(presetId);
        }

        #endregion

        #region Hierarchy Drop

        private static void OnHierarchyItemGUI(int instanceID, Rect selectionRect)
        {
            var evt = Event.current;
            if (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform)
                return;
            if (!selectionRect.Contains(evt.mousePosition))
                return;
            if (!HasPresetDragData())
                return;

            var go = EditorUtility.EntityIdToObject(instanceID) as GameObject;
            if (go == null) return;

            var uxText = go.GetComponent<UXText>();
            var tmpText = uxText != null ? null : go.GetComponent<TextMeshProUGUI>();

            if (uxText == null && tmpText == null) return;

            if (evt.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                evt.Use();
            }
            else if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                if (TryGetPresetDragData(out var asset, out var presetId))
                {
                    if (uxText != null)
                        ApplyPresetTo(uxText, asset, presetId);
                    else
                        ReplaceWithUXText(go, asset, presetId);
                }
                evt.Use();
            }
        }

        #endregion

        #region Apply / Replace

        public static void ApplyPresetTo(UXText uxText, TextPresetAsset asset, string presetId)
        {
            Undo.RecordObject(uxText, "Apply Text Preset via Drag");
            uxText.PresetAsset = asset;
            uxText.PresetId = presetId;
            EditorUtility.SetDirty(uxText);
        }

        public static void ReplaceWithUXText(GameObject go, TextPresetAsset asset, string presetId)
        {
            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp == null) return;

            Undo.SetCurrentGroupName("Convert TMP to UXText");
            int group = Undo.GetCurrentGroup();

            string json = EditorJsonUtility.ToJson(tmp);
            Undo.DestroyObjectImmediate(tmp);

            var uxText = Undo.AddComponent<UXText>(go);
            EditorJsonUtility.FromJsonOverwrite(json, uxText);

            if (asset != null)
            {
                uxText.PresetAsset = asset;
                uxText.PresetId = presetId;
            }
            EditorUtility.SetDirty(uxText);

            Undo.CollapseUndoOperations(group);
        }

        #endregion
    }

    [CustomEditor(typeof(TextMeshProUGUI), true)]
    [CanEditMultipleObjects]
    class TMPTextPresetDropEditor : TMP_EditorPanelUI
    {
        public override void OnInspectorGUI()
        {
            if (!(target is UXText) && DrawConvertButton())
                return;

            base.OnInspectorGUI();

            if (!(target is UXText))
                HandlePresetDrop();
        }

        private bool DrawConvertButton()
        {
            if (GUILayout.Button("点击转换为 UXText", GUILayout.Height(24)))
            {
                foreach (var t in targets)
                {
                    var tmp = t as TextMeshProUGUI;
                    if (tmp != null && !(tmp is UXText))
                        TextPresetDragHandler.ReplaceWithUXText(tmp.gameObject, null, null);
                }
                return true;
            }
            EditorGUILayout.Space(2);
            return false;
        }

        private void HandlePresetDrop()
        {
            var evt = Event.current;
            if (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform)
                return;
            if (!TextPresetDragHandler.HasPresetDragData())
                return;

            if (evt.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                evt.Use();
            }
            else if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                if (TextPresetDragHandler.TryGetPresetDragData(out var asset, out var presetId))
                {
                    foreach (var t in targets)
                    {
                        var tmp = t as TextMeshProUGUI;
                        if (tmp != null)
                            TextPresetDragHandler.ReplaceWithUXText(tmp.gameObject, asset, presetId);
                    }
                }
                evt.Use();
            }
        }
    }
}
#endif
