#if TMP_PRESENT
using TMPro;
using UnityEngine;

namespace UITool
{
    /// <summary>
    /// 继承 TextMeshProUGUI，增加文字预设绑定能力。
    /// 选择预设后自动应用字体、样式、字号、间距等参数，同时保持 TMP 的全部功能。
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("UI/UXText")]
    public class UXText : TextMeshProUGUI
    {
        [SerializeField] private TextPresetAsset presetAsset;
        [SerializeField] private string presetId;
        [SerializeField] private bool applyOnAwake = true;

        public TextPresetAsset PresetAsset
        {
            get => presetAsset;
            set { presetAsset = value; ApplyPreset(); }
        }

        public string PresetId
        {
            get => presetId;
            set { presetId = value; ApplyPreset(); }
        }

        public bool ApplyOnAwake
        {
            get => applyOnAwake;
            set => applyOnAwake = value;
        }

        protected override void Awake()
        {
            base.Awake();
            if (applyOnAwake)
                ApplyPreset();
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            ApplyPreset();
        }

        /// <summary>
        /// 根据当前绑定的预设 ID 将预设参数应用到自身
        /// </summary>
        public void ApplyPreset()
        {
            if (presetAsset == null || string.IsNullOrEmpty(presetId)) return;

            var entry = presetAsset.FindById(presetId);
            if (entry == null) return;

            if (entry.fontAsset != null) font = entry.fontAsset;
            fontStyle = entry.fontStyle;
            fontSize = entry.fontSize;
            lineSpacing = entry.lineSpacing;
            characterSpacing = entry.characterSpacing;
        }

        /// <summary>
        /// 获取当前绑定的预设名称
        /// </summary>
        public string GetPresetName()
        {
            if (presetAsset == null || string.IsNullOrEmpty(presetId)) return "无";
            var entry = presetAsset.FindById(presetId);
            return entry != null ? entry.presetName : "缺失";
        }
    }
}
#endif
