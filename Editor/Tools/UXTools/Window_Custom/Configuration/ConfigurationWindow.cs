#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

using System.IO;
using System.Linq;
using System;

namespace UITool
{
    public class ConfigurationOption : VisualElement
    {
        Action clickAction;
        float height = 40;
        public TextElement nameLabel;
        private bool m_selected = false;

        public ConfigurationOption(string text = "", Action action = null)
        {
            clickAction = action;

            style.position = Position.Absolute;
            style.left = 0;
            style.right = 0;
            style.height = height;

            nameLabel = new TextElement();
            nameLabel.text = text;
            nameLabel.style.position = Position.Absolute;
            nameLabel.style.bottom = 10;
            nameLabel.style.alignSelf = Align.Center;
            nameLabel.style.fontSize = 15;
            nameLabel.style.color = Color.white;
            this.Add(nameLabel);

            RegisterCallback<MouseDownEvent>(OnClick);
            RegisterCallback<MouseEnterEvent>(OnHoverStateChang);
            RegisterCallback<MouseLeaveEvent>(OnHoverStateChang);
        }

        private void OnHoverStateChang(EventBase e)
        {
            if (!m_selected)
            {
                if (e.eventTypeId == MouseEnterEvent.TypeId())
                {

                    style.backgroundColor = new Color(0.6f, 0.6f, 0.6f, 1);

                }
                else if (e.eventTypeId == MouseLeaveEvent.TypeId())
                {
                    if (parent != null)
                    {
                        style.backgroundColor = parent.style.backgroundColor;
                    }

                }
            }
        }

        public void isSelect()
        {
            m_selected = true;
            style.backgroundColor = new Color(0.6f, 0.6f, 0.6f, 1);
        }

        private void OnClick(MouseDownEvent e)
        {
            if (e.button == 0)
            {
                if (!m_selected)
                {
                    clickAction.Invoke();
                }
            }
        }
    }

    public class ConfigurationWindow : EditorWindow
    {
        private static ConfigurationWindow c_window;

        /// <summary>
        /// 打开文档：若在设置中配置了 URL 则打开 URL，否则打开本包内 md 文档。
        /// </summary>
        [MenuItem(UIToolConfig.Menu_OpenDocumentation, false, 200)]
        public static void OpenDocumentation()
        {
            var data = JsonAssetManager.GetAssets<UXToolCommonData>();
            string url = data?.DocumentationUrl?.Trim();
            if (!string.IsNullOrEmpty(url))
            {
                Application.OpenURL(url);
                return;
            }

            string localPath = FindLocalDocPath();
            if (!string.IsNullOrEmpty(localPath))
            {
                EditorUtility.OpenWithDefaultApp(localPath);
            }
            else
            {
                Debug.LogWarning("[UXTools] 未找到本地文档文件，请在 UXTool → 设置 → 通用 中配置文档链接。");
            }
        }

        /// <summary>
        /// 在 Assets/UXTools 和 Packages/com.ys4fun.uxtools 两处查找文档文件。
        /// </summary>
        private static string FindLocalDocPath()
        {
            string docFileName = "UXTools-用户手册.md";
            // 开发模式：Assets/UXTools/Documentation~/
            string inAssets = Path.Combine(Application.dataPath, "UXTools", "Documentation~", docFileName);
            if (File.Exists(inAssets)) return inAssets;
            // 安装包模式：Packages/com.ys4fun.uxtools/Documentation~/
            string projRoot = Path.GetDirectoryName(Application.dataPath);
            string inPackage = Path.Combine(projRoot, "Packages", "com.ys4fun.uxtools", "Documentation~", docFileName);
            if (File.Exists(inPackage)) return inPackage;
            return string.Empty;
        }

        [MenuItem(UIToolConfig.Menu_Setting, false, -148)]
        public static void OpenWindow()
        {
            int width = 650;
            int height = 430;
            c_window = GetWindow<ConfigurationWindow>();
            c_window.minSize = new Vector2(width, height);
            c_window.maxSize = new Vector2(width, height);
            c_window.position = new Rect((Screen.currentResolution.width - width) / 2, (Screen.currentResolution.height - height) / 2, width, height);
            c_window.titleContent.text = EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_设置);
        }

        public static void CloseWindow()
        {
            if (c_window != null)
            {
                c_window.Close();
            }
        }

        [UnityEditor.Callbacks.DidReloadScripts(0)]
        private static void OnScriptReload()
        {
            if (HasOpenInstances<ConfigurationWindow>())
                c_window = GetWindow<ConfigurationWindow>();
        }

        private void OnEnable()
        {
            InitWindowData();
            InitWindowUI();
        }

        private VisualElement Root;
        private VisualElement rightContainer;
        private VisualElement leftContainer;
        private Toggle[] switchToggles;
        private IntegerField maxFilesField;
        private IntegerField maxPrefabsField;
        private TextElement errorLabel;
        private TextElement errorPrefabLabel;
        private TextField _docUrlField;

        private ConfigurationOption GeneralOption;
        private ConfigurationOption StorageOption;
        private ConfigurationOption SwitchOption;
        private ConfigurationOption PathOption;

        private WidgetInstantiateMode PrefabDragMode;
        // private string prefabPath;
        // private string componentPath;

        private UXToolCommonData commonData;

        private void InitWindowData()
        {
            int max = Enum.GetValues(typeof(SwitchSetting.SwitchType)).Cast<int>().Max();
            switchToggles = new Toggle[max + 1];
            for (int i = 0; i < switchToggles.Length; i++)
            {
                switchToggles[i] = new Toggle();
                switchToggles[i].value = SwitchSetting.CheckValid(i);
            }

            commonData = JsonAssetManager.GetAssets<UXToolCommonData>();
        }

        private void InitWindowUI()
        {
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UIToolConfig.UIBuilderPath + "SettingWindow.uxml");
            Root = visualTree.CloneTree();
            rootVisualElement.Add(Root);
            Root.style.alignSelf = Align.Center;

            leftContainer = Root.Q<VisualElement>("LeftContainer");
            rightContainer = Root.Q<VisualElement>("RightContainer");

            Label nameLabel = Root.Q<Label>("Title");
            nameLabel.text = EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_设置);

            Button confirmBtn = Root.Q<Button>("ConfirmBtn");
            confirmBtn.clicked += ConfirmOnClick;
            confirmBtn.text = EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_确定);

            Button cancelBtn = Root.Q<Button>("CancelBtn");
            cancelBtn.clicked += CloseWindow;
            cancelBtn.text = EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_取消);

            leftContainerRefresh();
            GeneralOnClick();
        }

        private void GeneralOnClick()
        {
            leftContainerRefresh();
            GeneralOption.isSelect();
            rightContainer.Clear();

            VisualElement container = UXBuilder.Div(rightContainer, new UXBuilderDivStruct()
            {
                style = new UXStyle()
                {
                    position = Position.Absolute,
                    alignSelf = Align.Center,
                    bottom = 5,
                    top = 5,
                    width = 400,
                }
            });

            AddGeneralSetting(container);

            AddFilesCountLimit(container);

        }

        private void AddGeneralSetting(VisualElement container)
        {
            UXBuilder.Text(container, new UXBuilderTextStruct()
            {
                text = EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_通用),
                style = new UXStyle()
                {
                    position = Position.Absolute,
                    left = -40,
                    fontSize = 16,
                    top = 0,
                    color = Color.white,
                }
            });

            // 文档链接配置
            UXBuilder.Text(container, new UXBuilderTextStruct()
            {
                text = "文档链接 URL",
                style = new UXStyle()
                {
                    position = Position.Absolute,
                    left = 0,
                    fontSize = 13,
                    top = 30,
                    color = Color.white,
                }
            });

            _docUrlField = new TextField();
            _docUrlField.style.position = Position.Absolute;
            _docUrlField.style.top = 52;
            _docUrlField.style.left = 0;
            _docUrlField.style.right = 0;
            _docUrlField.style.height = 22;
            _docUrlField.value = commonData?.DocumentationUrl ?? string.Empty;
            container.Add(_docUrlField);

            UXBuilder.Text(container, new UXBuilderTextStruct()
            {
                text = "留空时点击「文档」菜单将打开本地 md 文档",
                style = new UXStyle()
                {
                    position = Position.Absolute,
                    left = 0,
                    fontSize = 11,
                    top = 77,
                    color = new Color(0.6f, 0.6f, 0.6f, 1f),
                }
            });
        }


        private void AddFilesCountLimit(VisualElement container)
        {
            TextElement limitTitleLabel = UXBuilder.Text(container, new UXBuilderTextStruct()
            {
                text = EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_面板设置),
                style = new UXStyle()
                {
                    position = Position.Absolute,
                    left = -40,
                    fontSize = 16,
                    top = 100,
                    color = Color.white,
                }
            });

            TextElement maxPrefabsLabel = UXBuilder.Text(container, new UXBuilderTextStruct()
            {
                text = EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_最近打开模板数上限),
                style = new UXStyle()
                {
                    position = Position.Absolute,
                    left = 0,
                    fontSize = 13,
                    top = 140,
                    color = Color.white,
                    maxWidth = 270,
                }
            });

            maxPrefabsField = new IntegerField();
            maxPrefabsField.style.position = Position.Absolute;
            maxPrefabsField.style.width = 137;
            maxPrefabsField.style.height = 25;
            maxPrefabsField.style.top = 140;
            maxPrefabsField.style.right = 0;
            if (commonData != null)
            {
                maxPrefabsField.value = commonData.MaxRecentOpenedPrefabs;
            }

            container.Add(maxPrefabsField);
            errorPrefabLabel = UXBuilder.Text(container, new UXBuilderTextStruct()
            {
                text = EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_显示上限必须大于0),
                style = new UXStyle()
                {
                    position = Position.Absolute,
                    color = Color.red,
                    maxWidth = 137,
                    display = DisplayStyle.None,
                    fontSize = 13,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    top = 165,
                    right = 1,

                }
            });


            TextElement maxFilesLabel = UXBuilder.Text(container, new UXBuilderTextStruct()
            {
                text = EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_最近选中文件数上限),
                style = new UXStyle()
                {
                    position = Position.Absolute,
                    left = 0,
                    fontSize = 13,
                    top = 195,
                    color = Color.white,
                    maxWidth = 250,
                }
            });

            maxFilesField = new IntegerField();
            maxFilesField.style.position = Position.Absolute;
            maxFilesField.style.width = 137;
            maxFilesField.style.height = 25;
            maxFilesField.style.top = 195;
            maxFilesField.style.right = 0;
            if (commonData != null)
            {
                maxFilesField.value = commonData.MaxRecentSelectedFiles;
            }

            container.Add(maxFilesField);

            errorLabel = UXBuilder.Text(container, new UXBuilderTextStruct()
            {
                text = EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_显示上限必须大于0),
                style = new UXStyle()
                {
                    position = Position.Absolute,
                    color = Color.red,
                    maxWidth = 137,
                    display = DisplayStyle.None,
                    fontSize = 13,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    top = 220,
                    right = 1,

                }
            });
        }

        private void leftContainerRefresh()
        {
            leftContainer.Clear();

            GeneralOption = new ConfigurationOption(EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_通用), GeneralOnClick);
            GeneralOption.style.top = 0;
            leftContainer.Add(GeneralOption);

            SwitchOption = new ConfigurationOption(EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_功能开关), SwitchOnClick);
            SwitchOption.style.top = 40;
            leftContainer.Add(SwitchOption);

            PathOption = new ConfigurationOption("路径设置", PathOnClick);
            PathOption.style.top = 80;
            leftContainer.Add(PathOption);
        }


        Dictionary<string, List<(string, int)>> GetSwitchCategories()
        {
            // 这里的字典key为分类名称，value为该分类下的所有开关的本地化以及SwitchSetting对应的SwitchType序号
            return new Dictionary<string, List<(string, int)>>()
            {
                { "基础common", new List<(string , int)> {
                    (EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_对齐吸附), 1),
                    (EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_右键选择列表), 2),
                    (EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_快速复制), 3),
                    (EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_移动快捷键), 4)
                }},
                { "操作记录", new List<(string , int)> {
                    (EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_最近打开面板记录), 0),
                    (EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_最近选中面板记录), 8)
                }},
                { "其他", new List<(string , int)> {
                    (EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_Prefab多开), 5)
                }}
            };
        }

        private void AddSwitchToggle(VisualElement container, string text, ref Toggle toggle)
        {
            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginTop = 5;
            row.style.marginBottom = 5;
            row.style.marginLeft = 40;

            toggle.style.marginRight = 10;
            row.Add(toggle);

            Label label = new Label();
            label.text = text;

            label.style.fontSize = 13;
            label.style.color = Color.white;

            row.Add(label);
            container.Add(row);
        }
        private void SwitchOnClick()
        {
            leftContainerRefresh();
            SwitchOption.isSelect();
            rightContainer.Clear();
            ScrollView scrollView = new ScrollView();
            scrollView.style.flexGrow = 1;
            rightContainer.Add(scrollView);

            var categories = GetSwitchCategories();

            foreach (var category in categories)
            {
                // 添加分类标题
                Label header = new Label(category.Key);
                header.style.fontSize = 16;
                header.style.color = Color.white;
                header.style.marginLeft = 20;
                scrollView.Add(header);

                // 添加该分类下的所有开关
                for (int i = 0; i < category.Value.Count; i++)
                {
                    var (localization, toggleIndex) = category.Value[i];
                    AddSwitchToggle(scrollView, localization, ref switchToggles[toggleIndex]);
                }
            }
        }

        private TextField _configParentField;
        private TextField _editorParentField;
        private TextField _runtimeParentField;
        private Toggle _enableEditorToggle;
        private Toggle _enableRuntimeToggle;
        private string _origConfigParent;
        private string _origEditorParent;
        private string _origRuntimeParent;
        private Label _previewConfigLabel;
        private Label _previewEditorLabel;
        private Label _previewRuntimeLabel;

        private void PathOnClick()
        {
            leftContainerRefresh();
            PathOption.isSelect();
            rightContainer.Clear();

            var settings = UXToolsProjectSettings.Instance;
            _origConfigParent = settings.configParentPath;
            _origEditorParent = settings.editorParentPath;
            _origRuntimeParent = settings.runtimeParentPath;

            var scroll = new ScrollView();
            scroll.style.position = Position.Absolute;
            scroll.style.top = 5;
            scroll.style.bottom = 5;
            scroll.style.left = 20;
            scroll.style.right = 20;
            rightContainer.Add(scroll);

            var container = new VisualElement();
            scroll.Add(container);

            var titleLabel = new Label("路径设置");
            titleLabel.style.fontSize = 16;
            titleLabel.style.color = Color.white;
            titleLabel.style.marginBottom = 15;
            container.Add(titleLabel);

            // 1. 固定目录
            AddPathLabel(container, "编辑器数据目录（固定）");
            var fixedField = new TextField();
            fixedField.value = UIToolConfig.ProjectDataPath;
            fixedField.SetEnabled(false);
            fixedField.style.marginBottom = 12;
            container.Add(fixedField);

            // 2. 配置目录
            AddSectionHeader(container, $"配置目录 — {UXToolsProjectSettings.CONFIG_DIR_NAME}/");
            AddHintLabel(container, "颜色预设、文字预设等配置资产存放于此，始终创建。修改父级路径后会自动移动文件夹。");
            AddPathLabel(container, "父级路径");
            _configParentField = AddBrowsablePathField(container, settings.configParentPath, "选择配置目录父级路径");
            _previewConfigLabel = AddPreviewLabel(container, "");
            _configParentField.RegisterValueChangedCallback(e => RefreshPathPreview());

            // 3. Editor 扩展目录
            AddSectionHeader(container, $"Editor 扩展目录 — {UXToolsProjectSettings.EDITOR_DIR_NAME}/");
            AddHintLabel(container, "启用后自动创建目录，程序集关系由用户自行管理。");

            var editorRow = new VisualElement();
            editorRow.style.flexDirection = FlexDirection.Row;
            editorRow.style.alignItems = Align.Center;
            editorRow.style.marginBottom = 4;
            container.Add(editorRow);

            _enableEditorToggle = new Toggle("启用");
            _enableEditorToggle.value = settings.enableCustomEditor;
            _enableEditorToggle.style.marginRight = 12;
            _enableEditorToggle.RegisterValueChangedCallback(e => RefreshPathPreview());
            editorRow.Add(_enableEditorToggle);

            AddPathLabel(container, "父级路径");
            _editorParentField = AddBrowsablePathField(container, settings.editorParentPath, "选择 Editor 扩展目录父级路径");
            _previewEditorLabel = AddPreviewLabel(container, "");
            _editorParentField.RegisterValueChangedCallback(e => RefreshPathPreview());

            // 4. Runtime 扩展目录
            AddSectionHeader(container, $"Runtime 扩展目录 — {UXToolsProjectSettings.RUNTIME_DIR_NAME}/");
            AddHintLabel(container, "启用后自动创建目录，程序集关系由用户自行管理。");

            var runtimeRow = new VisualElement();
            runtimeRow.style.flexDirection = FlexDirection.Row;
            runtimeRow.style.alignItems = Align.Center;
            runtimeRow.style.marginBottom = 4;
            container.Add(runtimeRow);

            _enableRuntimeToggle = new Toggle("启用");
            _enableRuntimeToggle.value = settings.enableCustomRuntime;
            _enableRuntimeToggle.style.marginRight = 12;
            _enableRuntimeToggle.RegisterValueChangedCallback(e => RefreshPathPreview());
            runtimeRow.Add(_enableRuntimeToggle);

            AddPathLabel(container, "父级路径");
            _runtimeParentField = AddBrowsablePathField(container, settings.runtimeParentPath, "选择 Runtime 扩展目录父级路径");
            _previewRuntimeLabel = AddPreviewLabel(container, "");
            _runtimeParentField.RegisterValueChangedCallback(e => RefreshPathPreview());

            RefreshPathPreview();
        }

        private void RefreshPathPreview()
        {
            string configParent = UXToolsProjectSettings.EnsureTrailingSlash(_configParentField?.value ?? "Assets/");
            _previewConfigLabel.text = configParent + UXToolsProjectSettings.CONFIG_DIR_NAME + "/";

            if (_enableEditorToggle.value)
            {
                string editorParent = UXToolsProjectSettings.EnsureTrailingSlash(_editorParentField?.value ?? "Assets/");
                _previewEditorLabel.text = editorParent + UXToolsProjectSettings.EDITOR_DIR_NAME + "/";
                _previewEditorLabel.style.color = new Color(0.45f, 0.75f, 1f, 1f);
            }
            else
            {
                _previewEditorLabel.text = "（未启用）";
                _previewEditorLabel.style.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            }

            if (_enableRuntimeToggle.value)
            {
                string runtimeParent = UXToolsProjectSettings.EnsureTrailingSlash(_runtimeParentField?.value ?? "Assets/");
                _previewRuntimeLabel.text = runtimeParent + UXToolsProjectSettings.RUNTIME_DIR_NAME + "/";
                _previewRuntimeLabel.style.color = new Color(0.45f, 0.75f, 1f, 1f);
            }
            else
            {
                _previewRuntimeLabel.text = "（未启用）";
                _previewRuntimeLabel.style.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            }
        }

        private void AddPathLabel(VisualElement container, string text)
        {
            var label = new Label(text);
            label.style.fontSize = 13;
            label.style.color = Color.white;
            label.style.marginTop = 6;
            container.Add(label);
        }

        private void AddSectionHeader(VisualElement container, string text)
        {
            var label = new Label(text);
            label.style.fontSize = 13;
            label.style.color = Color.white;
            label.style.marginTop = 14;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            container.Add(label);
        }

        private TextField AddBrowsablePathField(VisualElement container, string value, string dialogTitle)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom = 2;
            container.Add(row);

            var field = new TextField();
            field.value = value ?? "Assets/";
            field.style.flexGrow = 1;
            row.Add(field);

            var browseBtn = new Button(() =>
            {
                string selected = EditorUtility.OpenFolderPanel(dialogTitle, "Assets", "");
                if (!string.IsNullOrEmpty(selected))
                {
                    string dataPath = Application.dataPath.Replace("\\", "/");
                    selected = selected.Replace("\\", "/");

                    if (!selected.StartsWith(dataPath, System.StringComparison.OrdinalIgnoreCase))
                    {
                        EditorUtility.DisplayDialog("路径错误", "目录必须在 Assets 目录下。", "确定");
                        return;
                    }
                    field.value = "Assets" + selected.Substring(dataPath.Length);
                }
            });
            browseBtn.text = "浏览";
            browseBtn.style.width = 50;
            row.Add(browseBtn);

            return field;
        }

        private Label AddPreviewLabel(VisualElement container, string text)
        {
            var label = new Label(text);
            label.style.fontSize = 12;
            label.style.color = new Color(0.45f, 0.75f, 1f, 1f);
            label.style.marginLeft = 4;
            label.style.marginBottom = 2;
            container.Add(label);
            return label;
        }

        private void AddHintLabel(VisualElement container, string text)
        {
            var hint = new Label(text);
            hint.style.fontSize = 11;
            hint.style.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            hint.style.whiteSpace = WhiteSpace.Normal;
            hint.style.marginBottom = 6;
            container.Add(hint);
        }

        private void ConfirmOnClick()
        {
            Selection.activeGameObject = null;

            SwitchSetting.ChangeSwitch(switchToggles);
            if (SceneViewToolBar.HaveToolbar)
            {
                SceneViewToolBar.CloseEditor();
                SceneViewToolBar.OpenEditor();
            }
            if (commonData != null)
            {
                if (maxPrefabsField.value >= 1)
                {
                    commonData.MaxRecentOpenedPrefabs = maxPrefabsField.value;
                    errorPrefabLabel.style.display = DisplayStyle.None;
                }
                else
                {
                    errorPrefabLabel.style.display = DisplayStyle.Flex;
                    return;
                }
                if (maxFilesField.value >= 1)
                {
                    commonData.MaxRecentSelectedFiles = maxFilesField.value;
                    errorLabel.style.display = DisplayStyle.None;
                }
                else
                {
                    errorLabel.style.display = DisplayStyle.Flex;
                    return;
                }

                commonData.DocumentationUrl = _docUrlField?.value?.Trim() ?? string.Empty;
                commonData.Save();
            }

            SavePathSettings();
            CloseWindow();
        }

        /// <summary>
        /// 尝试移动指定的固定名称目录到新父级路径下
        /// </summary>
        private static void TryMoveDir(string dirName, string oldParent, string newParent)
        {
            string normalizedOld = UXToolsProjectSettings.EnsureTrailingSlash(oldParent);
            string normalizedNew = UXToolsProjectSettings.EnsureTrailingSlash(newParent);

            if (normalizedOld == normalizedNew) return;

            UXToolsProjectSettings.MoveDirectory(dirName, normalizedOld, normalizedNew);
        }

        private void SavePathSettings()
        {
            if (_configParentField == null) return;

            var settings = UXToolsProjectSettings.Instance;
            bool dirty = false;

            // 配置目录父级路径变更
            string newConfigParent = _configParentField.value;
            if (string.IsNullOrEmpty(newConfigParent)) newConfigParent = "Assets/";
            if (newConfigParent != (_origConfigParent ?? settings.configParentPath))
            {
                TryMoveDir(UXToolsProjectSettings.CONFIG_DIR_NAME, _origConfigParent, newConfigParent);
                settings.configParentPath = newConfigParent;
                dirty = true;
            }

            // Editor 扩展目录
            if (_enableEditorToggle.value != settings.enableCustomEditor)
            {
                settings.enableCustomEditor = _enableEditorToggle.value;
                dirty = true;
            }
            string newEditorParent = _editorParentField.value;
            if (string.IsNullOrEmpty(newEditorParent)) newEditorParent = "Assets/";
            if (newEditorParent != (_origEditorParent ?? settings.editorParentPath))
            {
                if (settings.enableCustomEditor)
                    TryMoveDir(UXToolsProjectSettings.EDITOR_DIR_NAME, _origEditorParent, newEditorParent);
                settings.editorParentPath = newEditorParent;
                dirty = true;
            }

            // Runtime 扩展目录
            if (_enableRuntimeToggle.value != settings.enableCustomRuntime)
            {
                settings.enableCustomRuntime = _enableRuntimeToggle.value;
                dirty = true;
            }
            string newRuntimeParent = _runtimeParentField.value;
            if (string.IsNullOrEmpty(newRuntimeParent)) newRuntimeParent = "Assets/";
            if (newRuntimeParent != (_origRuntimeParent ?? settings.runtimeParentPath))
            {
                if (settings.enableCustomRuntime)
                    TryMoveDir(UXToolsProjectSettings.RUNTIME_DIR_NAME, _origRuntimeParent, newRuntimeParent);
                settings.runtimeParentPath = newRuntimeParent;
                dirty = true;
            }

            if (dirty)
            {
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                settings.EnsureOptionalDirectories();
            }
        }
    }
}
#endif