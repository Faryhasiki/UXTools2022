namespace UITool
{
    /// <summary>
    /// 颜色预设绑定接口。
    /// 所有支持绑定颜色预设的组件（UXText、UXImage、UXColorBinding）都实现此接口，
    /// 使拖拽处理器和编辑器工具可以通过统一接口操作，而无需关心具体组件类型。
    /// </summary>
    public interface IColorPresetTarget
    {
        /// <summary>
        /// 颜色预设库资产引用
        /// </summary>
        ColorPresetAsset ColorPresetAsset { get; set; }

        /// <summary>
        /// 当前绑定的颜色预设 ID
        /// </summary>
        string ColorPresetId { get; set; }

        /// <summary>
        /// 将当前绑定的颜色预设应用到自身
        /// </summary>
        void ApplyColorPreset();

        /// <summary>
        /// 获取当前绑定的颜色预设名称（用于显示）
        /// </summary>
        string GetColorPresetName();
    }

    /// <summary>
    /// IColorPresetTarget 通用扩展方法。
    /// 所有实现了 IColorPresetTarget 的组件自动获得这些方法，无需各自重复实现。
    /// </summary>
    public static class ColorPresetTargetExtensions
    {
        /// <summary>
        /// 通过 IUXColorKey 切换颜色预设（类型安全，推荐程序侧使用）。
        /// 接受代码生成常量、外部枚举扩展、或 ColorPresetEntry 自身。
        /// </summary>
        public static void SetColor(this IColorPresetTarget target, IUXColorKey key)
        {
            if (target == null || key == null) return;
            target.ColorPresetId = key.PresetId;
        }

        /// <summary>
        /// 通过预设名称切换颜色预设（供配置表按名称引用时使用）
        /// </summary>
        public static void SetColorByName(this IColorPresetTarget target, string presetName)
        {
            if (target == null || target.ColorPresetAsset == null || string.IsNullOrEmpty(presetName)) return;
            var entry = target.ColorPresetAsset.FindByName(presetName);
            if (entry != null)
                target.ColorPresetId = entry.id;
        }
    }
}
