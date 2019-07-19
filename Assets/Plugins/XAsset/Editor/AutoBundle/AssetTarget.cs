using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

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
        private string _assetPath;
        private string _bundleName;
        private List<AssetTarget> _deps;
        private AssetBundleExportType _exportType;
        public static List<AssetTarget> allAssetTargts = new List<AssetTarget>();

        public AssetTarget(string assetPath, string bundleName, AssetBundleExportType exportType)
        {
            _assetPath = assetPath;
            _bundleName = bundleName;
            _exportType = exportType;
            allAssetTargts.Add(this);
        }

        public static void ProcessRelations()
        {
            foreach (var assetTarget in allAssetTargts)
            {
                Debug.Log("-----"+assetTarget._assetPath);
                string[] dependencies = AssetDatabase.GetDependencies(assetTarget._assetPath, false);
                foreach (var dep in dependencies)
                {
                    if (dep.EndsWith(".cs")) continue;
                    if (!dep.Contains(Application.dataPath)) continue;
                    Debug.Log("    "+dep);

                }
            }
        }
    }
}