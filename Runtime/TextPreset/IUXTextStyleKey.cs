#if TMP_PRESENT
namespace UITool
{
    /// <summary>
    /// 文字预设键接口。
    /// 任何能标识一个文字预设的对象都可以实现此接口，用于类型安全的文字预设切换。
    /// </summary>
    public interface IUXTextStyleKey
    {
        /// <summary>
        /// 文字预设的唯一 ID（对应 TextPresetEntry.id）
        /// </summary>
        string PresetId { get; }
    }

    /// <summary>
    /// UXText 文字预设切换扩展方法
    /// </summary>
    public static class UXTextStyleExtensions
    {
        /// <summary>
        /// 通过 IUXTextStyleKey 切换文字预设（类型安全，推荐运行时使用）
        /// </summary>
        public static void SetTextStyle(this UXText target, IUXTextStyleKey key)
        {
            if (target == null || key == null) return;
            target.PresetId = key.PresetId;
        }

        /// <summary>
        /// 通过预设名称切换文字预设（供配置表按名称引用时使用）
        /// </summary>
        public static void SetTextStyleByName(this UXText target, string presetName)
        {
            if (target == null || target.PresetAsset == null || string.IsNullOrEmpty(presetName)) return;
            var entry = target.PresetAsset.presets.Find(p => p.presetName == presetName);
            if (entry != null)
                target.PresetId = entry.id;
        }
    }
}
#endif
