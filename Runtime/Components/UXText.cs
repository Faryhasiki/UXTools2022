#if TMP_PRESENT
using TMPro;
using UnityEngine;

namespace UITool
{
    /// <summary>
    /// 继承 TextMeshProUGUI，增加文字预设和颜色预设绑定能力。
    /// 选择预设后自动应用字体、样式、字号、间距、颜色等参数，同时保持 TMP 的全部功能。
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("UI/UXText")]
    public class UXText : TextMeshProUGUI, IColorPresetTarget
    {
        #region 文字预设字段

        [SerializeField] private TextPresetAsset _presetAsset;
        [SerializeField] private string _presetId;
        [SerializeField] private bool _applyOnAwake = true;

        /// <summary>
        /// 文字预设库资产引用
        /// </summary>
        public TextPresetAsset PresetAsset
        {
            get => _presetAsset;
            set { _presetAsset = value; ApplyPreset(); }
        }

        /// <summary>
        /// 当前绑定的文字预设 ID
        /// </summary>
        public string PresetId
        {
            get => _presetId;
            set { _presetId = value; ApplyPreset(); }
        }

        /// <summary>
        /// 是否在 Awake 时自动应用预设
        /// </summary>
        public bool ApplyOnAwake
        {
            get => _applyOnAwake;
            set => _applyOnAwake = value;
        }

        #endregion

        #region 颜色预设字段

        [SerializeField] private ColorPresetAsset _colorPresetAsset;
        [SerializeField] private string _colorPresetId;
        [SerializeField] private bool _applyColorOnAwake = true;

        /// <summary>
        /// 颜色预设库资产引用
        /// </summary>
        public ColorPresetAsset ColorPresetAsset
        {
            get => _colorPresetAsset;
            set { _colorPresetAsset = value; ApplyColorPreset(); }
        }

        /// <summary>
        /// 当前绑定的颜色预设 ID
        /// </summary>
        public string ColorPresetId
        {
            get => _colorPresetId;
            set { _colorPresetId = value; ApplyColorPreset(); }
        }

        /// <summary>
        /// 是否在 Awake 时自动应用颜色预设
        /// </summary>
        public bool ApplyColorOnAwake
        {
            get => _applyColorOnAwake;
            set => _applyColorOnAwake = value;
        }

        #endregion

        #region Unity 生命周期

        protected override void Awake()
        {
            base.Awake();
            if (_applyOnAwake)
                ApplyPreset();
            if (_applyColorOnAwake)
                ApplyColorPreset();
        }
#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            ApplyPreset();
            ApplyColorPreset();
        }
#endif
        #endregion

        #region 文字预设方法

        /// <summary>
        /// 根据当前绑定的文字预设 ID 将预设参数应用到自身
        /// </summary>
        public void ApplyPreset()
        {
            if (_presetAsset == null || string.IsNullOrEmpty(_presetId)) return;

            var entry = _presetAsset.FindById(_presetId);
            if (entry == null) return;

            if (entry.fontAsset != null) font = entry.fontAsset;
            fontStyle = entry.fontStyle;
            fontSize = entry.fontSize;
            lineSpacing = entry.lineSpacing;
            characterSpacing = entry.characterSpacing;
        }

        /// <summary>
        /// 获取当前绑定的文字预设名称
        /// </summary>
        public string GetPresetName()
        {
            if (_presetAsset == null || string.IsNullOrEmpty(_presetId)) return "无";
            var entry = _presetAsset.FindById(_presetId);
            return entry != null ? entry.presetName : "缺失";
        }

        #endregion

        #region IColorPresetTarget 实现

        /// <summary>
        /// 将当前绑定的颜色预设应用到自身的文字颜色
        /// </summary>
        public void ApplyColorPreset()
        {
            if (_colorPresetAsset == null || string.IsNullOrEmpty(_colorPresetId)) return;

            var entry = _colorPresetAsset.FindById(_colorPresetId);
            if (entry != null)
                color = entry.GetColor();
        }

        /// <summary>
        /// 获取当前绑定的颜色预设名称
        /// </summary>
        public string GetColorPresetName()
        {
            if (_colorPresetAsset == null || string.IsNullOrEmpty(_colorPresetId)) return "无";
            var entry = _colorPresetAsset.FindById(_colorPresetId);
            return entry != null ? entry.presetName : "缺失";
        }

        #endregion
    }
}
#endif
