using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Plugins.XAsset.Editor;
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
        private readonly string _assetPath;
        private readonly List<string> _parents = new List<string>();
        private readonly List<string> _children = new List<string>();
        private AssetBundleExportType _exportType;
        public static readonly Dictionary<string, AssetTarget> AllAssetTargts = new Dictionary<string, AssetTarget>();

        private bool _marked;

        public AssetTarget(string assetPath, string bundleName, AssetBundleExportType exportType)
        {
            _assetPath = assetPath;
            _bundleName = AssetsMenuItem.TrimedAssetBundleName(bundleName ?? assetPath);
            var dir = Path.GetDirectoryName(_bundleName);
            var name = Path.GetFileNameWithoutExtension(_bundleName);
            _bundleName = Path.Combine(dir, name).Replace("\\", "/").ToLower();
            if (bundleName == null)
                _bundleName += "_auto";
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

            SaveRelationMap(bundleMap);

            return bundleMap;
        }

        private static void SaveRelationMap(Dictionary<string, List<string>> bundleMap)
        {
            string header = @"digraph dep {
    fontname = ""Microsoft YaHei"";
    label = ""AssetBundle 依赖关系""
    nodesep=0.5
    rankdir = ""LR""
    fontsize = 12;
    node [ fontname = ""Microsoft YaHei"", fontsize = 12, shape = ""record"" color=""skyblue""];
    edge [ fontname = ""Microsoft YaHei"", fontsize = 12 , color=""coral""];";
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(header);
            var nodes = new HashSet<string>();
            foreach (KeyValuePair<string, AssetTarget> assetTarget in AllAssetTargts)
            {
                var bundleName = assetTarget.Value._bundleName;
                if (bundleMap.ContainsKey(bundleName) && nodes.Add(bundleName))
                {
//                    var deps = manifest.GetAllDependencies(assetTarget.abFileName);
                    builder.Append("\t");
                    builder.Append('"' + bundleName  + '"');
                    if (assetTarget.Value._exportType == AssetBundleExportType.Shared)
                        builder.Append(
                            " [color=\"red\", fontcolor=\"red\", shape=\"ellipse\", fillcolor=\"lightblue1\", style=\"filled\"]");
                    else if (assetTarget.Value._exportType == AssetBundleExportType.Root)
                    {
                        builder.Append(
                            string.Format(" [color=\"blue\", fontcolor=\"blue\", label=\"{{<f0> {0} |<f1> * }}\"]",
                                bundleName));
                    }


                    builder.AppendLine();
                }
            }

            var assetBundleBuildConfig = AssetDatabase.LoadAssetAtPath<AssetBundleBuildConfig>(AssetBundleBuildPanel.savePath);

            bool showDepResName = assetBundleBuildConfig.graphMode == AssetBundleBuildConfig.GraphMode.ShowLinkName;
            bool mergeShow =
                assetBundleBuildConfig.graphMode ==
                AssetBundleBuildConfig.GraphMode.MergeLink; //一个包里有多个资源依赖同一个资源 就会有多条链接
            var linked = new HashSet<string>();
            foreach (var kv in AllAssetTargts)
            {
                var assetTarget = kv.Value;
                var deps = assetTarget._parents;
                foreach (var depname in deps)
                {
                    var depTarget = AllAssetTargts[depname];
                    if (!bundleMap.ContainsKey(assetTarget._bundleName) || !bundleMap.ContainsKey(depTarget._bundleName))
                        continue;
                    string edge = '"' + assetTarget._bundleName+ "\"->\"" + depTarget._bundleName+ '"';
                    bool needShow = true;
                    if (mergeShow)
                    {
                        if (!linked.Add(edge))
                        {
                            needShow = false;
                        }
                    }

                    if (needShow)
                    {
                        if (!mergeShow && showDepResName && assetTarget._bundleName.Contains("_t"))
                            edge += string.Format(" [label=\"{0}({1})\"]", Path.GetFileName(assetTarget._assetPath),
                                Path.GetFileName(depTarget._assetPath));
                        builder.Append("\t");
                        builder.AppendLine(edge);
                    }
                }

                builder.AppendLine();
            }

            builder.AppendLine("}");
            File.WriteAllText(Path.Combine(Application.dataPath, "00dep.dot"), builder.ToString());
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
                    var pre = Directory.GetParent(Application.dataPath).FullName + Path.DirectorySeparatorChar;
                    var path = file.FullName.Replace(pre, "");
                    return path;
                case PackMode.AllInOne:
                    return bundleDir + "_t" + (int) fPackMode;
                case PackMode.PerAnyDir:
                    var d = file.Directory;
                    // ReSharper disable once PossibleNullReferenceException
                    var str2 = bundleDir + d.FullName.Replace(bundleDir.FullName, "");
                    return str2 + "_t" + (int) fPackMode;
                case PackMode.PerSubDir:
                    var dir = file.Directory;
                    var subDir = "";
                    // ReSharper disable once PossibleNullReferenceException
                    while (dir.FullName != bundleDir.FullName)
                    {
                        subDir = dir.Name;
                        dir = dir.Parent;
                    }

                    return bundleDir + Path.DirectorySeparatorChar.ToString() + subDir + "_t" + (int) fPackMode;
                default:
                    return null;
            }
        }
    }
}