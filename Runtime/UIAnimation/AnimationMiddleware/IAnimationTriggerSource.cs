using System.Collections.Generic;

namespace UIAnimation
{
    /// <summary>
    /// 动画触发源接口。
    /// 实现此接口的组件声明自己可以在特定时机触发其他动画，
    /// 通过 Inspector 配置触发绑定关系，实现动画编排的可视化配置。
    /// 组件可同时实现 IAnimationTriggerable 以支持链式触发。
    ///
    /// <para>
    /// <b>Code-First 最简实现方式：</b>只需声明 TriggerBindings 属性，
    /// 然后在触发方法上标注 [TriggerPoint("标签")] 即可，无需重写 GetTriggerPointNames()。
    /// </para>
    /// <para>
    /// <b>Authoring-First 模式：</b>可像 AnimationTriggerAuthoring 一样，
    /// 自定义维护触发键值列表，并通过 ITriggerPointLabelProvider 提供给 Inspector 的显示名。
    /// </para>
    /// </summary>
    public interface IAnimationTriggerSource
    {
        /// <summary>
        /// 获取该组件提供的所有触发点键值，供 Inspector 下拉选择。
        /// <para>
        /// 默认实现：自动扫描实现类上所有标注了 [TriggerPoint] 的方法，返回其方法名数组。
        /// 若需要自定义触发点（不使用 [TriggerPoint] 属性），override 此方法并手动返回即可。
        /// </para>
        /// </summary>
        string[] GetTriggerPointNames() => TriggerPointAttribute.GetNames(GetType());

        /// <summary>
        /// 触发绑定配置列表（Inspector 中配置的绑定关系）
        /// </summary>
        List<AnimationTriggerBinding> TriggerBindings { get; }
    }
}
