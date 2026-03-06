#if TMP_PRESENT
using System;
using TMPro;
using UnityEngine;

namespace UITool
{
    /// <summary>
    /// 单条文字预设数据。实现 IUXTextStyleKey 接口，可直接作为文字预设键传递给组件。
    /// </summary>
    [Serializable]
    public class TextPresetEntry : IUXTextStyleKey
    {
        public string id;

        /// <summary>
        /// IUXTextStyleKey 实现，返回预设 ID
        /// </summary>
        string IUXTextStyleKey.PresetId => id;
        public string presetName;
        public string description;
        public string category;
        public TMP_FontAsset fontAsset;
        public FontStyles fontStyle;
        public int fontSize = 24;
        public float lineSpacing;
        public float characterSpacing;

        /// <summary>
        /// 是否为此预设生成代码常量（默认 false）
        /// </summary>
        public bool generateCode = false;

        /// <summary>
        /// 代码生成别名，用于生成更符合程序命名风格的常量名。
        /// 非空时优先使用别名作为生成的字段名，否则使用 presetName。
        /// </summary>
        public string codeAlias = "";

        public void ApplyTo(TMP_Text text)
        {
            if (text == null) return;
            if (fontAsset != null) text.font = fontAsset;
            text.fontStyle = fontStyle;
            text.fontSize = fontSize;
            text.lineSpacing = lineSpacing;
            text.characterSpacing = characterSpacing;
        }

        public string GetLineSpacingDisplay()
        {
            return lineSpacing == 0f ? "默认" : lineSpacing.ToString("F1");
        }

        /// <summary>
        /// 将 FontStyles 的 Bold/Italic 标志映射为 Unity 内置 FontStyle，用于 Editor UIElements 预览
        /// </summary>
        public FontStyle GetPreviewFontStyle()
        {
            bool bold = ((int)fontStyle & 1) != 0;
            bool italic = ((int)fontStyle & 2) != 0;
            if (bold && italic) return UnityEngine.FontStyle.BoldAndItalic;
            if (bold) return UnityEngine.FontStyle.Bold;
            if (italic) return UnityEngine.FontStyle.Italic;
            return UnityEngine.FontStyle.Normal;
        }
    }
}
#endif
