using UnityEditor;
#if UNITY_2021_2_OR_NEWER
using UnityEditor.Build;
#endif

namespace ThunderFireUITool
{
    public class ScriptingDefineSymbolUtils
    {
#if UXTOOLS_DEV
        [MenuItem("Tools/Enable My Define Symbol")]
#endif
        public static void EnableInputSystemDefineSymbol()
        {
            EnableDefineSymbol("USE_InputSystem");
        }

#if UXTOOLS_DEV
        [MenuItem("Tools/Disable My Define Symbol")]
#endif
        public static void DisableInputSystemDefineSymbol()
        {
            DisableDefineSymbol("USE_InputSystem");
        }

        private static void EnableDefineSymbol(string defineSymbol)
        {
#if UNITY_2021_2_OR_NEWER
            foreach (var target in GetSupportedNamedBuildTargets())
            {
                string defines = PlayerSettings.GetScriptingDefineSymbols(target);
                if (!defines.Contains(defineSymbol))
                {
                    defines += ";" + defineSymbol;
                    PlayerSettings.SetScriptingDefineSymbols(target, defines);
                }
            }
#else
            string defineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
            if (!defineSymbols.Contains(defineSymbol))
            {
                defineSymbols += ";" + defineSymbol;
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, defineSymbols);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS, defineSymbols);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, defineSymbols);
            }
#endif
        }

        private static void DisableDefineSymbol(string defineSymbol)
        {
#if UNITY_2021_2_OR_NEWER
            foreach (var target in GetSupportedNamedBuildTargets())
            {
                string defines = PlayerSettings.GetScriptingDefineSymbols(target);
                if (defines.Contains(defineSymbol))
                {
                    defines = defines.Replace(defineSymbol + ";", "");
                    defines = defines.Replace(defineSymbol, "");
                    PlayerSettings.SetScriptingDefineSymbols(target, defines);
                }
            }
#else
            string defineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
            if (defineSymbols.Contains(defineSymbol))
            {
                defineSymbols = defineSymbols.Replace(defineSymbol + ";", "");
                defineSymbols = defineSymbols.Replace(defineSymbol, "");
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, defineSymbols);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS, defineSymbols);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, defineSymbols);
            }
#endif
        }

#if UNITY_2021_2_OR_NEWER
        private static System.Collections.Generic.List<NamedBuildTarget> GetSupportedNamedBuildTargets()
        {
            var targets = new System.Collections.Generic.List<NamedBuildTarget>
            {
                NamedBuildTarget.Standalone,
                NamedBuildTarget.iOS,
                NamedBuildTarget.Android,
            };
            TryAddTarget(targets, BuildTargetGroup.PS4);
            TryAddTarget(targets, BuildTargetGroup.PS5);
            TryAddTarget(targets, "GameCoreXboxOne");
            TryAddTarget(targets, "GameCoreXboxSeries");
            TryAddTarget(targets, "XboxOne");
            return targets;
        }

        private static void TryAddTarget(System.Collections.Generic.List<NamedBuildTarget> list, BuildTargetGroup group)
        {
            try
            {
                var target = NamedBuildTarget.FromBuildTargetGroup(group);
                if (!list.Contains(target)) list.Add(target);
            }
            catch { }
        }

        private static void TryAddTarget(System.Collections.Generic.List<NamedBuildTarget> list, string targetName)
        {
            try
            {
                var field = typeof(NamedBuildTarget).GetField(targetName,
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (field != null)
                {
                    var target = (NamedBuildTarget)field.GetValue(null);
                    if (!list.Contains(target)) list.Add(target);
                }
            }
            catch { }
        }
#endif
    }
}