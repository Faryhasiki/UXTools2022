#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThunderFireUITool
{
    [Serializable]
    public class ColorPresetItem
    {
        public string id = "";
        public string name = "";
        public string description = "";
        public string category = "";
        public string hex = "FFFFFF";
        public int opacity = 100;

        public Color GetColor()
        {
            if (ColorUtility.TryParseHtmlString("#" + hex, out Color c))
            {
                c.a = opacity / 100f;
                return c;
            }
            return Color.white;
        }

        public void SetColor(Color c)
        {
            hex = ColorUtility.ToHtmlStringRGB(c);
            opacity = Mathf.RoundToInt(c.a * 100);
        }
    }

    [Serializable]
    public class ColorPresetLibrary
    {
        public List<string> categories = new List<string>();
        public List<ColorPresetItem> presets = new List<ColorPresetItem>();

        public void Save()
        {
            JsonAssetManager.SaveAssets(this);
        }

        public ColorPresetItem AddPreset(string category)
        {
            var item = new ColorPresetItem
            {
                id = Guid.NewGuid().ToString(),
                category = string.IsNullOrEmpty(category) || category == "全部" ? "" : category,
                name = "新颜色",
                hex = "FFFFFF",
                opacity = 100
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

        public List<ColorPresetItem> GetFiltered(string category)
        {
            if (string.IsNullOrEmpty(category) || category == "全部")
                return new List<ColorPresetItem>(presets);
            return presets.FindAll(p => p.category == category);
        }
    }
}
#endif
