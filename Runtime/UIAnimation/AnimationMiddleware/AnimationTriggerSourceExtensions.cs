using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UIAnimation
{
    /// <summary>
    /// IAnimationTriggerSource 通用扩展方法。
    /// 所有实现了 IAnimationTriggerSource 的组件自动获得这些方法，无需各自重复实现触发逻辑。
    ///
    /// 调用方式：
    ///   Code-First:      this.FireTrigger(nameof(OnClickTrigger));
    ///   Authoring-First: authoring.FireById("stable-slot-id");
    ///
    /// FireTrigger 只做键值匹配，不关心该键值代表方法名还是稳定 Id。
    /// 对于带延迟的绑定，同一触发源上的同一条绑定若被重复触发，
    /// 新请求会取消旧的待执行延迟任务，只保留最后一次，避免协程堆积。
    /// </summary>
    public static class AnimationTriggerSourceExtensions
    {
        // 缓存 WaitForSeconds，避免频繁触发时重复分配相同延迟对象。
        private static readonly Dictionary<float, WaitForSeconds> DelayCache = new();
        // 按“触发源实例 + 绑定对象”维度记录待执行协程，新的触发会取消旧的延迟任务，避免堆积。
        private static readonly Dictionary<DelayedBindingKey, Coroutine> PendingDelayedBindings = new();

        /// <summary>
        /// 执行与 <paramref name="triggerPointName"/> 匹配的所有绑定。
        /// </summary>
        public static void FireTrigger(this IAnimationTriggerSource source, string triggerPointName)
        {
            if (source == null || source.TriggerBindings == null || string.IsNullOrEmpty(triggerPointName))
                return;

            var monoBehaviour = source as MonoBehaviour;

            foreach (var binding in source.TriggerBindings)
            {
                if (binding == null) continue;
                if (binding.TriggerPointName != triggerPointName) continue;
                if (binding.Target == null) continue;

                if (!(binding.Target is IAnimationTriggerable triggerable))
                    continue;

                var animName = binding.AnimationName;
                if (string.IsNullOrEmpty(animName)) continue;

                if (binding.Delay > 0f && monoBehaviour != null)
                    ScheduleDelayedPlay(monoBehaviour, source, binding, triggerable, animName);
                else
                    triggerable.PlayAnimation(animName);
            }
        }

        /// <summary>
        /// 执行与 <paramref name="triggerPointName"/> 匹配的所有绑定（忽略延迟，立即全部触发）。
        /// </summary>
        public static void FireTriggerImmediate(this IAnimationTriggerSource source, string triggerPointName)
        {
            if (source == null || source.TriggerBindings == null || string.IsNullOrEmpty(triggerPointName))
                return;

            foreach (var binding in source.TriggerBindings)
            {
                if (binding == null) continue;
                if (binding.TriggerPointName != triggerPointName) continue;
                if (binding.Target == null) continue;

                if (binding.Target is IAnimationTriggerable triggerable
                    && !string.IsNullOrEmpty(binding.AnimationName))
                {
                    CancelDelayedPlay(source, binding);
                    triggerable.PlayAnimation(binding.AnimationName);
                }
            }
        }

        private static void ScheduleDelayedPlay(MonoBehaviour monoBehaviour, IAnimationTriggerSource source,
            AnimationTriggerBinding binding, IAnimationTriggerable triggerable, string animationName)
        {
            var key = new DelayedBindingKey(source, binding);

            if (PendingDelayedBindings.TryGetValue(key, out var runningCoroutine) && runningCoroutine != null)
                monoBehaviour.StopCoroutine(runningCoroutine);

            PendingDelayedBindings[key] = monoBehaviour.StartCoroutine(
                DelayedPlay(key, triggerable, animationName, binding.Delay));
        }

        private static void CancelDelayedPlay(IAnimationTriggerSource source, AnimationTriggerBinding binding)
        {
            if (source is not MonoBehaviour monoBehaviour)
                return;

            var key = new DelayedBindingKey(source, binding);
            if (!PendingDelayedBindings.TryGetValue(key, out var runningCoroutine) || runningCoroutine == null)
                return;

            monoBehaviour.StopCoroutine(runningCoroutine);
            PendingDelayedBindings.Remove(key);
        }

        private static IEnumerator DelayedPlay(DelayedBindingKey key, IAnimationTriggerable triggerable,
            string animationName, float delay)
        {
            yield return GetDelayInstruction(delay);
            PendingDelayedBindings.Remove(key);

            if (triggerable is Component comp && comp != null)
                triggerable.PlayAnimation(animationName);
        }

        private static WaitForSeconds GetDelayInstruction(float delay)
        {
            if (!DelayCache.TryGetValue(delay, out var waitInstruction))
            {
                waitInstruction = new WaitForSeconds(delay);
                DelayCache[delay] = waitInstruction;
            }

            return waitInstruction;
        }

        private readonly struct DelayedBindingKey
        {
            private readonly int _sourceId;
            private readonly int _bindingId;

            public DelayedBindingKey(IAnimationTriggerSource source, AnimationTriggerBinding binding)
            {
                _sourceId = source != null ? source.GetHashCode() : 0;
                _bindingId = binding != null ? binding.GetHashCode() : 0;
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (_sourceId * 397) ^ _bindingId;
                }
            }

            public override bool Equals(object obj)
            {
                return obj is DelayedBindingKey other
                       && _sourceId == other._sourceId
                       && _bindingId == other._bindingId;
            }
        }
    }
}
