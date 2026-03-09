#if UNITY_EDITOR
using System;
using UnityEngine;

namespace UITool
{
    [Serializable]
    public class UXToolCommonData
    {
        public int MaxRecentSelectedFiles = 15;
        public int MaxRecentOpenedPrefabs = 15;
        /// <summary>
        /// 自定义文档 URL。非空时点击「文档」菜单将打开此链接；
        /// 为空时打开包内 Documentation~/UXTools-用户手册.md。
        /// </summary>
        public string DocumentationUrl = "";

        public void Save()
        {
            JsonAssetManager.SaveAssets(this);
            RecentSelectRecord.UpdateRecentFiles();
#if UNITY_EDITOR_WIN
            if (PrefabRecentWindow.GetInstance() != null)
            {
                PrefabRecentWindow.GetInstance().RefreshWindow();
            }
#endif
        }
    }
}
#endif
