#if UNITY_EDITOR && TMP_PRESENT
using System.Linq;
using TMPro;
using TMPro.EditorUtilities;
using UITool;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UITool
{
    [CustomEditor(typeof(UXText), true)]
    [CanEditMultipleObjects]
    public class UXTextEditor : TMP_EditorPanelUI
    {
        private SerializedProperty presetAssetProp;
        private SerializedProperty presetIdProp;
        private SerializedProperty applyOnAwakeProp;

        private string[] presetNames;
        private string[] presetIds;
        private bool showPresetDetails = false;

        protected override void OnEnable()
        {
            base.OnEnable();
            presetAssetProp = serializedObject.FindProperty("presetAsset");
            presetIdProp = serializedObject.FindProperty("presetId");
            applyOnAwakeProp = serializedObject.FindProperty("applyOnAwake");
            RefreshPresetList();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPresetSection();
            EditorGUILayout.Space(6);
            serializedObject.ApplyModifiedProperties();

            base.OnInspectorGUI();

            HandleInspectorDrop();
        }

        private void DrawPresetSection()
        {
            EditorGUILayout.LabelField("文字预设", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(presetAssetProp, new GUIContent("预设库"));
                if (GUILayout.Button("刷新", GUILayout.Width(42)))
                    RefreshPresetList();
            }

            var asset = presetAssetProp.objectReferenceValue as TextPresetAsset;
            if (asset == null)
            {
                AutoFindPresetAsset();
                asset = presetAssetProp.objectReferenceValue as TextPresetAsset;
            }

            if (asset == null || presetNames == null || presetNames.Length == 0)
            {
                EditorGUILayout.HelpBox("未找到预设库资产。请先在设计库中创建文字预设并确保已同步。", MessageType.Info);
                return;
            }

            var displayNames = new string[presetNames.Length + 1];
            var displayIds = new string[presetIds.Length + 1];
            displayNames[0] = "None";
            displayIds[0] = "";
            System.Array.Copy(presetNames, 0, displayNames, 1, presetNames.Length);
            System.Array.Copy(presetIds, 0, displayIds, 1, presetIds.Length);

            string currentId = presetIdProp.stringValue;
            int selectedIdx = string.IsNullOrEmpty(currentId) ? 0 : System.Array.IndexOf(displayIds, currentId);
            if (selectedIdx < 0) selectedIdx = 0;

            int newIdx = EditorGUILayout.Popup("预设类型", selectedIdx, displayNames);
            if (newIdx != selectedIdx)
            {
                presetIdProp.stringValue = displayIds[newIdx];
                serializedObject.ApplyModifiedProperties();
                foreach (var t in targets)
                {
                    var uxText = t as UXText;
                    if (uxText != null)
                    {
                        uxText.ApplyPreset();
                        if (newIdx > 0)
                            TryRegisterAssetGUID(asset, uxText);
                    }
                }
            }

            var entry = asset.FindById(presetIdProp.stringValue);
            if (entry != null)
            {
                showPresetDetails = EditorGUILayout.Foldout(showPresetDetails, "预设参数", true);
                if (showPresetDetails)
                {
                    EditorGUI.indentLevel++;
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.ObjectField("字体资产", entry.fontAsset, typeof(TMP_FontAsset), false);
                        EditorGUILayout.EnumFlagsField("字体样式", entry.fontStyle);
                        EditorGUILayout.IntField("字号", entry.fontSize);
                        EditorGUILayout.FloatField("行间距", entry.lineSpacing);
                        EditorGUILayout.FloatField("字符间距", entry.characterSpacing);
                    }
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.PropertyField(applyOnAwakeProp, new GUIContent("启动时应用预设"));

            if (entry != null && GUILayout.Button("应用预设"))
            {
                foreach (var t in targets)
                {
                    var uxText = t as UXText;
                    if (uxText != null)
                    {
                        Undo.RecordObject(uxText, "Apply Text Preset");
                        uxText.ApplyPreset();
                        EditorUtility.SetDirty(uxText);
                    }
                }
            }
        }

        private void HandleInspectorDrop()
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
                if (TextPresetDragHandler.TryGetPresetDragData(out var dragAsset, out var presetId))
                {
                    foreach (var t in targets)
                    {
                        var uxText = t as UXText;
                        if (uxText != null)
                            TextPresetDragHandler.ApplyPresetTo(uxText, dragAsset, presetId);
                    }
                    RefreshPresetList();
                }
                evt.Use();
            }
        }

        private void RefreshPresetList()
        {
            var asset = presetAssetProp.objectReferenceValue as TextPresetAsset;
            if (asset == null)
            {
                AutoFindPresetAsset();
                asset = presetAssetProp.objectReferenceValue as TextPresetAsset;
            }

            if (asset != null && asset.presets.Count > 0)
            {
                presetNames = asset.GetPresetNames();
                presetIds = asset.GetPresetIds();
            }
            else
            {
                presetNames = new string[0];
                presetIds = new string[0];
            }
        }

        private void AutoFindPresetAsset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextPresetAsset>(UIToolConfig.TextPresetAssetPath);
            if (asset != null)
            {
                presetAssetProp.objectReferenceValue = asset;
                serializedObject.ApplyModifiedProperties();
                RefreshPresetList();
            }
        }

        #region Hierarchy 右键菜单创建 UXText

        [MenuItem("GameObject/UI (Canvas)/UXUI/UXText", false, 2000)]
        private static void CreateUXText(MenuCommand menuCommand)
        {
            var go = new GameObject("UXText");
            go.layer = LayerMask.NameToLayer("UI");

            var uxText = go.AddComponent<UXText>();
            uxText.text = "New UXText";
            uxText.fontSize = TMP_Settings.defaultFontSize;
            uxText.color = Color.white;
            uxText.alignment = TextAlignmentOptions.Center;
            uxText.raycastTarget = false;

            if (TMP_Settings.autoSizeTextContainer)
            {
                var size = uxText.GetPreferredValues(TMP_Math.FLOAT_MAX, TMP_Math.FLOAT_MAX);
                uxText.rectTransform.sizeDelta = size;
            }
            else
            {
                uxText.rectTransform.sizeDelta = TMP_Settings.defaultTextMeshProUITextContainerSize;
            }

            var presetAsset = AssetDatabase.LoadAssetAtPath<TextPresetAsset>(UIToolConfig.TextPresetAssetPath);
            if (presetAsset != null)
                uxText.PresetAsset = presetAsset;

            PlaceUIElement(go, menuCommand);
        }

        private static void PlaceUIElement(GameObject element, MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            bool explicitParent = parent != null;

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

        private static void TryRegisterAssetGUID(TextPresetAsset asset, UXText uxText)
        {
            if (asset == null || uxText == null) return;

            string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(uxText);
            if (string.IsNullOrEmpty(assetPath) && uxText.gameObject.scene.IsValid())
                assetPath = uxText.gameObject.scene.path;
            if (string.IsNullOrEmpty(assetPath)) return;

            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            asset.RegisterAssetGUID(guid);
        }

        #endregion
    }
}
#endif
