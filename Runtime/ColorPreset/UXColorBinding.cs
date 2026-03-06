using UnityEngine;
using UnityEngine.UI;

namespace UITool
{
    /// <summary>
    /// 通用颜色预设绑定组件。
    /// 可挂载到任何含有 Graphic 组件的 GameObject 上（RawImage、第三方组件等），
    /// 实现颜色预设的绑定与自动应用。
    /// 注意：如果 Graphic 本身已实现 IColorPresetTarget（如 UXText、UXImage），
    /// 此组件会自动跳过以避免冲突。
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Graphic))]
    [AddComponentMenu("UI/UXColorBinding")]
    public class UXColorBinding : MonoBehaviour, IColorPresetTarget
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

        #region 冲突检测

        /// <summary>
        /// 检查同 GameObject 上的 Graphic 是否已自带颜色预设功能（UXText、UXImage 等）
        /// </summary>
        public bool HasConflict()
        {
            if (!TryGetComponent<Graphic>(out var graphic)) return false;
            return graphic is IColorPresetTarget;
        }

        #endregion

        #region Unity 生命周期

        private void Awake()
        {
            if (_applyOnAwake)
                ApplyColorPreset();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ApplyColorPreset();
        }
#endif

        #endregion

        #region IColorPresetTarget 实现

        /// <summary>
        /// 将当前绑定的颜色预设应用到同 GameObject 上的 Graphic 组件。
        /// 如果 Graphic 已自带 IColorPresetTarget（如 UXText/UXImage），则跳过以避免覆盖。
        /// </summary>
        public void ApplyColorPreset()
        {
            if (_colorPresetAsset == null || string.IsNullOrEmpty(_colorPresetId)) return;
            if (HasConflict()) return;

            var entry = _colorPresetAsset.FindById(_colorPresetId);
            if (entry == null) return;

            if (TryGetComponent<Graphic>(out var graphic))
                graphic.color = entry.GetColor();
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
