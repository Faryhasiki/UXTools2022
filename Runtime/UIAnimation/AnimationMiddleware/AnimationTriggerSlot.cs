using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UIAnimation
{
    /// <summary>
    /// 触发时机槽位。
    /// Id 是运行时稳定键；DisplayName 给动画师使用；CodeMethodName 给程序生成代码使用。
    /// </summary>
    [Serializable]
    [MovedFrom(true, null, null, "UIAnimation.AnimationTriggerDefinition")]
    public class AnimationTriggerSlot
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private string _methodName;

        public string Id
        {
            get => _id;
            set => _id = value;
        }

        public string DisplayName
        {
            get => _displayName;
            set => _displayName = value;
        }

        public string CodeMethodName
        {
            get => _methodName;
            set => _methodName = value;
        }
    }
}
