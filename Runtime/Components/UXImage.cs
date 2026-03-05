using UnityEngine;
using UnityEngine.UI;

namespace UITool
{
    /// <summary>
    /// 继承 Image，增加颜色预设绑定能力。
    /// 选择预设后自动应用颜色，同时保持 Image 的全部功能。
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("UI/UXImage")]
    public class UXImage : Image, IColorPresetTarget
    {
        #region 颜色预设字段

        [SerializeField] private ColorPresetAsset _colorPresetAsset;
        [SerializeField] private string _colorPresetId;
        [SerializeField] private bool _applyOnAwake = true;

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
        public bool ApplyOnAwake
        {
            get => _applyOnAwake;
            set => _applyOnAwake = value;
        }

        #endregion

        #region Unity 生命周期

        protected override void Awake()
        {
            base.Awake();
            if (_applyOnAwake)
                ApplyColorPreset();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            ApplyColorPreset();
        }
#endif

        #endregion

        #region IColorPresetTarget 实现

        /// <summary>
        /// 将当前绑定的颜色预设应用到自身的 Image 颜色
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

        // 颜色预设切换 API（SetColor / SetColorByName）
        // 由 IColorPresetTarget 扩展方法统一提供，无需在此重复实现。
    }
}
