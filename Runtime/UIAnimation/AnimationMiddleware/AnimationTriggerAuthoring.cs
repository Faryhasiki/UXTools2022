using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace UIAnimation
{
    /// <summary>
    /// 动画触发作者组件。
    /// 供动画师预先定义触发时机、配置动画绑定，并作为程序生成代码时的稳定数据源。
    /// Authoring 组件长期挂在 prefab 上，程序侧脚本通过组合引用它，而不是继承它。
    /// </summary>
    [DisallowMultipleComponent]
    public class AnimationTriggerAuthoring : MonoBehaviour, IAnimationTriggerSource, ITriggerPointLabelProvider
    {
        [FormerlySerializedAs("_triggerDefinitions")]
        [SerializeField] private List<AnimationTriggerSlot> _triggerSlots = new();
        [SerializeField] private List<AnimationTriggerBinding> _triggerBindings = new();

        [Header("代码生成")]
        [SerializeField] private string _generatedNamespace = "Game.UI";
        [SerializeField] private string _generatedClassName = "NewAnimationTrigger";
        [SerializeField] private string _generatedBaseTypeName = "UnityEngine.MonoBehaviour";

        public IReadOnlyList<AnimationTriggerSlot> TriggerSlots => _triggerSlots;

        List<AnimationTriggerBinding> IAnimationTriggerSource.TriggerBindings => _triggerBindings;

        string[] IAnimationTriggerSource.GetTriggerPointNames()
        {
            if (_triggerSlots == null || _triggerSlots.Count == 0)
                return Array.Empty<string>();

            var results = new List<string>(_triggerSlots.Count);
            var usedIds = new HashSet<string>();

            foreach (var slot in _triggerSlots)
            {
                if (slot == null || string.IsNullOrWhiteSpace(slot.Id))
                    continue;

                if (usedIds.Add(slot.Id))
                    results.Add(slot.Id);
            }

            return results.ToArray();
        }

        string ITriggerPointLabelProvider.GetTriggerPointLabel(string triggerPointName)
        {
            var slot = FindSlotById(triggerPointName);
            if (slot == null || string.IsNullOrWhiteSpace(slot.DisplayName))
                return triggerPointName;

            return slot.DisplayName;
        }

        public void FireById(string triggerId)
        {
            this.FireTrigger(triggerId);
        }

        public void EnsureSlotIds()
        {
            if (_triggerSlots == null)
                return;

            var usedIds = new HashSet<string>();

            foreach (var slot in _triggerSlots)
            {
                if (slot == null)
                    continue;

                if (string.IsNullOrWhiteSpace(slot.Id))
                {
                    slot.Id = string.IsNullOrWhiteSpace(slot.CodeMethodName)
                        ? Guid.NewGuid().ToString("N")
                        : slot.CodeMethodName;
                }

                while (!usedIds.Add(slot.Id))
                    slot.Id = Guid.NewGuid().ToString("N");
            }
        }

        public AnimationTriggerSlot FindSlotById(string triggerId)
        {
            if (_triggerSlots == null || string.IsNullOrWhiteSpace(triggerId))
                return null;

            foreach (var slot in _triggerSlots)
            {
                if (slot != null && slot.Id == triggerId)
                    return slot;
            }

            return null;
        }

        public string GeneratedNamespace
        {
            get => _generatedNamespace;
            set => _generatedNamespace = value;
        }

        public string GeneratedClassName
        {
            get => _generatedClassName;
            set => _generatedClassName = value;
        }

        public string GeneratedBaseTypeName
        {
            get => _generatedBaseTypeName;
            set => _generatedBaseTypeName = value;
        }

        private void OnValidate()
        {
            EnsureSlotIds();
        }
    }
}
