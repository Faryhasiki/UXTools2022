#if UNITY_EDITOR
using System;
using UnityEngine;

namespace ThunderFireUITool
{
    [Serializable]
    public class UXToolCommonData
    {
        public int MaxRecentSelectedFiles = 15;
        public int MaxRecentOpenedPrefabs = 15;

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
