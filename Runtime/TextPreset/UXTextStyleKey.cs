#if TMP_PRESENT
namespace UITool
{
    /// <summary>
    /// 文字预设键的轻量值类型实现，主要供代码生成器使用。
    /// </summary>
    public readonly struct UXTextStyleKey : IUXTextStyleKey
    {
        /// <summary>
        /// 文字预设的唯一 ID
        /// </summary>
        public string PresetId { get; }

        /// <summary>
        /// 文字预设的可读名称（用于调试和日志）
        /// </summary>
        public string Name { get; }

        public UXTextStyleKey(string presetId, string name)
        {
            PresetId = presetId;
            Name = name;
        }

        public override string ToString() => $"UXTextStyleKey({Name}, {PresetId})";
    }
}
#endif
