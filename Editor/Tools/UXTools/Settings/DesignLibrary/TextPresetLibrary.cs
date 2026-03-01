#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThunderFireUITool
{
    [Serializable]
    public class TextPresetItem
    {
        public string id = "";
        public string name = "";
        public string description = "";
        public string category = "";
        public string fontFamily = "";
        public string fontWeight = "Regular";
        public int fontSize = 24;
        public float lineHeight = -1f;
        public float letterSpacing = 0f;

        public string GetLineHeightDisplay()
        {
            return lineHeight < 0 ? "Auto" : lineHeight.ToString("F0");
        }
    }

    [Serializable]
    public class TextPresetLibrary
    {
        public List<string> categories = new List<string>();
        public List<TextPresetItem> presets = new List<TextPresetItem>();

        public void Save()
        {
            JsonAssetManager.SaveAssets(this);
        }

        public TextPresetItem AddPreset(string category)
        {
            var item = new TextPresetItem
            {
                id = Guid.NewGuid().ToString(),
                category = string.IsNullOrEmpty(category) || category == "全部" ? "" : category,
                name = "新文字样式",
                fontFamily = "Arial",
                fontWeight = "Regular",
                fontSize = 24,
                lineHeight = -1f,
                letterSpacing = 0f
            };
            presets.Insert(0, item);
            Save();
            return item;
        }

        public void RemovePreset(string id)
        {
            presets.RemoveAll(p => p.id == id);
            Save();
        }

        public void AddCategory(string catName)
        {
            if (!string.IsNullOrEmpty(catName) && !categories.Contains(catName))
            {
                categories.Add(catName);
                Save();
            }
        }

        public void RemoveCategory(string catName)
        {
            categories.Remove(catName);
            presets.RemoveAll(p => p.category == catName);
            Save();
        }

        public List<TextPresetItem> GetFiltered(string category)
        {
            if (string.IsNullOrEmpty(category) || category == "全部")
                return new List<TextPresetItem>(presets);
            return presets.FindAll(p => p.category == category);
        }
    }
}
#endif
