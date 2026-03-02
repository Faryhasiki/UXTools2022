#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UITool
{
    public class DesignLibraryWindow : EditorWindow
    {
        private static DesignLibraryWindow s_window;

        private int activeTab = 0;
        private readonly string[] tabLabels = { "\u2003组件库", "\u2003文字", "\u2003颜色" };

        private TextPresetLibrary textLib;
        private ColorPresetLibrary colorLib;

        private string textFilterCat = "全部";
        private string colorFilterCat = "全部";
        private TextPresetItem selectedText;
        private ColorPresetItem selectedColor;

        private VisualElement contentRoot;

        static readonly Color BgDark = new Color(0.19f, 0.19f, 0.19f);
        static readonly Color BgPanel = new Color(0.22f, 0.22f, 0.22f);
        static readonly Color BgSelected = new Color(0.17f, 0.36f, 0.53f);
        static readonly Color BgHover = new Color(0.3f, 0.3f, 0.3f);
        static readonly Color BgTabActive = new Color(0.25f, 0.25f, 0.25f);
        static readonly Color BgTabInactive = new Color(0.19f, 0.19f, 0.19f);
        static readonly Color BorderCol = new Color(0.14f, 0.14f, 0.14f);
        static readonly Color TextWhite = new Color(0.85f, 0.85f, 0.85f);
        static readonly Color TextGray = new Color(0.55f, 0.55f, 0.55f);

        [MenuItem(UIToolConfig.Menu_DesignLibrary, false, 50)]
        public static void OpenWindow()
        {
            s_window = GetWindow<DesignLibraryWindow>();
            s_window.titleContent = new GUIContent("设计库");
            s_window.minSize = new Vector2(960, 520);
        }

        private void OnEnable()
        {
            textLib = JsonAssetManager.GetAssets<TextPresetLibrary>();
            colorLib = JsonAssetManager.GetAssets<ColorPresetLibrary>();
            BuildUI();
        }

        // ─────────────────────── UI Framework ───────────────────────

        private void BuildUI()
        {
            var root = rootVisualElement;
            root.Clear();
            root.style.flexDirection = FlexDirection.Column;
            root.style.backgroundColor = BgDark;

            BuildTabBar(root);

            contentRoot = new VisualElement();
            contentRoot.style.flexGrow = 1;
            root.Add(contentRoot);

            RebuildContent();
        }

        private void BuildTabBar(VisualElement parent)
        {
            var bar = new VisualElement();
            bar.style.flexDirection = FlexDirection.Row;
            bar.style.height = 32;
            bar.style.backgroundColor = BgDark;
            bar.style.borderBottomWidth = 1;
            bar.style.borderBottomColor = BorderCol;
            parent.Add(bar);

            for (int i = 0; i < tabLabels.Length; i++)
            {
                int idx = i;
                var tab = new Label(tabLabels[i]);
                tab.style.unityTextAlign = TextAnchor.MiddleCenter;
                tab.style.paddingLeft = 14;
                tab.style.paddingRight = 14;
                tab.style.fontSize = 13;
                tab.style.color = TextWhite;
                tab.style.backgroundColor = idx == activeTab ? BgTabActive : BgTabInactive;
                tab.style.borderRightWidth = 1;
                tab.style.borderRightColor = BorderCol;
                tab.style.cursor = new UnityEngine.UIElements.Cursor();
                tab.RegisterCallback<MouseDownEvent>(e => SwitchTab(idx));
                tab.RegisterCallback<MouseEnterEvent>(e =>
                {
                    if (idx != activeTab) tab.style.backgroundColor = BgHover;
                });
                tab.RegisterCallback<MouseLeaveEvent>(e =>
                {
                    tab.style.backgroundColor = idx == activeTab ? BgTabActive : BgTabInactive;
                });
                bar.Add(tab);
            }
        }

        private void SwitchTab(int tabIndex)
        {
            if (tabIndex == activeTab) return;
            activeTab = tabIndex;
            BuildUI();
        }

        // ─────────────────────── Widget (placeholder) ───────────────────────

        private void BuildWidgetPanel()
        {
            var wrapper = new VisualElement();
            wrapper.style.flexGrow = 1;
            wrapper.style.alignItems = Align.Center;
            wrapper.style.justifyContent = Justify.Center;
            var btn = new Button(() => EditorApplication.ExecuteMenuItem(UIToolConfig.Menu_WidgetLibrary));
            btn.text = "打开组件库窗口";
            btn.style.fontSize = 14;
            btn.style.width = 200;
            btn.style.height = 36;
            wrapper.Add(btn);
            contentRoot.Add(wrapper);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━ COLOR PANEL ━━━━━━━━━━━━━━━━━━━━━━━

        private void BuildColorPanel()
        {
            var row = ThreeColumnLayout();
            contentRoot.Add(row);

            BuildColorCategories(row.Q("left"));
            BuildColorList(row.Q("center"));
            BuildColorDetail(row.Q("right"));
        }

        private void BuildColorCategories(VisualElement left)
        {
            left.Clear();
            AddCategoryButton(left, "全部", colorFilterCat == "全部", () =>
            {
                colorFilterCat = "全部";
                RebuildContent();
            });

            if (colorLib != null)
            {
                foreach (var cat in colorLib.categories)
                {
                    string c = cat;
                    AddCategoryButton(left, c, colorFilterCat == c, () =>
                    {
                        colorFilterCat = c;
                        RebuildContent();
                    }, () => ShowCategoryContextMenu(c, false));
                }
            }

            AddPlusButton(left, () =>
            {
                ShowAddCategoryPopup(name =>
                {
                    colorLib?.AddCategory(name);
                    RebuildContent();
                });
            });
        }

        private void BuildColorList(VisualElement center)
        {
            center.Clear();

            var header = ListHeaderRow("应用场景", "色号");
            var addBtn = header.Q("addBtn");
            addBtn?.RegisterCallback<MouseDownEvent>(e =>
            {
                var item = colorLib?.AddPreset(colorFilterCat);
                if (item != null) selectedColor = item;
                RebuildContent();
            });
            center.Add(header);

            var scroll = new ScrollView();
            scroll.style.flexGrow = 1;
            center.Add(scroll);

            if (colorLib == null) return;
            var items = colorLib.GetFiltered(colorFilterCat);
            foreach (var item in items)
            {
                var it = item;
                bool selected = selectedColor != null && selectedColor.id == it.id;
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.height = 28;
                row.style.alignItems = Align.Center;
                row.style.paddingLeft = 12;
                row.style.paddingRight = 12;
                row.style.backgroundColor = selected ? BgSelected : StyleKeyword.Null;

                row.RegisterCallback<MouseDownEvent>(e =>
                {
                    if (e.button == 0) { selectedColor = it; RebuildContent(); }
                    else if (e.button == 1) ShowPresetContextMenu(() => { colorLib.RemovePreset(it.id); if (selectedColor?.id == it.id) selectedColor = null; RebuildContent(); });
                });
                row.RegisterCallback<MouseEnterEvent>(e => { if (selectedColor?.id != it.id) row.style.backgroundColor = BgHover; });
                row.RegisterCallback<MouseLeaveEvent>(e => { row.style.backgroundColor = selectedColor?.id == it.id ? BgSelected : StyleKeyword.Null; });

                var dot = new VisualElement();
                dot.style.width = 14;
                dot.style.height = 14;
                dot.style.borderTopLeftRadius = dot.style.borderTopRightRadius = dot.style.borderBottomLeftRadius = dot.style.borderBottomRightRadius = 7;
                dot.style.backgroundColor = it.GetColor();
                dot.style.marginRight = 8;
                row.Add(dot);

                var nameLabel = new Label(it.name);
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

        private void BuildColorDetail(VisualElement right)
        {
            right.Clear();
            if (selectedColor == null)
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
            preview.style.backgroundColor = selectedColor.GetColor();
            right.Add(preview);

            right.Add(DetailLabel("名称"));
            var nameField = DetailTextField(selectedColor.name, v =>
            {
                selectedColor.name = v;
                colorLib.Save();
                RebuildContent();
            });
            right.Add(nameField);

            right.Add(DetailLabel("描述"));
            var descField = DetailTextField(selectedColor.description, v =>
            {
                selectedColor.description = v;
                colorLib.Save();
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
            colorField.value = selectedColor.GetColor();
            colorField.showAlpha = true;
            colorField.style.flexGrow = 1;
            colorField.style.height = 24;
            colorField.RegisterValueChangedCallback(e =>
            {
                selectedColor.SetColor(e.newValue);
                colorLib.Save();
                RebuildContent();
            });
            colorRow.Add(colorField);

            var opLabel = new Label(selectedColor.opacity + " %");
            opLabel.style.width = 50;
            opLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            opLabel.style.color = TextGray;
            opLabel.style.marginLeft = 8;
            colorRow.Add(opLabel);

            right.Add(colorRow);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━ TEXT PANEL ━━━━━━━━━━━━━━━━━━━━━━━

        private void BuildTextPanel()
        {
            var row = ThreeColumnLayout();
            contentRoot.Add(row);

            BuildTextCategories(row.Q("left"));
            BuildTextList(row.Q("center"));
            BuildTextDetail(row.Q("right"));
        }

        private void BuildTextCategories(VisualElement left)
        {
            left.Clear();
            AddCategoryButton(left, "全部", textFilterCat == "全部", () =>
            {
                textFilterCat = "全部";
                RebuildContent();
            });

            if (textLib != null)
            {
                foreach (var cat in textLib.categories)
                {
                    string c = cat;
                    AddCategoryButton(left, c, textFilterCat == c, () =>
                    {
                        textFilterCat = c;
                        RebuildContent();
                    }, () => ShowCategoryContextMenu(c, true));
                }
            }

            AddPlusButton(left, () =>
            {
                ShowAddCategoryPopup(name =>
                {
                    textLib?.AddCategory(name);
                    RebuildContent();
                });
            });
        }

        private void BuildTextList(VisualElement center)
        {
            center.Clear();

            var header = ListHeaderRow("字体应用", "字号", "行距");
            var addBtn = header.Q("addBtn");
            addBtn?.RegisterCallback<MouseDownEvent>(e =>
            {
                var item = textLib?.AddPreset(textFilterCat);
                if (item != null) selectedText = item;
                RebuildContent();
            });
            center.Add(header);

            var scroll = new ScrollView();
            scroll.style.flexGrow = 1;
            center.Add(scroll);

            if (textLib == null) return;
            var items = textLib.GetFiltered(textFilterCat);
            foreach (var item in items)
            {
                var it = item;
                bool selected = selectedText != null && selectedText.id == it.id;
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.height = 28;
                row.style.alignItems = Align.Center;
                row.style.paddingLeft = 12;
                row.style.paddingRight = 12;
                row.style.backgroundColor = selected ? BgSelected : StyleKeyword.Null;

                row.RegisterCallback<MouseDownEvent>(e =>
                {
                    if (e.button == 0) { selectedText = it; RebuildContent(); }
                    else if (e.button == 1) ShowPresetContextMenu(() => { textLib.RemovePreset(it.id); if (selectedText?.id == it.id) selectedText = null; RebuildContent(); });
                });
                row.RegisterCallback<MouseEnterEvent>(e => { if (selectedText?.id != it.id) row.style.backgroundColor = BgHover; });
                row.RegisterCallback<MouseLeaveEvent>(e => { row.style.backgroundColor = selectedText?.id == it.id ? BgSelected : StyleKeyword.Null; });

                var ag = new Label("Ag");
                ag.style.width = 30;
                ag.style.unityFontStyleAndWeight = it.fontWeight == "Bold" ? FontStyle.Bold : FontStyle.Normal;
                ag.style.fontSize = 14;
                ag.style.color = TextWhite;
                row.Add(ag);

                var nameLabel = new Label(it.name);
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

                var lhLabel = new Label(it.GetLineHeightDisplay());
                lhLabel.style.width = 50;
                lhLabel.style.unityTextAlign = TextAnchor.MiddleRight;
                lhLabel.style.color = TextGray;
                lhLabel.style.fontSize = 12;
                row.Add(lhLabel);

                scroll.Add(row);
            }
        }

        private void BuildTextDetail(VisualElement right)
        {
            right.Clear();
            if (selectedText == null)
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
            var previewLabel = new Label("Rag 123");
            previewLabel.style.fontSize = Mathf.Clamp(selectedText.fontSize, 14, 48);
            previewLabel.style.unityFontStyleAndWeight = selectedText.fontWeight == "Bold" ? FontStyle.Bold : FontStyle.Normal;
            previewLabel.style.color = TextWhite;
            previewBox.Add(previewLabel);
            right.Add(previewBox);

            right.Add(DetailLabel("名称"));
            right.Add(DetailTextField(selectedText.name, v =>
            {
                selectedText.name = v;
                textLib.Save();
                RebuildContent();
            }));

            right.Add(DetailLabel("描述"));
            right.Add(DetailTextField(selectedText.description, v =>
            {
                selectedText.description = v;
                textLib.Save();
            }));

            var attrLabel = new Label("属性");
            attrLabel.style.color = TextWhite;
            attrLabel.style.fontSize = 13;
            attrLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            attrLabel.style.marginTop = 12;
            right.Add(attrLabel);

            var fontNames = Font.GetOSInstalledFontNames().ToList();
            fontNames.Sort();
            int fontIdx = fontNames.IndexOf(selectedText.fontFamily);
            if (fontIdx < 0 && fontNames.Count > 0) fontIdx = 0;

            if (fontNames.Count > 0)
            {
                var fontField = new PopupField<string>(fontNames, fontIdx);
                fontField.style.marginTop = 6;
                fontField.style.height = 22;
                fontField.RegisterValueChangedCallback(e =>
                {
                    selectedText.fontFamily = e.newValue;
                    textLib.Save();
                    RebuildContent();
                });
                right.Add(fontField);
            }

            var weightAndSizeRow = new VisualElement();
            weightAndSizeRow.style.flexDirection = FlexDirection.Row;
            weightAndSizeRow.style.marginTop = 6;

            var weights = new List<string> { "Thin", "Light", "Regular", "Medium", "SemiBold", "Bold", "ExtraBold", "Black" };
            int wIdx = weights.IndexOf(selectedText.fontWeight);
            if (wIdx < 0) wIdx = 2;
            var weightField = new PopupField<string>(weights, wIdx);
            weightField.style.flexGrow = 1;
            weightField.style.height = 22;
            weightField.RegisterValueChangedCallback(e =>
            {
                selectedText.fontWeight = e.newValue;
                textLib.Save();
                RebuildContent();
            });
            weightAndSizeRow.Add(weightField);

            var sizeField = new IntegerField();
            sizeField.value = selectedText.fontSize;
            sizeField.style.width = 60;
            sizeField.style.height = 22;
            sizeField.style.marginLeft = 4;
            sizeField.RegisterValueChangedCallback(e =>
            {
                selectedText.fontSize = Mathf.Max(1, e.newValue);
                textLib.Save();
                RebuildContent();
            });
            weightAndSizeRow.Add(sizeField);
            right.Add(weightAndSizeRow);

            var lhAndLsRow = new VisualElement();
            lhAndLsRow.style.flexDirection = FlexDirection.Row;
            lhAndLsRow.style.marginTop = 6;

            var lhContainer = new VisualElement();
            lhContainer.style.flexGrow = 1;
            var lhLabel2 = new Label("行高");
            lhLabel2.style.fontSize = 11;
            lhLabel2.style.color = TextGray;
            lhContainer.Add(lhLabel2);
            var lhOptions = new List<string> { "Auto", "16", "20", "24", "28", "32", "36", "40", "48" };
            string curLh = selectedText.lineHeight < 0 ? "Auto" : ((int)selectedText.lineHeight).ToString();
            int lhIdx = lhOptions.IndexOf(curLh);
            if (lhIdx < 0) { lhOptions.Add(curLh); lhIdx = lhOptions.Count - 1; }
            var lhField = new PopupField<string>(lhOptions, lhIdx);
            lhField.style.height = 22;
            lhField.RegisterValueChangedCallback(e =>
            {
                selectedText.lineHeight = e.newValue == "Auto" ? -1f : float.Parse(e.newValue);
                textLib.Save();
                RebuildContent();
            });
            lhContainer.Add(lhField);
            lhAndLsRow.Add(lhContainer);

            var lsContainer = new VisualElement();
            lsContainer.style.flexGrow = 1;
            lsContainer.style.marginLeft = 4;
            var lsLabel = new Label("字距");
            lsLabel.style.fontSize = 11;
            lsLabel.style.color = TextGray;
            lsContainer.Add(lsLabel);
            var lsField = new FloatField();
            lsField.value = selectedText.letterSpacing;
            lsField.style.height = 22;
            lsField.RegisterValueChangedCallback(e =>
            {
                selectedText.letterSpacing = e.newValue;
                textLib.Save();
            });
            lsContainer.Add(lsField);
            lhAndLsRow.Add(lsContainer);

            right.Add(lhAndLsRow);
        }

        // ─────────────────────── Shared UI Helpers ───────────────────────

        private VisualElement ThreeColumnLayout()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.flexGrow = 1;

            var left = new VisualElement();
            left.name = "left";
            left.style.width = 110;
            left.style.backgroundColor = BgPanel;
            left.style.borderRightWidth = 1;
            left.style.borderRightColor = BorderCol;
            left.style.paddingTop = 8;
            row.Add(left);

            var center = new VisualElement();
            center.name = "center";
            center.style.flexGrow = 1;
            center.style.flexDirection = FlexDirection.Column;
            row.Add(center);

            var right = new VisualElement();
            right.name = "right";
            right.style.width = 260;
            right.style.backgroundColor = BgPanel;
            right.style.borderLeftWidth = 1;
            right.style.borderLeftColor = BorderCol;
            right.style.paddingTop = 12;
            right.style.paddingLeft = 14;
            right.style.paddingRight = 14;
            row.Add(right);

            return row;
        }

        private void AddCategoryButton(VisualElement parent, string text, bool selected, Action onClick, Action onRightClick = null)
        {
            var btn = new Label(text);
            btn.style.height = 30;
            btn.style.unityTextAlign = TextAnchor.MiddleCenter;
            btn.style.fontSize = 12;
            btn.style.color = TextWhite;
            btn.style.backgroundColor = selected ? BgSelected : StyleKeyword.Null;
            btn.style.marginLeft = 6;
            btn.style.marginRight = 6;
            btn.style.marginTop = 2;
            btn.style.borderTopLeftRadius = btn.style.borderTopRightRadius = btn.style.borderBottomLeftRadius = btn.style.borderBottomRightRadius = 4;

            btn.RegisterCallback<MouseDownEvent>(e =>
            {
                if (e.button == 0) onClick?.Invoke();
                else if (e.button == 1) onRightClick?.Invoke();
            });
            btn.RegisterCallback<MouseEnterEvent>(e => { if (!selected) btn.style.backgroundColor = BgHover; });
            btn.RegisterCallback<MouseLeaveEvent>(e => { btn.style.backgroundColor = selected ? BgSelected : StyleKeyword.Null; });

            parent.Add(btn);
        }

        private void AddPlusButton(VisualElement parent, Action onClick)
        {
            var btn = new Label("+");
            btn.style.height = 30;
            btn.style.width = 30;
            btn.style.unityTextAlign = TextAnchor.MiddleCenter;
            btn.style.fontSize = 18;
            btn.style.color = TextGray;
            btn.style.alignSelf = Align.Center;
            btn.style.marginTop = 6;
            btn.style.borderTopLeftRadius = btn.style.borderTopRightRadius = btn.style.borderBottomLeftRadius = btn.style.borderBottomRightRadius = 4;

            btn.RegisterCallback<MouseDownEvent>(e => onClick?.Invoke());
            btn.RegisterCallback<MouseEnterEvent>(e => btn.style.backgroundColor = BgHover);
            btn.RegisterCallback<MouseLeaveEvent>(e => btn.style.backgroundColor = StyleKeyword.Null);

            parent.Add(btn);
        }

        private VisualElement ListHeaderRow(params string[] columns)
        {
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.height = 28;
            header.style.alignItems = Align.Center;
            header.style.paddingLeft = 12;
            header.style.paddingRight = 12;
            header.style.borderBottomWidth = 1;
            header.style.borderBottomColor = BorderCol;
            header.style.backgroundColor = BgPanel;

            var addBtn = new Label("+");
            addBtn.name = "addBtn";
            addBtn.style.width = 24;
            addBtn.style.height = 24;
            addBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
            addBtn.style.fontSize = 16;
            addBtn.style.color = TextGray;
            addBtn.style.marginRight = 8;
            addBtn.style.borderTopLeftRadius = addBtn.style.borderTopRightRadius = addBtn.style.borderBottomLeftRadius = addBtn.style.borderBottomRightRadius = 3;
            addBtn.RegisterCallback<MouseEnterEvent>(e => addBtn.style.backgroundColor = BgHover);
            addBtn.RegisterCallback<MouseLeaveEvent>(e => addBtn.style.backgroundColor = StyleKeyword.Null);
            header.Add(addBtn);

            for (int i = 0; i < columns.Length; i++)
            {
                var col = new Label(columns[i]);
                col.style.color = TextGray;
                col.style.fontSize = 11;
                if (i == 0)
                    col.style.flexGrow = 1;
                else
                {
                    col.style.width = i == columns.Length - 1 && columns.Length <= 2 ? 80 : 50;
                    col.style.unityTextAlign = TextAnchor.MiddleRight;
                }
                header.Add(col);
            }

            return header;
        }

        private Label DetailLabel(string text)
        {
            var lbl = new Label(text);
            lbl.style.color = TextGray;
            lbl.style.fontSize = 11;
            lbl.style.marginTop = 8;
            return lbl;
        }

        private VisualElement DetailTextField(string value, Action<string> onChange)
        {
            var field = new TextField();
            field.value = value ?? "";
            field.style.height = 22;
            field.style.marginTop = 2;
            field.RegisterCallback<FocusOutEvent>(e =>
            {
                onChange?.Invoke(field.value);
            });
            return field;
        }

        // ─────────────────────── Context Menus & Popups ───────────────────────

        private void ShowCategoryContextMenu(string catName, bool isText)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("删除分类 \"" + catName + "\""), false, () =>
            {
                if (isText)
                {
                    textLib?.RemoveCategory(catName);
                    if (textFilterCat == catName) textFilterCat = "全部";
                }
                else
                {
                    colorLib?.RemoveCategory(catName);
                    if (colorFilterCat == catName) colorFilterCat = "全部";
                }
                RebuildContent();
            });
            menu.ShowAsContext();
        }

        private void ShowPresetContextMenu(Action deleteAction)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("删除"), false, () => deleteAction?.Invoke());
            menu.ShowAsContext();
        }

        private void ShowAddCategoryPopup(Action<string> onAdd)
        {
            AddCategoryPopup.Show(position, onAdd);
        }

        private void RebuildContent()
        {
            contentRoot.Clear();
            switch (activeTab)
            {
                case 0: BuildWidgetPanel(); break;
                case 1: BuildTextPanel(); break;
                case 2: BuildColorPanel(); break;
            }
        }
    }

    // ─────────────────────── Add Category Popup ───────────────────────

    public class AddCategoryPopup : EditorWindow
    {
        private string inputName = "";
        private Action<string> callback;

        public static void Show(Rect parentRect, Action<string> onAdd)
        {
            var popup = CreateInstance<AddCategoryPopup>();
            popup.callback = onAdd;
            popup.titleContent = new GUIContent("添加分类");
            var size = new Vector2(260, 90);
            popup.minSize = size;
            popup.maxSize = size;
            popup.position = new Rect(parentRect.x + 120, parentRect.y + 200, size.x, size.y);
            popup.ShowUtility();
        }

        private void OnEnable()
        {
            var root = rootVisualElement;
            root.style.paddingTop = 12;
            root.style.paddingLeft = 12;
            root.style.paddingRight = 12;

            var field = new TextField("分类名称");
            field.style.marginBottom = 10;
            field.RegisterValueChangedCallback(e => inputName = e.newValue);
            root.Add(field);

            var btnRow = new VisualElement();
            btnRow.style.flexDirection = FlexDirection.Row;
            btnRow.style.justifyContent = Justify.FlexEnd;

            var cancelBtn = new Button(Close) { text = "取消" };
            cancelBtn.style.width = 60;
            btnRow.Add(cancelBtn);

            var okBtn = new Button(() =>
            {
                if (!string.IsNullOrEmpty(inputName))
                {
                    callback?.Invoke(inputName);
                    Close();
                }
            }) { text = "确定" };
            okBtn.style.width = 60;
            okBtn.style.marginLeft = 6;
            btnRow.Add(okBtn);

            root.Add(btnRow);
        }
    }
}
#endif
