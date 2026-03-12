#if UNITY_EDITOR
using System.Collections.Generic;

namespace UIAnimation
{
    /// <summary>
    /// AnimationTriggerAuthoring 的基础校验工具。
    /// 用于给动画师 Inspector 提示明显的配置问题。
    /// </summary>
    public static class AnimationTriggerValidationUtility
    {
        public static List<string> Validate(AnimationTriggerAuthoring authoring)
        {
            var issues = new List<string>();
            if (authoring == null)
                return issues;

            var displayNames = new HashSet<string>();

            foreach (var slot in authoring.TriggerSlots)
            {
                if (slot == null)
                    continue;

                if (string.IsNullOrWhiteSpace(slot.DisplayName))
                {
                    issues.Add("存在未填写显示名的触发时机。");
                    continue;
                }

                if (!displayNames.Add(slot.DisplayName))
                    issues.Add("存在重复的触发时机显示名，建议区分。");
            }

            var source = authoring as IAnimationTriggerSource;
            var validTriggerIds = new HashSet<string>(source?.GetTriggerPointNames() ?? System.Array.Empty<string>());

            foreach (var binding in source?.TriggerBindings ?? new List<AnimationTriggerBinding>())
            {
                if (binding == null)
                    continue;

                if (string.IsNullOrWhiteSpace(binding.TriggerPointName))
                    issues.Add("存在未指定触发时机的绑定。");
                else if (!validTriggerIds.Contains(binding.TriggerPointName))
                    issues.Add("存在绑定引用了无效的触发时机。");

                if (binding.Target == null)
                    issues.Add("存在未指定目标组件的绑定。");
                else if (binding.Target is not IAnimationTriggerable)
                    issues.Add("存在目标组件未实现 IAnimationTriggerable 的绑定。");

                if (string.IsNullOrWhiteSpace(binding.AnimationName))
                    issues.Add("存在未指定动画名称的绑定。");
            }

            return RemoveDuplicateIssues(issues);
        }

        private static List<string> RemoveDuplicateIssues(List<string> issues)
        {
            var results = new List<string>();
            var seen = new HashSet<string>();

            foreach (var issue in issues)
            {
                if (seen.Add(issue))
                    results.Add(issue);
            }

            return results;
        }
    }
}
#endif
