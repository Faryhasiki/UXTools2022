#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UITool
{
    public class SwitchSetting
    {
        [SerializeField]
        private bool[] m_values;
        public enum SwitchType
        {
            RecentlyOpened,
            AlignSnap,
            RightClickList,
            QuickCopy,
            MovementShortcuts,
            PrefabMultiOpen,
            RecentlySelected = 8,
        }

        private static SwitchSetting m_instance;

        [MenuItem(UIToolConfig.Menu_CreateAssets + "/" + UIToolConfig.Setting + "/SettingData", false, -41)]
        public static void Create()
        {
            m_instance = JsonAssetManager.CreateAssets<SwitchSetting>(UIToolConfig.SwitchSettingPath);
            int count = Enum.GetValues(typeof(SwitchType)).Length;
            m_instance.m_values = new bool[count];
            for (int i = 0; i < count; i++)
            {
                m_instance.m_values[i] = true;
            }
            JsonAssetManager.SaveAssets(m_instance);

        }

        public static void ChangeSwitch(Toggle[] toggles)
        {
            m_instance = JsonAssetManager.GetAssets<SwitchSetting>();
            if (m_instance == null)
            {
                m_instance = JsonAssetManager.CreateAssets<SwitchSetting>(UIToolConfig.SwitchSettingPath);
            }
            m_instance.m_values = new bool[toggles.Length];
            for (int i = 0; i < toggles.Length; i++)
            {
                m_instance.m_values[i] = toggles[i].value;
            }
            JsonAssetManager.SaveAssets(m_instance);

            SceneViewToolBar.CloseFunction();
            SceneViewToolBar.InitFunction();
        }

        public static bool CheckValid(int x)
        {
            if (m_instance == null)
            {
                m_instance = JsonAssetManager.GetAssets<SwitchSetting>();
            }
            if (m_instance == null || m_instance.m_values == null || m_instance.m_values.Length <= x)
            {
                return true;
            }
            return m_instance.m_values[x];
        }

        public static bool CheckValid(SwitchType type)
        {
            return CheckValid((int)type);
        }
    }
}
#endif
