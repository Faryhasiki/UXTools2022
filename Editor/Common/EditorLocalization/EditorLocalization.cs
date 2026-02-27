using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace ThunderFireUITool
{
    public class EditorLocalization
    {
        public static EditorLocalizationData currentLocalizationData;
        public static EditorLocalizationUIInspectorData currentLocalizationUIInspectorData;
        public static Dictionary<string, string> List = new Dictionary<string, string>();

        public static void Clear()
        {
            currentLocalizationData = null;
            currentLocalizationUIInspectorData = null;
        }

        public static string GetLocalization(long key)
        {
            if (currentLocalizationData == null)
            {
                var localDataPath = EditorLocalizationConfig.LocalizationData + EditorLocalName.Chinese.ToString() + EditorLocalizationConfig.Jsonsuffix;
                currentLocalizationData = JsonAssetManager.LoadAssetAtPath<EditorLocalizationData>(localDataPath);
            }
            var strList = currentLocalizationData;
            if (strList == null)
            {
                LocalizationDecode.Decode();
            }
            return strList.GetValue(key);
        }

        public static string GetLocalization(string type, string fieldName)
        {
            if (currentLocalizationUIInspectorData == null)
            {
                var inspectorDataPath = EditorLocalizationConfig.LocalizationUIInspectorData + EditorLocalName.Chinese.ToString() + EditorLocalizationConfig.Jsonsuffix;
                currentLocalizationUIInspectorData = JsonAssetManager.LoadAssetAtPath<EditorLocalizationUIInspectorData>(inspectorDataPath);
            }
            var strList = currentLocalizationUIInspectorData;
            if (strList == null)
            {
                InspectorLocalizationDecode.Decode();
            }
            return strList.GetValue(type, fieldName);
        }

        public static void RefreshDict()
        {
            var localDataPath = EditorLocalizationConfig.LocalizationUIInspectorData + EditorLocalName.Chinese.ToString() + EditorLocalizationConfig.Jsonsuffix;
            var strList = JsonAssetManager.LoadAssetAtPath<EditorLocalizationUIInspectorData>(localDataPath);
            if (strList == null)
            {
                InspectorLocalizationDecode.Decode();
            }
            strList.RefreshDict();
        }
    }
}
