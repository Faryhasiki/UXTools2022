#if UNITY_EDITOR && TMP_PRESENT
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace UITool
{
    public class TextPresetWindow : DesignLibraryWindowBase
    {
        private TextPresetAsset asset;
        private string filterCat = "全部";
        private TextPresetEntry selected;
        private Vector2 dragStartPos;

        [MenuItem(UIToolConfig.Menu_TextPresets, false, 50)]
        public static void OpenWindow()
        {
            var win = GetWindow<TextPresetWindow>();
            win.titleContent = new GUIContent("文字预设");
            win.minSize = new Vector2(720, 400);
        }

        private void OnEnable()
        {
            asset = LoadOrCreateAsset();
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

            var trackInfo = new Label(asset != null ? $"已追踪 {asset.TrackedCount} 个资产" : "");
            trackInfo.style.color = TextGray;
            trackInfo.style.fontSize = 11;
            trackInfo.style.flexGrow = 1;
            toolbar.Add(trackInfo);

            var rebuildBtn = new Button(() =>
            {
                if (EditorUtility.DisplayDialog("重建索引",
                    "将全量扫描项目中所有预制体和场景，查找引用了当前预设库的资产。\n确定继续？", "确定", "取消"))
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
                if (asset != null && asset.TrackedCount == 0)
                {
                    if (!EditorUtility.DisplayDialog("无追踪资产",
                        "当前注册表为空，建议先执行\"重建索引\"。\n是否仍然只同步当前场景？", "继续", "取消"))
                        return;
                }
                else if (!EditorUtility.DisplayDialog("强制同步全部",
                    $"将对 {asset?.TrackedCount ?? 0} 个已追踪资产中的 UXText 重新应用预设。\n确定继续？", "确定", "取消"))
                    return;

                ForceApplyAllPresets();
                RebuildContent();
            });
            syncBtn.text = "强制同步全部";
            syncBtn.style.height = 22;
            toolbar.Add(syncBtn);

            contentRoot.Add(toolbar);
        }

        #region Asset 管理

        private static TextPresetAsset LoadOrCreateAsset()
        {
            string path = UIToolConfig.TextPresetAssetPath;
            var existing = AssetDatabase.LoadAssetAtPath<TextPresetAsset>(path);
            if (existing != null) return existing;

            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var newAsset = ScriptableObject.CreateInstance<TextPresetAsset>();
            AssetDatabase.CreateAsset(newAsset, path);
            AssetDatabase.SaveAssets();
            return newAsset;
        }

        private void SaveAsset()
        {
            if (asset == null) return;
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
            RefreshSceneUXTexts();
        }

        private void RefreshSceneUXTexts()
        {
            var allTexts = FindObjectsByType<UXText>(FindObjectsSortMode.None);
            foreach (var ux in allTexts)
            {
                if (ux.PresetAsset == asset)
                {
                    ux.ApplyPreset();
                    EditorUtility.SetDirty(ux);
                }
            }
        }

        #endregion

        #region 全局同步

        private void ForceApplyAllPresets()
        {
            if (asset == null) return;

            int prefabCount = 0, sceneCount = 0, textCount = 0;
            var guids = new List<string>(asset.TrackedAssetGUIDs);
            var invalidGuids = new List<string>();

            var openScenePaths = new HashSet<string>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
                openScenePaths.Add(SceneManager.GetSceneAt(i).path);

            // 同步当前已加载的场景
            var loaded = FindObjectsByType<UXText>(FindObjectsSortMode.None);
            foreach (var ux in loaded)
            {
                if (ux.PresetAsset == asset)
                {
                    ux.ApplyPreset();
                    EditorUtility.SetDirty(ux);
                    textCount++;
                }
            }

            for (int i = 0; i < guids.Count; i++)
            {
                string guid = guids[i];
                string path = AssetDatabase.GUIDToAssetPath(guid);

                EditorUtility.DisplayProgressBar("强制同步预设",
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
                    if (n > 0) { prefabCount++; textCount += n; }
                }
                else if (path.EndsWith(".unity") && !openScenePaths.Contains(path))
                {
                    int n = ApplyToScene(path);
                    if (n > 0) { sceneCount++; textCount += n; }
                }
            }

            foreach (var g in invalidGuids)
                asset.UnregisterAssetGUID(g);

            AssetDatabase.SaveAssets();
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("同步完成",
                $"已同步 {textCount} 个 UXText\n预制体: {prefabCount}  场景: {sceneCount}\n已清理 {invalidGuids.Count} 个失效引用", "确定");
        }

        private int ApplyToPrefab(string prefabPath)
        {
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null) return 0;

            var texts = root.GetComponentsInChildren<UXText>(true);
            int count = 0;
            foreach (var ux in texts)
            {
                if (ux.PresetAsset == asset)
                {
                    ux.ApplyPreset();
                    count++;
                }
            }

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
                var texts = go.GetComponentsInChildren<UXText>(true);
                foreach (var ux in texts)
                {
                    if (ux.PresetAsset == asset)
                    {
                        ux.ApplyPreset();
                        EditorUtility.SetDirty(ux);
                        count++;
                    }
                }
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
            if (asset == null) return;

            string assetPath = AssetDatabase.GetAssetPath(asset);
            string assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
            asset.ClearTrackedGUIDs();

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
                        asset.RegisterAssetGUID(guid);
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
                        asset.RegisterAssetGUID(guid);
                        break;
                    }
                }
            }

            AssetDatabase.SaveAssetIfDirty(asset);
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("索引重建完成",
                $"已追踪 {asset.TrackedCount} 个资产", "确定");
            RebuildContent();
        }

        #endregion

        #region Categories

        private void BuildCategories(VisualElement left)
        {
            left.Clear();
            AddCategoryButton(left, "全部", filterCat == "全部", () =>
            {
                filterCat = "全部";
                RebuildContent();
            });

            if (asset != null)
            {
                foreach (var cat in asset.categories)
                {
                    string c = cat;
                    AddCategoryButton(left, c, filterCat == c, () =>
                    {
                        filterCat = c;
                        RebuildContent();
                    }, () => ShowDeleteCategoryMenu(c));
                }
            }

            AddPlusButton(left, () =>
            {
                ShowAddCategoryPopup(name =>
                {
                    asset?.AddCategory(name);
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
                asset?.RemoveCategory(catName);
                SaveAsset();
                if (filterCat == catName) filterCat = "全部";
                RebuildContent();
            });
            menu.ShowAsContext();
        }

        #endregion

        #region List

        private void BuildList(VisualElement center)
        {
            center.Clear();

            var header = ListHeaderRow("字体应用", "字号", "行距");
            var addBtn = header.Q("addBtn");
            addBtn?.RegisterCallback<MouseDownEvent>(e =>
            {
                var item = asset?.AddPreset(filterCat);
                if (item != null) { selected = item; SaveAsset(); }
                RebuildContent();
            });
            center.Add(header);

            var scroll = new ScrollView();
            scroll.style.flexGrow = 1;
            center.Add(scroll);

            if (asset == null) return;
            var items = asset.GetFiltered(filterCat);
            foreach (var item in items)
            {
                var it = item;
                bool isSel = selected != null && selected.id == it.id;
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.height = 28;
                row.style.alignItems = Align.Center;
                row.style.paddingLeft = 12;
                row.style.paddingRight = 12;
                row.style.backgroundColor = isSel ? BgSelected : StyleKeyword.Null;

                row.RegisterCallback<PointerDownEvent>(e =>
                {
                    if (e.button == 0) dragStartPos = e.position;
                });
                row.RegisterCallback<PointerMoveEvent>(e =>
                {
                    if (e.pressedButtons == 1 && Vector2.Distance(dragStartPos, e.position) > 5f)
                    {
                        DragAndDrop.PrepareStartDrag();
                        DragAndDrop.objectReferences = new Object[] { asset };
                        DragAndDrop.SetGenericData("TextPresetId", it.id);
                        DragAndDrop.StartDrag(it.presetName);
                        e.StopPropagation();
                    }
                });
                row.RegisterCallback<MouseDownEvent>(e =>
                {
                    if (e.button == 0) { selected = it; RebuildContent(); }
                    else if (e.button == 1) ShowPresetContextMenu(() =>
                    {
                        asset.RemovePreset(it.id);
                        SaveAsset();
                        if (selected?.id == it.id) selected = null;
                        RebuildContent();
                    });
                });
                row.RegisterCallback<MouseEnterEvent>(e => { if (selected?.id != it.id) row.style.backgroundColor = BgHover; });
                row.RegisterCallback<MouseLeaveEvent>(e => { row.style.backgroundColor = selected?.id == it.id ? BgSelected : StyleKeyword.Null; });

                var ag = new Label("Ag");
                ag.style.width = 30;
                ag.style.unityFontStyleAndWeight = it.GetPreviewFontStyle();
                ag.style.fontSize = 14;
                ag.style.color = TextWhite;
                row.Add(ag);

                var nameLabel = new Label(it.presetName);
                nameLabel.style.flexGrow = 1;
                nameLabel.style.color = TextWhite;
                nameLabel.style.fontSize = 12;
                row.Add(nameLabel);

                var sizeLabel = new Label(it.fontSize.ToString());
                sizeLabel.style.width = 50;
                sizeLabel.style.unityTextAlign = TextAnchor.MiddleRight;
                sizeLabel.style.color = TextGray;
                sizeLabel.style.fontSize = 12;
                row.Add(sizeLabel);

                var lhLabel = new Label(it.GetLineSpacingDisplay());
                lhLabel.style.width = 50;
                lhLabel.style.unityTextAlign = TextAnchor.MiddleRight;
                lhLabel.style.color = TextGray;
                lhLabel.style.fontSize = 12;
                row.Add(lhLabel);

                scroll.Add(row);
            }
        }

        #endregion

        #region Font Style Bar

        private static readonly (string label, FontStyles flag, FontStyle uiStyle)[] StyleToggleDefs =
        {
            ("B",  FontStyles.Bold,          FontStyle.Bold),
            ("I",  FontStyles.Italic,        FontStyle.Italic),
            ("U",  FontStyles.Underline,     FontStyle.Normal),
            ("S",  FontStyles.Strikethrough, FontStyle.Normal),
            ("ab", FontStyles.LowerCase,     FontStyle.Normal),
            ("AB", FontStyles.UpperCase,     FontStyle.Normal),
            ("SC", FontStyles.SmallCaps,     FontStyle.Normal),
        };

        private VisualElement BuildFontStyleBar(TextPresetEntry entry)
        {
            var bar = new VisualElement();
            bar.style.flexDirection = FlexDirection.Row;
            bar.style.height = 24;

            foreach (var def in StyleToggleDefs)
            {
                bool active = (entry.fontStyle & def.flag) != 0;
                var btn = new Label(def.label);
                btn.style.width = 28;
                btn.style.height = 22;
                btn.style.unityTextAlign = TextAnchor.MiddleCenter;
                btn.style.fontSize = 12;
                btn.style.unityFontStyleAndWeight = def.uiStyle;
                btn.style.color = active ? new Color(1f, 1f, 1f) : TextGray;
                btn.style.backgroundColor = active ? new Color(0.35f, 0.55f, 0.85f) : new Color(0.22f, 0.22f, 0.22f);
                btn.style.borderTopLeftRadius = 3;
                btn.style.borderTopRightRadius = 3;
                btn.style.borderBottomLeftRadius = 3;
                btn.style.borderBottomRightRadius = 3;
                btn.style.marginRight = 2;

                var flag = def.flag;
                btn.RegisterCallback<MouseDownEvent>(e =>
                {
                    bool willActivate = (entry.fontStyle & flag) == 0;
                    entry.fontStyle ^= flag;
                    if (willActivate)
                    {
                        if (flag == FontStyles.LowerCase)
                            entry.fontStyle &= ~FontStyles.UpperCase;
                        else if (flag == FontStyles.UpperCase)
                            entry.fontStyle &= ~FontStyles.LowerCase;
                    }
                    SaveAsset();
                    btn.schedule.Execute(RebuildContent);
                });

                bar.Add(btn);
            }

            return bar;
        }

        #endregion

        #region Detail

        private void BuildDetail(VisualElement right)
        {
            right.Clear();
            if (selected == null)
            {
                var hint = new Label("选择一个文字预设以查看详情");
                hint.style.color = TextGray;
                hint.style.unityTextAlign = TextAnchor.MiddleCenter;
                hint.style.flexGrow = 1;
                right.Add(hint);
                return;
            }

            var previewBox = new VisualElement();
            previewBox.style.height = 100;
            previewBox.style.marginBottom = 12;
            previewBox.style.backgroundColor = new Color(0.26f, 0.26f, 0.26f);
            previewBox.style.alignItems = Align.Center;
            previewBox.style.justifyContent = Justify.Center;

            string previewText = "Ag 文字预览 123";
            var fs = selected.fontStyle;
            if ((fs & FontStyles.UpperCase) != 0)
                previewText = previewText.ToUpper();
            else if ((fs & FontStyles.LowerCase) != 0)
                previewText = previewText.ToLower();

            var previewWrapper = new VisualElement();
            previewWrapper.style.alignItems = Align.Center;

            var previewLabel = new Label(previewText);
            int clampedSize = Mathf.Clamp(selected.fontSize, 14, 48);
            previewLabel.style.fontSize = (fs & FontStyles.SmallCaps) != 0
                ? Mathf.Max(10, clampedSize * 0.8f)
                : clampedSize;
            previewLabel.style.unityFontStyleAndWeight = selected.GetPreviewFontStyle();
            previewLabel.style.color = TextWhite;
            previewLabel.style.letterSpacing = selected.characterSpacing;
            if (selected.fontAsset != null && selected.fontAsset.sourceFontFile != null)
                previewLabel.style.unityFont = new StyleFont(selected.fontAsset.sourceFontFile);
            previewWrapper.Add(previewLabel);

            if ((fs & FontStyles.Underline) != 0)
            {
                var underline = new VisualElement();
                underline.style.height = 1;
                underline.style.backgroundColor = TextWhite;
                underline.style.alignSelf = Align.Stretch;
                underline.style.marginTop = -2;
                underline.style.marginLeft = 4;
                underline.style.marginRight = 4;
                previewWrapper.Add(underline);
            }

            if ((fs & FontStyles.Strikethrough) != 0)
            {
                var strike = new VisualElement();
                strike.style.height = 1;
                strike.style.backgroundColor = TextWhite;
                strike.style.position = Position.Absolute;
                strike.style.left = 4;
                strike.style.right = 4;
                strike.style.top = new StyleLength(new Length(50, LengthUnit.Percent));
                previewWrapper.Add(strike);
            }

            previewBox.Add(previewWrapper);
            right.Add(previewBox);

            right.Add(DetailLabel("名称"));
            right.Add(DetailTextField(selected.presetName, v =>
            {
                selected.presetName = v;
                SaveAsset();
                RebuildContent();
            }));

            right.Add(DetailLabel("描述"));
            right.Add(DetailTextField(selected.description, v =>
            {
                selected.description = v;
                SaveAsset();
            }));

            var attrLabel = new Label("属性");
            attrLabel.style.color = TextWhite;
            attrLabel.style.fontSize = 13;
            attrLabel.style.unityFontStyleAndWeight = UnityEngine.FontStyle.Bold;
            attrLabel.style.marginTop = 12;
            right.Add(attrLabel);

            right.Add(DetailLabel("字体资产"));
            var fontField = new ObjectField();
            fontField.objectType = typeof(TMP_FontAsset);
            fontField.value = selected.fontAsset;
            fontField.style.marginTop = 2;
            fontField.style.height = 22;
            fontField.RegisterValueChangedCallback(e =>
            {
                selected.fontAsset = e.newValue as TMP_FontAsset;
                SaveAsset();
                fontField.schedule.Execute(RebuildContent);
            });
            right.Add(fontField);

            var styleAndSizeRow = new VisualElement();
            styleAndSizeRow.style.flexDirection = FlexDirection.Row;
            styleAndSizeRow.style.marginTop = 6;

            var styleContainer = new VisualElement();
            styleContainer.style.flexGrow = 1;
            var styleLabel = new Label("字体样式");
            styleLabel.style.fontSize = 11;
            styleLabel.style.color = TextGray;
            styleContainer.Add(styleLabel);
            var styleBar = BuildFontStyleBar(selected);
            styleContainer.Add(styleBar);
            styleAndSizeRow.Add(styleContainer);

            var sizeContainer = new VisualElement();
            sizeContainer.style.width = 70;
            sizeContainer.style.marginLeft = 4;
            var sizeLabel = new Label("字号");
            sizeLabel.style.fontSize = 11;
            sizeLabel.style.color = TextGray;
            sizeContainer.Add(sizeLabel);
            var sizeField = new IntegerField();
            sizeField.value = selected.fontSize;
            sizeField.style.height = 22;
            sizeField.RegisterValueChangedCallback(e =>
            {
                selected.fontSize = Mathf.Max(1, e.newValue);
            });
            sizeField.RegisterCallback<FocusOutEvent>(e =>
            {
                SaveAsset();
                sizeField.schedule.Execute(RebuildContent);
            });
            sizeContainer.Add(sizeField);
            styleAndSizeRow.Add(sizeContainer);
            right.Add(styleAndSizeRow);

            var spacingRow = new VisualElement();
            spacingRow.style.flexDirection = FlexDirection.Row;
            spacingRow.style.marginTop = 6;

            var lsContainer = new VisualElement();
            lsContainer.style.flexGrow = 1;
            var lsLabel = new Label("行间距");
            lsLabel.style.fontSize = 11;
            lsLabel.style.color = TextGray;
            lsContainer.Add(lsLabel);
            var lsField = new FloatField();
            lsField.value = selected.lineSpacing;
            lsField.style.height = 22;
            lsField.RegisterValueChangedCallback(e =>
            {
                selected.lineSpacing = e.newValue;
            });
            lsField.RegisterCallback<FocusOutEvent>(e =>
            {
                SaveAsset();
                lsField.schedule.Execute(RebuildContent);
            });
            lsContainer.Add(lsField);
            spacingRow.Add(lsContainer);

            var csContainer = new VisualElement();
            csContainer.style.flexGrow = 1;
            csContainer.style.marginLeft = 4;
            var csLabel = new Label("字符间距");
            csLabel.style.fontSize = 11;
            csLabel.style.color = TextGray;
            csContainer.Add(csLabel);
            var csField = new FloatField();
            csField.value = selected.characterSpacing;
            csField.style.height = 22;
            csField.RegisterValueChangedCallback(e =>
            {
                selected.characterSpacing = e.newValue;
            });
            csField.RegisterCallback<FocusOutEvent>(e =>
            {
                SaveAsset();
                csField.schedule.Execute(RebuildContent);
            });
            csContainer.Add(csField);
            spacingRow.Add(csContainer);

            right.Add(spacingRow);
        }

        #endregion
    }
}
#endif
