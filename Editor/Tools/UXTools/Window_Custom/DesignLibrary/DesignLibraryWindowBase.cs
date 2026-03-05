#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UITool
{
    public abstract class DesignLibraryWindowBase : EditorWindow
    {
        protected VisualElement contentRoot;

        protected static readonly Color BgDark = new Color(0.19f, 0.19f, 0.19f);
        protected static readonly Color BgPanel = new Color(0.22f, 0.22f, 0.22f);
        protected static readonly Color BgSelected = new Color(0.17f, 0.36f, 0.53f);
        protected static readonly Color BgHover = new Color(0.3f, 0.3f, 0.3f);
        protected static readonly Color BorderCol = new Color(0.14f, 0.14f, 0.14f);
        protected static readonly Color TextWhite = new Color(0.85f, 0.85f, 0.85f);
        protected static readonly Color TextGray = new Color(0.55f, 0.55f, 0.55f);

        protected void InitRootUI()
        {
            var root = rootVisualElement;
            root.Clear();
            root.style.flexDirection = FlexDirection.Column;
            root.style.backgroundColor = BgDark;

            contentRoot = new VisualElement();
            contentRoot.style.flexGrow = 1;
            root.Add(contentRoot);

            BuildContent();
        }

        protected void RebuildContent()
        {
            contentRoot.Clear();
            BuildContent();
        }

        protected abstract void BuildContent();

        #region Shared UI Helpers

        protected VisualElement ThreeColumnLayout()
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

        protected void AddCategoryButton(VisualElement parent, string text, bool selected, Action onClick, Action onRightClick = null)
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

        protected void AddPlusButton(VisualElement parent, Action onClick)
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

        protected VisualElement ListHeaderRow(params string[] columns)
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

        protected Label DetailLabel(string text)
        {
            var lbl = new Label(text);
            lbl.style.color = TextGray;
            lbl.style.fontSize = 11;
            lbl.style.marginTop = 8;
            return lbl;
        }

        protected VisualElement DetailTextField(string value, Action<string> onChange)
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

        protected void ShowPresetContextMenu(string presetId, Action deleteAction)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("复制 ID"), false, () =>
            {
                GUIUtility.systemCopyBuffer = presetId;
                Debug.Log($"[UXTools] 已复制预设 ID: {presetId}");
            });
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("删除"), false, () =>
            {
                if (EditorUtility.DisplayDialog("确认删除", "删除后不可恢复，确定要删除此预设吗？", "删除", "取消"))
                    deleteAction?.Invoke();
            });
            menu.ShowAsContext();
        }

        protected void ShowAddCategoryPopup(Action<string> onAdd)
        {
            AddCategoryPopup.Show(position, onAdd);
        }

        #endregion
    }

    #region Add Category Popup

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

    #endregion
}
#endif
