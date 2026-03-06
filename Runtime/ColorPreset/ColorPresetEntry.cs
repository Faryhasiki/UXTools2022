using System;
using UnityEngine;
using UnityEngine.UI;

namespace UITool
{
    /// <summary>
    /// 单条颜色预设数据，包含颜色的十六进制值和不透明度。
    /// 实现 IUXColorKey 接口，可直接作为颜色预设键传递给组件。
    /// 可通过 ApplyTo 将预设颜色应用到任意 Graphic 组件。
    /// </summary>
    [Serializable]
    public class ColorPresetEntry : IUXColorKey
    {
        /// <summary>
        /// 预设唯一标识符
        /// </summary>
        public string id;

        /// <summary>
        /// IUXColorKey 实现，返回预设 ID
        /// </summary>
        string IUXColorKey.PresetId => id;

        /// <summary>
        /// 预设名称（用于显示和检索）
        /// </summary>
        public string presetName;

        /// <summary>
        /// 预设描述
        /// </summary>
        public string description;

        /// <summary>
        /// 所属分类
        /// </summary>
        public string category;

        /// <summary>
        /// 颜色十六进制值（不含 # 号，如 "FF5500"）
        /// </summary>
        public string hex = "FFFFFF";

        /// <summary>
        /// 不透明度（0~100 整数，100 表示完全不透明）
        /// </summary>
        public int opacity = 100;

        /// <summary>
        /// 是否为此预设生成代码常量（默认 false）
        /// </summary>
        public bool generateCode = false;

        /// <summary>
        /// 代码生成别名，用于生成更符合程序命名风格的常量名。
        /// 非空时优先使用别名作为生成的字段名，否则使用 presetName。
        /// </summary>
        public string codeAlias = "";

        /// <summary>
        /// 将 hex + opacity 转换为 Unity Color
        /// </summary>
        public Color GetColor()
        {
            if (ColorUtility.TryParseHtmlString("#" + hex, out Color c))
            {
                c.a = opacity / 100f;
                return c;
            }
            return Color.white;
        }

        /// <summary>
        /// 从 Unity Color 写回 hex 和 opacity
        /// </summary>
        public void SetColor(Color c)
        {
            hex = ColorUtility.ToHtmlStringRGB(c);
            opacity = Mathf.RoundToInt(c.a * 100);
        }

        /// <summary>
        /// 将预设颜色应用到任意 Graphic 组件（Image、Text、RawImage 等）
        /// </summary>
        public void ApplyTo(Graphic graphic)
        {
            if (graphic == null) return;
            graphic.color = GetColor();
        }

        /// <summary>
        /// 获取用于显示的不透明度文本
        /// </summary>
        public string GetOpacityDisplay()
        {
            return opacity + " %";
        }
    }
}
