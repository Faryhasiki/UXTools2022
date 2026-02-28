#if UNITY_EDITOR
using UnityEditor;

namespace ThunderFireUITool
{
    [UXInitialize(100)]
    public class PrefabOpenRecord
    {
        static PrefabOpenRecord()
        {
            PrefabStageUtils.AddOpenedEvent((p) =>
            {
                var prefabStage = PrefabStageUtils.GetCurrentPrefabStage();
                string guid = AssetDatabase.AssetPathToGUID(prefabStage.GetAssetPath());
                if (SwitchSetting.CheckValid(SwitchSetting.SwitchType.RecentlyOpened))
                {
                    var RecentOpened = JsonAssetManager.GetAssets<PrefabOpenedSetting>();
                    var RecentList = RecentOpened.List;
                    if (!RecentList.Contains(guid))
                    {
                        RecentOpened.Add(guid);
                    }
                    else
                    {
                        RecentOpened.ResortLast(guid);
                    }
                }
                if (!SwitchSetting.CheckValid(SwitchSetting.SwitchType.PrefabMultiOpen)) return;
                EditorApplication.delayCall += () =>
                {
                    PrefabTabs.OpenTab(guid, false);
                    //SceneViewToolBar.BringToFront();
                };
            });
            PrefabStageUtils.AddClosingEvent((p) =>
            {
                if (!SwitchSetting.CheckValid(SwitchSetting.SwitchType.PrefabMultiOpen)) return;
#if UNITY_2020_1_OR_NEWER
                var prefabStage = PrefabStageUtils.GetCurrentPrefabStage();
                if (prefabStage == null)
                {
                    PrefabTabs.CloseTab("", false);
                }
#else
                PrefabTabs.CloseTab("", false);
#endif
            });
        }
    }
}
#endif