using System;
using UnityEngine;

namespace UIAnimation
{
    /// <summary>
    /// 动画触发绑定数据。
    /// 描述一条"当触发源的某个触发点触发时，在目标组件上播放指定动画"的绑定关系。
    /// 由 IAnimationTriggerSource 的实现者通过 [SerializeField] 序列化到 Inspector 中。
    /// </summary>
    [Serializable]
    public class AnimationTriggerBinding
    {
        /// <summary>
        /// 触发时机键值（对应 IAnimationTriggerSource.GetTriggerPointNames() 中的值）。
        /// 对于 Code-First 模式通常是方法名；对于 Authoring-First 模式通常是稳定 Id。
        /// </summary>
        [SerializeField] private string _triggerPointName;

        /// <summary>
        /// 可被触发的目标组件（必须实现 IAnimationTriggerable）
        /// </summary>
        [SerializeField] private Component _target;

        /// <summary>
        /// 要在目标上播放的动画名称（对应 IAnimationTriggerable.GetAnimationNames() 中的值）
        /// </summary>
        [SerializeField] private string _animationName;

        /// <summary>
        /// 触发延迟（秒），0 表示立即触发
        /// </summary>
        [SerializeField] private float _delay;

        public string TriggerPointName
        {
            get => _triggerPointName;
            set => _triggerPointName = value;
        }

        public Component Target
        {
            get => _target;
            set => _target = value;
        }

        public string AnimationName
        {
            get => _animationName;
            set => _animationName = value;
        }

        public float Delay
        {
            get => _delay;
            set => _delay = Mathf.Max(0f, value);
        }
    }
}
