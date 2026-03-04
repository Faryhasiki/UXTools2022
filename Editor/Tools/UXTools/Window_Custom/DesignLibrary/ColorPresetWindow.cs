#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UITool
{
    public class ColorPresetWindow : DesignLibraryWindowBase
    {
        private ColorPresetLibrary colorLib;
        private string filterCat = "全部";
        private ColorPresetItem selected;

        [MenuItem(UIToolConfig.Menu_ColorPresets, false, 51)]
        public static void OpenWindow()
        {
            var win = GetWindow<ColorPresetWindow>();
            win.titleContent = new GUIContent("颜色预设");
            win.minSize = new Vector2(720, 400);
        }

        private void OnEnable()
        {
            colorLib = JsonAssetManager.GetAssets<ColorPresetLibrary>();
            InitRootUI();
        }

        protected override void BuildContent()
        {
            var row = ThreeColumnLayout();
            contentRoot.Add(row);

            BuildCategories(row.Q("left"));
            BuildList(row.Q("center"));
            BuildDetail(row.Q("right"));
        }

        #region Categories

        private void BuildCategories(VisualElement left)
        {
            left.Clear();
            AddCategoryButton(left, "全部", filterCat == "全部", () =>
            {
                filterCat = "全部";
                RebuildContent();
            });

            if (colorLib != null)
            {
                foreach (var cat in colorLib.categories)
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
                    colorLib?.AddCategory(name);
                    RebuildContent();
                });
            });
        }

        private void ShowDeleteCategoryMenu(string catName)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("删除分类 \"" + catName + "\""), false, () =>
            {
                colorLib?.RemoveCategory(catName);
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

            var header = ListHeaderRow("应用场景", "色号");
            var addBtn = header.Q("addBtn");
            addBtn?.RegisterCallback<MouseDownEvent>(e =>
            {
                var item = colorLib?.AddPreset(filterCat);
                if (item != null) selected = item;
                RebuildContent();
            });
            center.Add(header);

            var scroll = new ScrollView();
            scroll.style.flexGrow = 1;
            center.Add(scroll);

            if (colorLib == null) return;
            var items = colorLib.GetFiltered(filterCat);
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

                row.RegisterCallback<MouseDownEvent>(e =>
                {
                    if (e.button == 0) { selected = it; RebuildContent(); }
                    else if (e.button == 1) ShowPresetContextMenu(() => { colorLib.RemovePreset(it.id); if (selected?.id == it.id) selected = null; RebuildContent(); });
                });
                row.RegisterCallback<MouseEnterEvent>(e => { if (selected?.id != it.id) row.style.backgroundColor = BgHover; });
                row.RegisterCallback<MouseLeaveEvent>(e => { row.style.backgroundColor = selected?.id == it.id ? BgSelected : StyleKeyword.Null; });

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

        #endregion

        #region Detail

        private void BuildDetail(VisualElement right)
        {
            right.Clear();
            if (selected == null)
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
            preview.style.backgroundColor = selected.GetColor();
            right.Add(preview);

            right.Add(DetailLabel("名称"));
            var nameField = DetailTextField(selected.name, v =>
            {
                selected.name = v;
                colorLib.Save();
                RebuildContent();
            });
            right.Add(nameField);

            right.Add(DetailLabel("描述"));
            var descField = DetailTextField(selected.description, v =>
            {
                selected.description = v;
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
            colorField.value = selected.GetColor();
            colorField.showAlpha = true;
            colorField.style.flexGrow = 1;
            colorField.style.height = 24;
            colorField.RegisterValueChangedCallback(e =>
            {
                selected.SetColor(e.newValue);
                colorLib.Save();
                RebuildContent();
            });
            colorRow.Add(colorField);

            var opLabel = new Label(selected.opacity + " %");
            opLabel.style.width = 50;
            opLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            opLabel.style.color = TextGray;
            opLabel.style.marginLeft = 8;
            colorRow.Add(opLabel);

            right.Add(colorRow);
        }

        #endregion
    }
}
#endif
