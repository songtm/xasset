using System.Collections.Generic;
using UnityEditor;

namespace Plugins.XAsset.Editor.AutoBundle
{
    public enum AssetBundleExportType
    {
        Root,
        Asset,
        Shared
    }
    public class AssetTarget
    {
        public string assetPath;
        public string bundleName;
        public List<AssetTarget> deps;
        public AssetBundleExportType exportType;

        public AssetTarget(string assetPath, string bundleName, AssetBundleExportType exportType)
        {
            this.assetPath = assetPath;
            this.bundleName = bundleName;
            this.exportType = exportType;
        }

        [MenuItem("Assets/AssetBundles/AutoBundle")]
        static void BuildBundeConfig()
        {

        }
    }
}