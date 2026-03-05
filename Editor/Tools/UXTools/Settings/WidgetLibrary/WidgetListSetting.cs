#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UITool
{
    //设为组件的Prefab列表
    [Serializable]
    public class WidgetListSetting
    {
        public List<string> List = new List<string>();
        private static int previewSize = 144;
        [MenuItem(UIToolConfig.Menu_CreateAssets + "/" + UIToolConfig.WidgetLibrary + "/WidgetListSettings", false, -50)]
        public static void Create()
        {

            var setting = JsonAssetManager.CreateAssets<WidgetListSetting>(UIToolConfig.WidgetListPath);
            string prefabDir = UIToolConfig.AssetsRootPath + "UX-GUI-PresetWidget/UXToolPrefabs/";
            if (Directory.Exists(prefabDir))
            {
                var guids = AssetDatabase.FindAssets("t:Prefab", new string[] { prefabDir });
                foreach (var guid in guids)
                {
                    if (!setting.List.Contains(guid))
                        setting.List.Add(guid);
                }
            }
            JsonAssetManager.SaveAssets(setting);
        }

        public void Add(string newLabel)
        {
            List.Add(newLabel);
            JsonAssetManager.SaveAssets(this);
            OnValueChanged();
        }

        public void Remove(string label)
        {
            var index = List.FindIndex(i => i == label); // like Where/Single
            if (index >= 0)
            {   // ensure item found
                List.RemoveAt(index);
            }
            //List.Remove(label);
            JsonAssetManager.SaveAssets(this);
            OnValueChanged();
        }

        public void ResortLast(string label)
        {
            var index = List.FindIndex(i => i == label);
            if (index >= 0)
            {   // ensure item found
                List.RemoveAt(index);
            }
            List.Add(label);
            JsonAssetManager.SaveAssets(this);
            Utils.UpdatePreviewTexture(label, previewSize);
            OnValueChanged();
        }

        private void OnValueChanged()
        {
            if (WidgetRepositoryWindow.GetInstance() != null)
            {
                WidgetRepositoryWindow.GetInstance().RefreshWindow();
            }
        }
    }
}
#endif