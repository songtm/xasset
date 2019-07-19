using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private List<string> _parents = new List<string>();
        private List<string> _children = new List<string>();
        private AssetBundleExportType _exportType;
        public static Dictionary<string, AssetTarget> allAssetTargts = new Dictionary<string, AssetTarget>();

        public AssetTarget(string assetPath, string bundleName, AssetBundleExportType exportType)
        {
            _assetPath = assetPath;
            _bundleName = bundleName ?? assetPath;
            _exportType = exportType;
            if (!allAssetTargts.ContainsKey(assetPath))
            {
                Debug.Log("add:" + assetPath);
                allAssetTargts.Add(assetPath, this);
                string[] dependencies = AssetDatabase.GetDependencies(assetPath, false);
                foreach (var dep in dependencies)
                {
                    if (dep.EndsWith(".cs") || dep.EndsWith(".prefab")) continue;
                    if (!File.Exists(Path.Combine(Path.GetDirectoryName(Application.dataPath), dep))) continue;
                    Debug.Log("    dep:" + dep);
                    _parents.Add(dep);
                    new AssetTarget(dep, null, AssetBundleExportType.Asset);
                }
            }
        }

        public static void ProcessRelations()
        {
            foreach (var assetTarget in allAssetTargts)
            {
                foreach (var parent in assetTarget.Value._parents)
                {
                    allAssetTargts[parent]._children.Add(assetTarget.Key);
                }

            }
            //todo: output relation graph!
        }

        private static void GetRoot(AssetTarget target, HashSet<AssetTarget> rootSet)
        {
            switch (target._exportType)
            {
                case AssetBundleExportType.Shared:
                case AssetBundleExportType.Root:
                    rootSet.Add(target);
                    break;
                default:
                    foreach (AssetTarget item in _dependChildrenSet)
                    {
                        item.GetRoot(rootSet);
                    }
                    break;
            }
        }
        public static string GetBundleName(DirectoryInfo bundleDir, FileInfo file, PackMode fPackMode, string parttern)
        {
            switch (fPackMode)
            {
                case PackMode.Indepent:
                    var path = file.FullName.Replace(Application.dataPath, "");
                    return path;
                case PackMode.AllInOne:
                    var str1 = "__" + bundleDir.ToString() + parttern + fPackMode;
//                    abName =  HashUtil.Get(str1)+".ab";
                    return bundleDir.ToString() + "/" + parttern + "(" + fPackMode + ")";
                case PackMode.PerAnyDir:
                    var d = file.Directory;
                    var str2 = bundleDir.ToString() + d.FullName.Replace(bundleDir.FullName, "");
//                    abName = HashUtil.Get("_" + str2) + ".ab";
                    return str2 + "/" + parttern + "(" + fPackMode + ")";
                case PackMode.PerSubDir:
                    var dir = file.Directory;
                    var subDir = "";
                    while (dir.FullName != bundleDir.FullName)
                    {
                        subDir = dir.Name + "/";
                        dir = dir.Parent;
                    }

                    var str = "____" + bundleDir.ToString() + subDir + parttern + fPackMode;
//                    abName = HashUtil.Get(str)+".ab";
                    return bundleDir.ToString() + "/" + subDir + parttern + "(" + fPackMode + ")";
                default:
                    return null;
            }
        }
    }
}