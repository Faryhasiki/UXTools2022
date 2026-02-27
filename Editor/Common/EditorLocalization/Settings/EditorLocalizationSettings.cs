using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ThunderFireUITool
{
    //增加支持的语言
    public enum EditorLocalName
    {
        Chinese,
        TraditionalChinese,
        English,
        Japanese,
        Korean,
    }
    //关于编辑器本地化的配置
    [Serializable]
    public class EditorLocalizationSettings
    {
#if UXTOOLS_DEV
        [MenuItem(ThunderFireUIToolConfig.Menu_CreateAssets + "/" + ThunderFireUIToolConfig.Menu_UXToolLocalization + "/EditorLocalizationSettings", false, -97)]
#endif
        public static void Create()
        {
            EditorLocalizationSettings settings = JsonAssetManager.CreateAssets<EditorLocalizationSettings>(EditorLocalizationConfig.LocalizationSettingsFullPath);
            settings.LocalType = EditorLocalName.Chinese;
            JsonAssetManager.SaveAssets<EditorLocalizationSettings>(settings);
        }

        public EditorLocalName LocalType;

        public void ChangeLocalValue(EditorLocalName Type) { }
    }
}
