#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UITool
{
    /// <summary>
    /// 颜色预设拖拽处理器。
    /// 负责 Hierarchy 窗口的拖放检测，支持将颜色预设拖拽到：
    /// - IColorPresetTarget 组件（UXText、UXImage、UXColorBinding）直接绑定
    /// - 普通 Image 自动转换为 UXImage 后绑定
    /// </summary>
    [InitializeOnLoad]
    static class ColorPresetDragHandler
    {
        /// <summary>
        /// DragAndDrop 数据键
        /// </summary>
        private const string DRAG_DATA_KEY = "ColorPresetId";

        static ColorPresetDragHandler()
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyItemGUI;
        }

        #region Drag Data Helpers

        /// <summary>
        /// 判断当前拖拽中是否包含颜色预设数据
        /// </summary>
        public static bool HasPresetDragData()
        {
            return DragAndDrop.GetGenericData(DRAG_DATA_KEY) is string id && !string.IsNullOrEmpty(id);
        }

        /// <summary>
        /// 尝试从当前拖拽中提取颜色预设数据
        /// </summary>
        public static bool TryGetPresetDragData(out ColorPresetAsset asset, out string presetId)
        {
            asset = null;
            presetId = null;
            if (DragAndDrop.GetGenericData(DRAG_DATA_KEY) is string id && !string.IsNullOrEmpty(id))
            {
                presetId = id;
                var refs = DragAndDrop.objectReferences;
                if (refs != null && refs.Length > 0)
                    asset = refs[0] as ColorPresetAsset;
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

            var colorTarget = go.GetComponent<IColorPresetTarget>();
            var plainImage = colorTarget == null ? go.GetComponent<Image>() : null;

            if (colorTarget == null && plainImage == null) return;

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
                    if (colorTarget != null)
                        ApplyColorPresetTo(colorTarget, asset, presetId);
                    else
                        ReplaceWithUXImage(go, asset, presetId);
                }
                evt.Use();
            }
        }

        #endregion

        #region Apply / Replace

        /// <summary>
        /// 将颜色预设应用到 IColorPresetTarget 组件
        /// </summary>
        public static void ApplyColorPresetTo(IColorPresetTarget target, ColorPresetAsset asset, string presetId)
        {
            var comp = target as Component;
            if (comp != null)
                Undo.RecordObject(comp, "Apply Color Preset via Drag");

            target.ColorPresetAsset = asset;
            target.ColorPresetId = presetId;

            if (comp != null)
                EditorUtility.SetDirty(comp);
        }

        /// <summary>
        /// 将普通 Image 替换为 UXImage 并绑定颜色预设
        /// </summary>
        public static void ReplaceWithUXImage(GameObject go, ColorPresetAsset asset, string presetId)
        {
            var img = go.GetComponent<Image>();
            if (img == null || img is UXImage) return;

            Undo.SetCurrentGroupName("Convert Image to UXImage");
            int group = Undo.GetCurrentGroup();

            string json = EditorJsonUtility.ToJson(img);
            Undo.DestroyObjectImmediate(img);

            var uxImage = Undo.AddComponent<UXImage>(go);
            EditorJsonUtility.FromJsonOverwrite(json, uxImage);

            if (asset != null)
            {
                uxImage.ColorPresetAsset = asset;
                uxImage.ColorPresetId = presetId;
            }
            EditorUtility.SetDirty(uxImage);

            Undo.CollapseUndoOperations(group);
        }

        #endregion
    }
}
#endif
