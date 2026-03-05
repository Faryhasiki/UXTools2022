namespace UITool
{
    /// <summary>
    /// 颜色预设键的轻量值类型实现，主要供代码生成器使用。
    /// 包含预设 ID 和可读名称，实现 IUXColorKey 接口。
    /// </summary>
    public readonly struct UXColorKey : IUXColorKey
    {
        /// <summary>
        /// 颜色预设的唯一 ID
        /// </summary>
        public string PresetId { get; }

        /// <summary>
        /// 颜色预设的可读名称（用于调试和日志）
        /// </summary>
        public string Name { get; }

        public UXColorKey(string presetId, string name)
        {
            PresetId = presetId;
            Name = name;
        }

        public override string ToString() => $"UXColorKey({Name}, {PresetId})";
    }
}
