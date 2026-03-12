using System;
using System.Collections.Generic;
using System.Reflection;

namespace UIAnimation
{
    /// <summary>
    /// 将方法标记为动画触发点，声明其在 Inspector 中的显示标签。
    ///
    /// 设计原则：
    ///   · Binding 存储的键 = 方法名（nameof 结果），运行时直接字符串比较，零开销
    ///   · [TriggerPoint("标签")] 仅用于 Inspector 下拉的显示文字，不参与运行时匹配
    ///
    /// 用法：
    ///   [TriggerPoint("点击时")]
    ///   private void OnClickTrigger() => this.FireTrigger(nameof(OnClickTrigger));
    ///
    ///   string[] IAnimationTriggerSource.GetTriggerPointNames()
    ///       => TriggerPointAttribute.GetNames(GetType());
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class TriggerPointAttribute : Attribute
    {
        /// <summary>触发点在 Inspector 中的显示标签（仅用于 UI，不作为运行时匹配键）</summary>
        public string Label { get; }

        public TriggerPointAttribute(string label) => Label = label;

        // ── 反射缓存（Type → methodName → displayLabel）──────────────
        // 每个 Type 只扫描一次；Unity Editor 重新编译时静态字段自动清空
        private static readonly Dictionary<Type, Dictionary<string, string>> s_cache = new();

        /// <summary>
        /// 扫描 <paramref name="type"/> 及其父类上所有标注了 [TriggerPoint] 的方法，
        /// 返回方法名数组作为触发点键值，供 GetTriggerPointNames() 直接返回。
        /// <para>
        /// 运行时的 Binding 存储的就是这些方法名，
        /// FireTrigger(nameof(method)) 传入相同的方法名，直接命中，无需任何查找。
        /// </para>
        /// </summary>
        public static string[] GetNames(Type type)
        {
            var map = BuildMap(type);
            var names = new string[map.Count];
            var i = 0;
            foreach (var key in map.Keys)
                names[i++] = key;
            return names;
        }

        /// <summary>
        /// 根据方法名获取 Inspector 显示标签。
        /// 若该方法标注了 [TriggerPoint]，返回其 Label；否则原样返回方法名。
        /// 仅供 Editor Drawer 使用，运行时不需要调用。
        /// </summary>
        public static string GetLabel(Type type, string methodName)
        {
            var map = BuildMap(type);
            return map.TryGetValue(methodName, out var label) ? label : methodName;
        }

        // ── 内部：构建并缓存 methodName → displayLabel 映射 ──────────

        private static Dictionary<string, string> BuildMap(Type type)
        {
            if (s_cache.TryGetValue(type, out var cached))
                return cached;

            var map = new Dictionary<string, string>();
            CollectFromType(type, map);
            s_cache[type] = map;
            return map;
        }

        private static void CollectFromType(Type type, Dictionary<string, string> map)
        {
            if (type == null || type == typeof(object))
                return;

            // 先递归父类，子类同名方法可覆盖父类标签
            CollectFromType(type.BaseType, map);

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static
                                     | BindingFlags.Public   | BindingFlags.NonPublic
                                     | BindingFlags.DeclaredOnly;

            foreach (var method in type.GetMethods(flags))
            {
                var attr = method.GetCustomAttribute<TriggerPointAttribute>();
                if (attr != null)
                    map[method.Name] = attr.Label;
            }
        }
    }
}
