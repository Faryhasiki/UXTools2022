#if TMP_PRESENT
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UITool
{
    public class TextPresetAsset : ScriptableObject
    {
        public List<string> categories = new List<string>();
        public List<TextPresetEntry> presets = new List<TextPresetEntry>();

        [NonSerialized]
        private Dictionary<string, TextPresetEntry> cache;

        #region 查找

        public TextPresetEntry FindById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (cache == null) RebuildCache();
            cache.TryGetValue(id, out var entry);
            return entry;
        }

        public void RebuildCache()
        {
            cache = new Dictionary<string, TextPresetEntry>(presets.Count);
            foreach (var p in presets)
            {
                if (!string.IsNullOrEmpty(p.id))
                    cache[p.id] = p;
            }
        }

        public string[] GetPresetNames()
        {
            var names = new string[presets.Count];
            for (int i = 0; i < presets.Count; i++)
                names[i] = presets[i].presetName;
            return names;
        }

        public string[] GetPresetIds()
        {
            var ids = new string[presets.Count];
            for (int i = 0; i < presets.Count; i++)
                ids[i] = presets[i].id;
            return ids;
        }

        public List<TextPresetEntry> GetFiltered(string category)
        {
            if (string.IsNullOrEmpty(category) || category == "全部")
                return new List<TextPresetEntry>(presets);
            return presets.FindAll(p => p.category == category);
        }

        #endregion

        #region 编辑

        public TextPresetEntry AddPreset(string category)
        {
            var entry = new TextPresetEntry
            {
                id = Guid.NewGuid().ToString(),
                category = string.IsNullOrEmpty(category) || category == "全部" ? "" : category,
                presetName = "新文字样式",
                fontStyle = TMPro.FontStyles.Normal,
                fontSize = 24,
                lineSpacing = 0f,
                characterSpacing = 0f
            };
            presets.Insert(0, entry);
            InvalidateCache();
            return entry;
        }

        public void RemovePreset(string id)
        {
            presets.RemoveAll(p => p.id == id);
            InvalidateCache();
        }

        public void AddCategory(string catName)
        {
            if (!string.IsNullOrEmpty(catName) && !categories.Contains(catName))
                categories.Add(catName);
        }

        public void RemoveCategory(string catName)
        {
            categories.Remove(catName);
            presets.RemoveAll(p => p.category == catName);
            InvalidateCache();
        }

        private void InvalidateCache()
        {
            cache = null;
        }

        #endregion

#if UNITY_EDITOR
        #region 资产追踪注册表

        [SerializeField, HideInInspector]
        private List<string> trackedAssetGUIDs = new List<string>();

        public System.Collections.Generic.IReadOnlyList<string> TrackedAssetGUIDs => trackedAssetGUIDs;

        public void RegisterAssetGUID(string guid)
        {
            if (!string.IsNullOrEmpty(guid) && !trackedAssetGUIDs.Contains(guid))
            {
                trackedAssetGUIDs.Add(guid);
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }

        public void UnregisterAssetGUID(string guid)
        {
            if (trackedAssetGUIDs.Remove(guid))
                UnityEditor.EditorUtility.SetDirty(this);
        }

        public void ClearTrackedGUIDs()
        {
            trackedAssetGUIDs.Clear();
            UnityEditor.EditorUtility.SetDirty(this);
        }

        public int TrackedCount => trackedAssetGUIDs.Count;

        #endregion
#endif
    }
}
#endif
