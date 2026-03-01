using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
namespace ThunderFireUITool
{
    //UXTools中的路径和常量
    public partial class ThunderFireUIToolConfig
    {
        public static readonly string RootPath = "Assets/";

        public static readonly string SamplesRootPath = "Assets/UX_Samples/";

        public static readonly string PackageRoot = ResolvePackageRoot();
        public static readonly string AssetsRootPath = PackageRoot + "Res/";
        public static readonly string ToolsRootPath = PackageRoot + "Editor/";

        public static readonly string AutoAssembleToolZipPath = PackageRoot + "3rdTools/AutoAssembleTool.zip";
        public static readonly string AutoAssembleToolPath = PackageRoot + "3rdTools/AutoAssembleTool.exe";

        private static string ResolvePackageRoot([CallerFilePath] string callerPath = "")
        {
            string normalized = callerPath.Replace('\\', '/');
            const string marker = "Editor/Common/Config/Config.cs";
            int idx = normalized.LastIndexOf(marker);
            if (idx < 0) return "Assets/UXTools/";

            string fullRoot = normalized.Substring(0, idx);

            int assetsIdx = fullRoot.LastIndexOf("/Assets/");
            if (assetsIdx >= 0)
                return fullRoot.Substring(assetsIdx + 1);

            int packagesIdx = fullRoot.LastIndexOf("/Packages/");
            if (packagesIdx >= 0)
                return fullRoot.Substring(packagesIdx + 1);

            return "Assets/UXTools/";
        }
    }
}
