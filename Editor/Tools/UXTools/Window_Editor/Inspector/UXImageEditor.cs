#if UNITY_EDITOR
using UITool;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UITool
{
    /// <summary>
    /// UXImage 自定义 Inspector，在 Image 默认面板之上增加颜色预设选择功能
    /// </summary>
    [CustomEditor(typeof(UXImage), true)]
    [CanEditMultipleObjects]
    public class UXImageEditor : ImageEditor
    {
        #region 颜色预设序列化属性

        private SerializedProperty _colorPresetAssetProp;
        private SerializedProperty _colorPresetIdProp;
        private SerializedProperty _applyOnAwakeProp;

        private string[] _colorPresetNames;
        private string[] _colorPresetIds;
        private bool _showColorPresetDetails;

        #endregion

        protected override void OnEnable()
        {
            base.OnEnable();

            _colorPresetAssetProp = serializedObject.FindProperty("_colorPresetAsset");
            _colorPresetIdProp = serializedObject.FindProperty("_colorPresetId");
            _applyOnAwakeProp = serializedObject.FindProperty("_applyOnAwake");
            RefreshColorPresetList();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawColorPresetSection();
            EditorGUILayout.Space(6);
            serializedObject.ApplyModifiedProperties();

            base.OnInspectorGUI();

            HandleInspectorDrop();
        }

        #region 颜色预设区域

        private void DrawColorPresetSection()
        {
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
                    var uxImage = t as UXImage;
                    if (uxImage != null)
                    {
                        uxImage.ApplyColorPreset();
                        if (newIdx > 0)
                            TryRegisterColorAssetGUID(colorAsset, uxImage);
                    }
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
                    var uxImage = t as UXImage;
                    if (uxImage != null)
                    {
                        Undo.RecordObject(uxImage, "Apply Color Preset");
                        uxImage.ApplyColorPreset();
                        EditorUtility.SetDirty(uxImage);
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
                        var uxImage = t as UXImage;
                        if (uxImage != null)
                            ColorPresetDragHandler.ApplyColorPresetTo(uxImage, colorAsset, colorId);
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

        #region Hierarchy 右键菜单创建 UXImage

        [MenuItem("GameObject/UI (Canvas)/UXUI/UXImage", false, 2001)]
        private static void CreateUXImage(MenuCommand menuCommand)
        {
            var go = new GameObject("UXImage");
            go.layer = LayerMask.NameToLayer("UI");

            var uxImage = go.AddComponent<UXImage>();
            uxImage.color = Color.white;
            uxImage.raycastTarget = true;
            uxImage.rectTransform.sizeDelta = new Vector2(100, 100);

            var colorAsset = AssetDatabase.LoadAssetAtPath<ColorPresetAsset>(UIToolConfig.ColorPresetAssetPath);
            if (colorAsset != null)
                uxImage.ColorPresetAsset = colorAsset;

            PlaceUIElement(go, menuCommand);
        }

        private static void PlaceUIElement(GameObject element, MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;

            if (parent == null)
            {
                parent = GetOrCreateCanvas();
                var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
                if (prefabStage != null && !prefabStage.IsPartOfPrefabContents(parent))
                    parent = prefabStage.prefabContentsRoot;
            }

            if (parent.GetComponentsInParent<Canvas>(true).Length == 0)
            {
                var canvas = CreateCanvas();
                Undo.SetTransformParent(canvas.transform, parent.transform, "");
                parent = canvas;
            }

            GameObjectUtility.EnsureUniqueNameForSibling(element);
            Undo.SetTransformParent(element.transform, parent.transform, "");

            var rt = element.transform as RectTransform;
            if (rt != null)
            {
                rt.anchoredPosition = Vector2.zero;
                var lp = rt.localPosition;
                lp.z = 0;
                rt.localPosition = lp;
            }
            element.transform.localRotation = Quaternion.identity;
            element.transform.localScale = Vector3.one;
            SetLayerRecursive(element, parent.layer);

            Undo.RegisterFullObjectHierarchyUndo(parent, "");
            Undo.SetCurrentGroupName("Create " + element.name);
            Selection.activeGameObject = element;
        }

        private static GameObject GetOrCreateCanvas()
        {
            var selected = Selection.activeGameObject;
            if (selected != null)
            {
                var c = selected.GetComponentInParent<Canvas>();
                if (IsValidCanvas(c))
                    return c.gameObject;
            }

            var canvases = StageUtility.GetCurrentStageHandle().FindComponentsOfType<Canvas>();
            foreach (var c in canvases)
                if (IsValidCanvas(c))
                    return c.gameObject;

            return CreateCanvas();
        }

        private static bool IsValidCanvas(Canvas canvas)
        {
            if (canvas == null || !canvas.gameObject.activeInHierarchy)
                return false;
            if (EditorUtility.IsPersistent(canvas) || (canvas.hideFlags & HideFlags.HideInHierarchy) != 0)
                return false;
            return StageUtility.GetStageHandle(canvas.gameObject) == StageUtility.GetCurrentStageHandle();
        }

        private static GameObject CreateCanvas()
        {
            var root = new GameObject("Canvas");
            root.layer = LayerMask.NameToLayer("UI");
            root.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            root.AddComponent<CanvasScaler>();
            root.AddComponent<GraphicRaycaster>();

            StageUtility.PlaceGameObjectInCurrentStage(root);
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
                root.transform.SetParent(prefabStage.prefabContentsRoot.transform, false);

            Undo.RegisterCreatedObjectUndo(root, "Create Canvas");

            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<StandaloneInputModule>();
                StageUtility.PlaceGameObjectInCurrentStage(esGo);
                Undo.RegisterCreatedObjectUndo(esGo, "Create EventSystem");
            }

            return root;
        }

        private static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
                SetLayerRecursive(child.gameObject, layer);
        }

        #endregion

        #region 资产追踪

        private static void TryRegisterColorAssetGUID(ColorPresetAsset asset, Component comp)
        {
            if (asset == null || comp == null) return;

            string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(comp);
            if (string.IsNullOrEmpty(assetPath) && comp.gameObject.scene.IsValid())
                assetPath = comp.gameObject.scene.path;
            if (string.IsNullOrEmpty(assetPath)) return;

            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            asset.RegisterAssetGUID(guid);
        }

        #endregion
    }

    /// <summary>
    /// 拦截普通 Image 的 Inspector，增加转换为 UXImage 的按钮和颜色预设拖拽接收
    /// </summary>
    [CustomEditor(typeof(Image), true)]
    [CanEditMultipleObjects]
    public class ImagePresetDropEditor : ImageEditor
    {
        public override void OnInspectorGUI()
        {
            if (!(target is UXImage) && DrawConvertButton())
                return;

            base.OnInspectorGUI();

            if (!(target is UXImage))
                HandlePresetDrop();
        }

        private bool DrawConvertButton()
        {
            if (GUILayout.Button("点击转换为 UXImage", GUILayout.Height(24)))
            {
                foreach (var t in targets)
                {
                    var img = t as Image;
                    if (img != null && !(img is UXImage))
                        ColorPresetDragHandler.ReplaceWithUXImage(img.gameObject, null, null);
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
                if (ColorPresetDragHandler.TryGetPresetDragData(out var asset, out var colorId))
                {
                    foreach (var t in targets)
                    {
                        var img = t as Image;
                        if (img != null)
                            ColorPresetDragHandler.ReplaceWithUXImage(img.gameObject, asset, colorId);
                    }
                }
                evt.Use();
            }
        }
    }
}
#endif
