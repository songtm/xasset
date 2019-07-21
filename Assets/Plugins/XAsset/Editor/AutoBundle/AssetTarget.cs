using System.Collections.Generic;
using System.IO;
using Plugins.XAsset.Editor.AutoBundle;
using UnityEditor;
using UnityEngine;

namespace XAsset.Plugins.XAsset.Editor.AutoBundle
{
    public enum AssetBundleExportType
    {
        Root,
        Asset,
        Shared
    }

    public class AssetTarget
    {
        private readonly string _bundleName;
        private readonly List<string> _parents = new List<string>();
        private readonly List<string> _children = new List<string>();
        private AssetBundleExportType _exportType;
        public static readonly Dictionary<string, AssetTarget> AllAssetTargts = new Dictionary<string, AssetTarget>();

        private bool _marked;

        public AssetTarget(string assetPath, string bundleName, AssetBundleExportType exportType)
        {
            _bundleName = bundleName ?? assetPath;
            _exportType = exportType;
            if (!AllAssetTargts.ContainsKey(assetPath))
            {
                Debug.Log("add:" + assetPath);
                AllAssetTargts.Add(assetPath, this);
                string[] dependencies = AssetDatabase.GetDependencies(assetPath, false);
                foreach (var dep in dependencies)
                {
                    if (dep.EndsWith(".cs") || dep.EndsWith(".prefab")) continue;
                    // ReSharper disable once AssignNullToNotNullAttribute
                    if (!File.Exists(Path.Combine(Path.GetDirectoryName(Application.dataPath), dep))) continue;
                    Debug.Log("    dep:" + dep);
                    _parents.Add(dep);
                    // ReSharper disable once ObjectCreationAsStatement
                    new AssetTarget(dep, null, AssetBundleExportType.Asset);
                }
            }
        }

        public static Dictionary<string, List<string>> ProcessRelations()
        {
            foreach (var assetTarget in AllAssetTargts)
            {
                foreach (var parent in assetTarget.Value._parents)
                {
                    AllAssetTargts[parent]._children.Add(assetTarget.Key);
                }
            }
            //todo: output relation graph!

            foreach (var target in AllAssetTargts)
            {
                MarkSharedAssets(target.Value);
            }

            var bundleMap = new Dictionary<string, List<string>>();
            foreach (var assetTargt in AllAssetTargts)
            {
                if (assetTargt.Value._exportType != AssetBundleExportType.Asset)
                {
                    string bundleName = assetTargt.Value._bundleName;
                    if (!bundleMap.ContainsKey(bundleName))
                    {
                        bundleMap.Add(bundleName, new List<string>());
                    }

                    bundleMap[bundleName].Add(assetTargt.Key);
                }
            }

            return bundleMap;
        }

        private static void MarkSharedAssets(AssetTarget target)
        {
            if (target._marked) return;
            foreach (var child in target._children)
            {
                MarkSharedAssets(AllAssetTargts[child]);
            }

            var hashSet = new HashSet<AssetTarget>();
            GetRoot(target, hashSet);
            var belongtos = new HashSet<string>();
            foreach (var root in hashSet)
            {
                belongtos.Add(root._bundleName);
            }

            if (belongtos.Count > 1)
            {
                target._exportType = AssetBundleExportType.Shared;
            }

            target._marked = true;
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
                    foreach (var child in target._children)
                    {
                        GetRoot(AllAssetTargts[child], rootSet);
                    }

                    break;
            }
        }

        public static string GetBundleName(DirectoryInfo bundleDir, FileInfo file, PackMode fPackMode, string pattern)
        {
            switch (fPackMode)
            {
                case PackMode.Indepent:
                    var path = file.FullName.Replace(Application.dataPath, "");
                    return path;
                case PackMode.AllInOne:
//                    var str1 = "__" + bundleDir.ToString() + pattern + fPackMode;
//                    abName =  HashUtil.Get(str1)+".ab";
                    return bundleDir + "/" + pattern + "(" + fPackMode + ")";
                case PackMode.PerAnyDir:
                    var d = file.Directory;
                    // ReSharper disable once PossibleNullReferenceException
                    var str2 = bundleDir + d.FullName.Replace(bundleDir.FullName, "");
//                    abName = HashUtil.Get("_" + str2) + ".ab";
                    return str2 + "/" + pattern + "(" + fPackMode + ")";
                case PackMode.PerSubDir:
                    var dir = file.Directory;
                    var subDir = "";
                    // ReSharper disable once PossibleNullReferenceException
                    while (dir.FullName != bundleDir.FullName)
                    {
                        subDir = dir.Name + "/";
                        dir = dir.Parent;
                    }

//                    var str = "____" + bundleDir.ToString() + subDir + pattern + fPackMode;
//                    abName = HashUtil.Get(str)+".ab";
                    return bundleDir + "/" + subDir + pattern + "(" + fPackMode + ")";
                default:
                    return null;
            }
        }
    }
}