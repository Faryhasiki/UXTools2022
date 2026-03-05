namespace UITool
{
    /// <summary>
    /// 颜色预设键接口。
    /// 任何能标识一个颜色预设的对象都可以实现此接口，用于类型安全的颜色预设切换。
    /// 
    /// 使用场景：
    /// 1. 代码生成的预设常量（UXColorDef.主色调）
    /// 2. 外部枚举扩展（ElementType.PrimaryButton.GetColor()）
    /// 3. Luban 等配置系统的桥接
    /// 4. ColorPresetEntry 自身
    /// 
    /// 示例：
    /// <code>
    /// // 外部业务枚举桥接
    /// public enum ElementType { PrimaryButton, SecondaryButton }
    /// 
    /// public static class ElementTypeExtensions
    /// {
    ///     public static IUXColorKey GetColor(this ElementType type) => type switch
    ///     {
    ///         ElementType.PrimaryButton => UXColorDef.主按钮色,
    ///         ElementType.SecondaryButton => UXColorDef.次按钮色,
    ///         _ => null
    ///     };
    /// }
    /// 
    /// // 使用
    /// _uxText.SetColor(ElementType.PrimaryButton.GetColor());
    /// </code>
    /// </summary>
    public interface IUXColorKey
    {
        /// <summary>
        /// 颜色预设的唯一 ID（对应 ColorPresetEntry.id）
        /// </summary>
        string PresetId { get; }
    }
}
