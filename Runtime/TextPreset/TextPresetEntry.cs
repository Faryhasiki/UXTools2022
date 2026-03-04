#if TMP_PRESENT
using System;
using TMPro;
using UnityEngine;

namespace UITool
{
    [Serializable]
    public class TextPresetEntry
    {
        public string id;
        public string presetName;
        public string description;
        public string category;
        public TMP_FontAsset fontAsset;
        public FontStyles fontStyle;
        public int fontSize = 24;
        public float lineSpacing;
        public float characterSpacing;

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
