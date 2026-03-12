namespace UIAnimation
{
    /// <summary>
    /// 可选接口：为触发点键值提供 Inspector 显示标签。
    /// 运行时匹配仍使用键值本身，标签仅用于编辑器展示。
    /// </summary>
    public interface ITriggerPointLabelProvider
    {
        /// <summary>
        /// 根据触发点键值返回 Inspector 展示标签。
        /// 若返回空，则编辑器回退为直接显示键值。
        /// </summary>
        string GetTriggerPointLabel(string triggerPointName);
    }
}
