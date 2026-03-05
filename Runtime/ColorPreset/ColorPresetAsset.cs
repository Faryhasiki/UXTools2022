using System;
using System.Collections.Generic;
using UnityEngine;

namespace UITool
{
    /// <summary>
    /// 颜色预设库 ScriptableObject，运行时可用。
    /// 管理所有颜色预设条目和分类，提供查找、过滤、增删等操作。
    /// </summary>
    public class ColorPresetAsset : ScriptableObject
    {
        /// <summary>
        /// 所有分类名称列表
        /// </summary>
        public List<string> categories = new List<string>();

        /// <summary>
        /// 所有颜色预设条目
        /// </summary>
        public List<ColorPresetEntry> presets = new List<ColorPresetEntry>();

        [NonSerialized]
        private Dictionary<string, ColorPresetEntry> _cache;

        [NonSerialized]
        private Dictionary<string, ColorPresetEntry> _nameCache;

        #region 查找

        /// <summary>
        /// 通过 ID 查找颜色预设，内部使用字典缓存加速
        /// </summary>
        public ColorPresetEntry FindById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (_cache == null) RebuildCache();
            _cache.TryGetValue(id, out var entry);
            return entry;
        }

        /// <summary>
        /// 通过预设名称查找颜色预设（供配置表按名称引用时使用）。
        /// 名称不唯一时返回第一个匹配项。
        /// </summary>
        public ColorPresetEntry FindByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (_nameCache == null) RebuildCache();
            _nameCache.TryGetValue(name, out var entry);
            return entry;
        }

        /// <summary>
        /// 重建查找缓存（ID 缓存和名称缓存）
        /// </summary>
        public void RebuildCache()
        {
            _cache = new Dictionary<string, ColorPresetEntry>(presets.Count);
            _nameCache = new Dictionary<string, ColorPresetEntry>(presets.Count);
            foreach (var p in presets)
            {
                if (!string.IsNullOrEmpty(p.id))
                    _cache[p.id] = p;
                if (!string.IsNullOrEmpty(p.presetName) && !_nameCache.ContainsKey(p.presetName))
                    _nameCache[p.presetName] = p;
            }
        }

        /// <summary>
        /// 获取所有预设名称数组（用于 Popup 下拉列表）
        /// </summary>
        public string[] GetPresetNames()
        {
            var names = new string[presets.Count];
            for (int i = 0; i < presets.Count; i++)
                names[i] = presets[i].presetName;
            return names;
        }

        /// <summary>
        /// 获取所有预设 ID 数组（与 GetPresetNames 索引一一对应）
        /// </summary>
        public string[] GetPresetIds()
        {
            var ids = new string[presets.Count];
            for (int i = 0; i < presets.Count; i++)
                ids[i] = presets[i].id;
            return ids;
        }

        /// <summary>
        /// 按分类过滤预设列表，"全部"或空字符串返回所有
        /// </summary>
        public List<ColorPresetEntry> GetFiltered(string category)
        {
            if (string.IsNullOrEmpty(category) || category == "全部")
                return new List<ColorPresetEntry>(presets);
            return presets.FindAll(p => p.category == category);
        }

        #endregion

        #region 编辑

        /// <summary>
        /// 在指定分类下新建一条颜色预设，插入到列表首位
        /// </summary>
        public ColorPresetEntry AddPreset(string category)
        {
            var entry = new ColorPresetEntry
            {
                id = Guid.NewGuid().ToString(),
                category = string.IsNullOrEmpty(category) || category == "全部" ? "" : category,
                presetName = "新颜色",
                hex = "FFFFFF",
                opacity = 100
            };
            presets.Insert(0, entry);
            InvalidateCache();
            return entry;
        }

        /// <summary>
        /// 按 ID 删除颜色预设
        /// </summary>
        public void RemovePreset(string id)
        {
            presets.RemoveAll(p => p.id == id);
            InvalidateCache();
        }

        /// <summary>
        /// 新增分类（不允许重复）
        /// </summary>
        public void AddCategory(string catName)
        {
            if (!string.IsNullOrEmpty(catName) && !categories.Contains(catName))
                categories.Add(catName);
        }

        /// <summary>
        /// 删除分类及其下所有预设
        /// </summary>
        public void RemoveCategory(string catName)
        {
            categories.Remove(catName);
            presets.RemoveAll(p => p.category == catName);
            InvalidateCache();
        }

        private void InvalidateCache()
        {
            _cache = null;
            _nameCache = null;
        }

        #endregion

#if UNITY_EDITOR
        #region 资产追踪注册表

        /// <summary>
        /// 追踪引用了此预设库的预制体/场景 GUID 列表
        /// </summary>
        [SerializeField, HideInInspector]
        private List<string> _trackedAssetGUIDs = new List<string>();

        /// <summary>
        /// 已追踪的资产 GUID 只读列表
        /// </summary>
        public IReadOnlyList<string> TrackedAssetGUIDs => _trackedAssetGUIDs;

        /// <summary>
        /// 已追踪资产数量
        /// </summary>
        public int TrackedCount => _trackedAssetGUIDs.Count;

        /// <summary>
        /// 注册一个资产 GUID 到追踪列表
        /// </summary>
        public void RegisterAssetGUID(string guid)
        {
            if (!string.IsNullOrEmpty(guid) && !_trackedAssetGUIDs.Contains(guid))
            {
                _trackedAssetGUIDs.Add(guid);
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }

        /// <summary>
        /// 从追踪列表移除指定 GUID
        /// </summary>
        public void UnregisterAssetGUID(string guid)
        {
            if (_trackedAssetGUIDs.Remove(guid))
                UnityEditor.EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// 清空所有追踪 GUID
        /// </summary>
        public void ClearTrackedGUIDs()
        {
            _trackedAssetGUIDs.Clear();
            UnityEditor.EditorUtility.SetDirty(this);
        }

        #endregion
#endif
    }
}
