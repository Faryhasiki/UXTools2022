namespace UIAnimation
{
    /// <summary>
    /// 可被触发的动画目标接口。
    /// 实现此接口的组件声明自己可以接收外部触发并播放指定名称的动画，
    /// 使触发源可以通过 Inspector 配置来驱动动画播放，无需代码耦合。
    /// </summary>
    public interface IAnimationTriggerable
    {
        /// <summary>
        /// 获取该组件支持的所有动画名称（供 Inspector 下拉选择和动画师查阅）
        /// </summary>
        string[] GetAnimationNames();

        /// <summary>
        /// 播放指定名称的动画
        /// </summary>
        void PlayAnimation(string animationName);
    }
}
