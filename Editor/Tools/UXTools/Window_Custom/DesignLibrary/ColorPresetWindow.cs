#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace UITool
{
    /// <summary>
    /// 颜色预设编辑窗口，三栏布局（分类 / 列表 / 详情）。
    /// 数据源为 ColorPresetAsset ScriptableObject，支持拖拽到 Hierarchy、工具栏追踪与同步。
    /// </summary>
    public class ColorPresetWindow : DesignLibraryWindowBase
    {
        [System.NonSerialized] private ColorPresetAsset _asset;
        [System.NonSerialized] private string _filterCat = "全部";
        [System.NonSerialized] private ColorPresetEntry _selected;
        [System.NonSerialized] private Vector2 _dragStartPos;

        [MenuItem(UIToolConfig.Menu_ColorPresets, false, 51)]
        public static void OpenWindow()
        {
            var win = GetWindow<ColorPresetWindow>();
            win.titleContent = new GUIContent("颜色预设");
            win.minSize = new Vector2(720, 400);
        }

        private void OnEnable()
        {
            _asset = LoadOrCreateAsset();
            InitRootUI();
        }

        protected override void BuildContent()
        {
            BuildToolbar();

            var row = ThreeColumnLayout();
            contentRoot.Add(row);

            BuildCategories(row.Q("left"));
            BuildList(row.Q("center"));
            BuildDetail(row.Q("right"));
        }

        #region Asset 管理

        private static ColorPresetAsset LoadOrCreateAsset()
        {
            string path = UIToolConfig.ColorPresetAssetPath;
            var existing = AssetDatabase.LoadAssetAtPath<ColorPresetAsset>(path);
            if (existing != null) return existing;

            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var newAsset = ScriptableObject.CreateInstance<ColorPresetAsset>();

            string legacyJson = UIToolConfig.ColorPresetLegacyJsonPath;
            if (File.Exists(legacyJson))
            {
                string json = File.ReadAllText(legacyJson);
                JsonUtility.FromJsonOverwrite(json, newAsset);
                Debug.Log($"[UXTools] 颜色预设窗口：已从旧 JSON 迁移 {newAsset.presets.Count} 条数据。");
            }

            AssetDatabase.CreateAsset(newAsset, path);
            AssetDatabase.SaveAssets();
            return newAsset;
        }

        private void SaveAsset()
        {
            if (_asset == null) return;
            EditorUtility.SetDirty(_asset);
            AssetDatabase.SaveAssetIfDirty(_asset);
            RefreshSceneColorTargets();
        }

        private void RefreshSceneColorTargets()
        {
            var allImages = Object.FindObjectsByType<UXImage>(FindObjectsSortMode.None);
            foreach (var ux in allImages)
            {
                if (ux.ColorPresetAsset == _asset)
                {
                    ux.ApplyColorPreset();
                    EditorUtility.SetDirty(ux);
                }
            }

#if TMP_PRESENT
            var allTexts = Object.FindObjectsByType<UXText>(FindObjectsSortMode.None);
            foreach (var ux in allTexts)
            {
                if (ux.ColorPresetAsset == _asset)
                {
                    ux.ApplyColorPreset();
                    EditorUtility.SetDirty(ux);
                }
            }
#endif
        }

        #endregion

        #region Toolbar

        private void BuildToolbar()
        {
            var toolbar = new VisualElement();
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.justifyContent = Justify.FlexEnd;
            toolbar.style.alignItems = Align.Center;
            toolbar.style.paddingLeft = 12;
            toolbar.style.paddingRight = 12;
            toolbar.style.height = 30;
            toolbar.style.borderBottomWidth = 1;
            toolbar.style.borderBottomColor = new Color(0.15f, 0.15f, 0.15f);

            var trackInfo = new Label(_asset != null ? $"已追踪 {_asset.TrackedCount} 个资产" : "");
            trackInfo.style.color = TextGray;
            trackInfo.style.fontSize = 11;
            trackInfo.style.flexGrow = 1;
            toolbar.Add(trackInfo);

            var codeGenBtn = new Button(() => PresetCodeGenerator.GenerateColorDef(_asset));
            codeGenBtn.text = "生成代码";
            codeGenBtn.style.height = 22;
            codeGenBtn.style.marginRight = 4;
            toolbar.Add(codeGenBtn);

            var rebuildBtn = new Button(() =>
            {
                if (EditorUtility.DisplayDialog("重建索引",
                    "将全量扫描项目中所有预制体和场景，查找引用了当前颜色预设库的资产。\n确定继续？", "确定", "取消"))
                {
                    RebuildTrackedIndex();
                }
            });
            rebuildBtn.text = "重建索引";
            rebuildBtn.style.height = 22;
            rebuildBtn.style.marginRight = 4;
            toolbar.Add(rebuildBtn);

            var syncBtn = new Button(() =>
            {
                if (_asset != null && _asset.TrackedCount == 0)
                {
                    if (!EditorUtility.DisplayDialog("无追踪资产",
                        "当前注册表为空，建议先执行\"重建索引\"。\n是否仍然只同步当前场景？", "继续", "取消"))
                        return;
                }
                else if (!EditorUtility.DisplayDialog("强制同步全部",
                    $"将对 {_asset?.TrackedCount ?? 0} 个已追踪资产中的颜色预设目标重新应用预设。\n确定继续？", "确定", "取消"))
                    return;

                ForceApplyAllPresets();
                RebuildContent();
            });
            syncBtn.text = "强制同步全部";
            syncBtn.style.height = 22;
            toolbar.Add(syncBtn);

            contentRoot.Add(toolbar);
        }

        #endregion

        #region 全局同步

        private void ForceApplyAllPresets()
        {
            if (_asset == null) return;

            int prefabCount = 0, sceneCount = 0, targetCount = 0;
            var guids = new List<string>(_asset.TrackedAssetGUIDs);
            var invalidGuids = new List<string>();

            var openScenePaths = new HashSet<string>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
                openScenePaths.Add(SceneManager.GetSceneAt(i).path);

            targetCount += ApplyToLoadedScene();

            for (int i = 0; i < guids.Count; i++)
            {
                string guid = guids[i];
                string path = AssetDatabase.GUIDToAssetPath(guid);

                EditorUtility.DisplayProgressBar("强制同步颜色预设",
                    $"处理 {i + 1}/{guids.Count}: {Path.GetFileName(path)}",
                    (float)(i + 1) / guids.Count);

                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                {
                    invalidGuids.Add(guid);
                    continue;
                }

                if (path.EndsWith(".prefab"))
                {
                    int n = ApplyToPrefab(path);
                    if (n > 0) { prefabCount++; targetCount += n; }
                }
                else if (path.EndsWith(".unity") && !openScenePaths.Contains(path))
                {
                    int n = ApplyToScene(path);
                    if (n > 0) { sceneCount++; targetCount += n; }
                }
            }

            foreach (var g in invalidGuids)
                _asset.UnregisterAssetGUID(g);

            AssetDatabase.SaveAssets();
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("同步完成",
                $"已同步 {targetCount} 个颜色目标\n预制体: {prefabCount}  场景: {sceneCount}\n已清理 {invalidGuids.Count} 个失效引用", "确定");
        }

        private int ApplyToLoadedScene()
        {
            int count = 0;
            var images = Object.FindObjectsByType<UXImage>(FindObjectsSortMode.None);
            foreach (var ux in images)
            {
                if (ux.ColorPresetAsset == _asset)
                {
                    ux.ApplyColorPreset();
                    EditorUtility.SetDirty(ux);
                    count++;
                }
            }

#if TMP_PRESENT
            var texts = Object.FindObjectsByType<UXText>(FindObjectsSortMode.None);
            foreach (var ux in texts)
            {
                if (ux.ColorPresetAsset == _asset)
                {
                    ux.ApplyColorPreset();
                    EditorUtility.SetDirty(ux);
                    count++;
                }
            }
#endif
            return count;
        }

        private int ApplyToPrefab(string prefabPath)
        {
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null) return 0;

            int count = 0;
            var images = root.GetComponentsInChildren<UXImage>(true);
            foreach (var ux in images)
            {
                if (ux.ColorPresetAsset == _asset)
                {
                    ux.ApplyColorPreset();
                    count++;
                }
            }

#if TMP_PRESENT
            var texts = root.GetComponentsInChildren<UXText>(true);
            foreach (var ux in texts)
            {
                if (ux.ColorPresetAsset == _asset)
                {
                    ux.ApplyColorPreset();
                    count++;
                }
            }
#endif

            if (count > 0)
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            return count;
        }

        private int ApplyToScene(string scenePath)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            var roots = scene.GetRootGameObjects();
            int count = 0;

            foreach (var go in roots)
            {
                var images = go.GetComponentsInChildren<UXImage>(true);
                foreach (var ux in images)
                {
                    if (ux.ColorPresetAsset == _asset)
                    {
                        ux.ApplyColorPreset();
                        EditorUtility.SetDirty(ux);
                        count++;
                    }
                }

#if TMP_PRESENT
                var texts = go.GetComponentsInChildren<UXText>(true);
                foreach (var ux in texts)
                {
                    if (ux.ColorPresetAsset == _asset)
                    {
                        ux.ApplyColorPreset();
                        EditorUtility.SetDirty(ux);
                        count++;
                    }
                }
#endif
            }

            if (count > 0)
                EditorSceneManager.SaveScene(scene);
            EditorSceneManager.CloseScene(scene, true);
            return count;
        }

        #endregion

        #region 重建索引

        private void RebuildTrackedIndex()
        {
            if (_asset == null) return;

            string assetPath = AssetDatabase.GetAssetPath(_asset);
            string assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
            _asset.ClearTrackedGUIDs();

            var prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            var sceneGuids = AssetDatabase.FindAssets("t:Scene");
            int total = prefabGuids.Length + sceneGuids.Length;
            int current = 0;

            foreach (var guid in prefabGuids)
            {
                current++;
                if (current % 50 == 0)
                    EditorUtility.DisplayProgressBar("重建索引",
                        $"扫描预制体 {current}/{total}", (float)current / total);

                string path = AssetDatabase.GUIDToAssetPath(guid);
                var deps = AssetDatabase.GetDependencies(path, false);
                foreach (var dep in deps)
                {
                    if (AssetDatabase.AssetPathToGUID(dep) == assetGuid)
                    {
                        _asset.RegisterAssetGUID(guid);
                        break;
                    }
                }
            }

            foreach (var guid in sceneGuids)
            {
                current++;
                if (current % 50 == 0)
                    EditorUtility.DisplayProgressBar("重建索引",
                        $"扫描场景 {current}/{total}", (float)current / total);

                string path = AssetDatabase.GUIDToAssetPath(guid);
                var deps = AssetDatabase.GetDependencies(path, false);
                foreach (var dep in deps)
                {
                    if (AssetDatabase.AssetPathToGUID(dep) == assetGuid)
                    {
                        _asset.RegisterAssetGUID(guid);
                        break;
                    }
                }
            }

            AssetDatabase.SaveAssetIfDirty(_asset);
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("索引重建完成",
                $"已追踪 {_asset.TrackedCount} 个资产", "确定");
            RebuildContent();
        }

        #endregion

        #region Categories

        private void BuildCategories(VisualElement left)
        {
            left.Clear();
            AddCategoryButton(left, "全部", _filterCat == "全部", () =>
            {
                _filterCat = "全部";
                RebuildContent();
            });

            if (_asset != null)
            {
                foreach (var cat in _asset.categories)
                {
                    string c = cat;
                    AddCategoryButton(left, c, _filterCat == c, () =>
                    {
                        _filterCat = c;
                        RebuildContent();
                    }, () => ShowDeleteCategoryMenu(c));
                }
            }

            AddPlusButton(left, () =>
            {
                ShowAddCategoryPopup(name =>
                {
                    _asset?.AddCategory(name);
                    SaveAsset();
                    RebuildContent();
                });
            });
        }

        private void ShowDeleteCategoryMenu(string catName)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("删除分类 \"" + catName + "\""), false, () =>
            {
                _asset?.RemoveCategory(catName);
                SaveAsset();
                if (_filterCat == catName) _filterCat = "全部";
                RebuildContent();
            });
            menu.ShowAsContext();
        }

        #endregion

        #region List

        private void BuildList(VisualElement center)
        {
            center.Clear();

            var header = ListHeaderRow("应用场景", "色号");
            var addBtn = header.Q("addBtn");
            addBtn?.RegisterCallback<MouseDownEvent>(e =>
            {
                var item = _asset?.AddPreset(_filterCat);
                if (item != null) { _selected = item; SaveAsset(); }
                RebuildContent();
            });
            center.Add(header);

            var scroll = new ScrollView();
            scroll.style.flexGrow = 1;
            center.Add(scroll);

            if (_asset == null) return;
            var items = _asset.GetFiltered(_filterCat);
            foreach (var item in items)
            {
                var it = item;
                bool isSel = _selected != null && _selected.id == it.id;
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.height = 28;
                row.style.alignItems = Align.Center;
                row.style.paddingLeft = 12;
                row.style.paddingRight = 12;
                row.style.backgroundColor = isSel ? BgSelected : StyleKeyword.Null;

                row.RegisterCallback<PointerDownEvent>(e =>
                {
                    if (e.button == 0) _dragStartPos = e.position;
                });
                row.RegisterCallback<PointerMoveEvent>(e =>
                {
                    if (e.pressedButtons == 1 && Vector2.Distance(_dragStartPos, e.position) > 5f)
                    {
                        DragAndDrop.PrepareStartDrag();
                        DragAndDrop.objectReferences = new Object[] { _asset };
                        DragAndDrop.SetGenericData("ColorPresetId", it.id);
                        DragAndDrop.StartDrag(it.presetName);
                        e.StopPropagation();
                    }
                });
                row.RegisterCallback<MouseDownEvent>(e =>
                {
                    if (e.button == 0) { _selected = it; RebuildContent(); }
                    else if (e.button == 1) ShowPresetContextMenu(it.id, () =>
                    {
                        _asset.RemovePreset(it.id);
                        SaveAsset();
                        if (_selected?.id == it.id) _selected = null;
                        RebuildContent();
                    });
                });
                row.RegisterCallback<MouseEnterEvent>(e => { if (_selected?.id != it.id) row.style.backgroundColor = BgHover; });
                row.RegisterCallback<MouseLeaveEvent>(e => { row.style.backgroundColor = _selected?.id == it.id ? BgSelected : StyleKeyword.Null; });

                var dot = new VisualElement();
                dot.style.width = 14;
                dot.style.height = 14;
                dot.style.borderTopLeftRadius = dot.style.borderTopRightRadius = dot.style.borderBottomLeftRadius = dot.style.borderBottomRightRadius = 7;
                dot.style.backgroundColor = it.GetColor();
                dot.style.marginRight = 8;
                row.Add(dot);

                var nameLabel = new Label(it.presetName);
                nameLabel.style.flexGrow = 1;
                nameLabel.style.color = TextWhite;
                nameLabel.style.fontSize = 12;
                row.Add(nameLabel);

                var hexLabel = new Label(it.hex);
                hexLabel.style.width = 80;
                hexLabel.style.unityTextAlign = TextAnchor.MiddleRight;
                hexLabel.style.color = TextGray;
                hexLabel.style.fontSize = 12;
                row.Add(hexLabel);

                scroll.Add(row);
            }
        }

        #endregion

        #region Detail

        private void BuildDetail(VisualElement right)
        {
            right.Clear();
            if (_selected == null)
            {
                var hint = new Label("选择一个颜色预设以查看详情");
                hint.style.color = TextGray;
                hint.style.unityTextAlign = TextAnchor.MiddleCenter;
                hint.style.flexGrow = 1;
                right.Add(hint);
                return;
            }

            var preview = new VisualElement();
            preview.style.height = 120;
            preview.style.marginBottom = 12;
            preview.style.borderTopLeftRadius = preview.style.borderTopRightRadius = preview.style.borderBottomLeftRadius = preview.style.borderBottomRightRadius = 4;
            preview.style.backgroundColor = _selected.GetColor();
            right.Add(preview);

            right.Add(DetailLabel("名称"));
            var nameRow = new VisualElement();
            nameRow.style.flexDirection = FlexDirection.Row;
            nameRow.style.marginTop = 2;

            var nameField = new TextField();
            nameField.value = _selected.presetName ?? "";
            nameField.style.flexGrow = 1;
            nameField.style.height = 22;
            nameField.SetEnabled(false);
            nameRow.Add(nameField);

            var nameEditBtn = new Button();
            nameEditBtn.text = "编辑";
            nameEditBtn.style.width = 42;
            nameEditBtn.clicked += () =>
            {
                bool editing = !nameField.enabledSelf;
                nameField.SetEnabled(editing);
                nameEditBtn.text = editing ? "确定" : "编辑";
                if (!editing && nameField.value != _selected.presetName)
                {
                    _selected.presetName = nameField.value;
                    SaveAsset();
                    RebuildContent();
                }
            };
            nameRow.Add(nameEditBtn);
            right.Add(nameRow);

            right.Add(DetailLabel("描述"));
            var descField = DetailTextField(_selected.description, v =>
            {
                _selected.description = v;
                SaveAsset();
            });
            right.Add(descField);

            var attrHeader = new VisualElement();
            attrHeader.style.flexDirection = FlexDirection.Row;
            attrHeader.style.justifyContent = Justify.SpaceBetween;
            attrHeader.style.marginTop = 12;
            var attrLabel = new Label("属性");
            attrLabel.style.color = TextWhite;
            attrLabel.style.fontSize = 13;
            attrLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            attrHeader.Add(attrLabel);
            right.Add(attrHeader);

            var colorRow = new VisualElement();
            colorRow.style.flexDirection = FlexDirection.Row;
            colorRow.style.alignItems = Align.Center;
            colorRow.style.marginTop = 6;

            var colorField = new ColorField();
            colorField.value = _selected.GetColor();
            colorField.showAlpha = true;
            colorField.style.flexGrow = 1;
            colorField.style.height = 24;
            colorField.RegisterValueChangedCallback(e =>
            {
                _selected.SetColor(e.newValue);
                SaveAsset();
                RebuildContent();
            });
            colorRow.Add(colorField);

            var opLabel = new Label(_selected.GetOpacityDisplay());
            opLabel.style.width = 50;
            opLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            opLabel.style.color = TextGray;
            opLabel.style.marginLeft = 8;
            colorRow.Add(opLabel);

            right.Add(colorRow);

            var codeToggle = new UnityEngine.UIElements.Toggle("生成代码");
            codeToggle.value = _selected.generateCode;
            codeToggle.style.marginTop = 12;
            codeToggle.RegisterValueChangedCallback(e =>
            {
                _selected.generateCode = e.newValue;
                SaveAsset();
            });
            right.Add(codeToggle);
        }

        #endregion
    }
}
#endif
