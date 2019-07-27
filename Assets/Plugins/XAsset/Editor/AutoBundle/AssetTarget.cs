#define NEW_ATLAS_SYSTEM
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Plugins.XAsset.Editor;
using Plugins.XAsset.Editor.AutoBundle;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using Object = UnityEngine.Object;


namespace XAsset.Plugins.XAsset.Editor.AutoBundle
{
    public enum AssetBundleExportType
    {
        Root,
        Asset,
        AtlasUsed,
        AtlasUnused,
        Shared
    }

    public class AssetTarget
    {
        public static HashSet<string> IgnoreDepFindExt = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private string _bundleName;
        private PackMode _bundlePackMode;
        private AssetBundleExportType _exportType;

        private readonly string _assetPath;
        private readonly List<string> _parents = new List<string>();
        private readonly List<string> _children = new List<string>();

        public static readonly Dictionary<string, AssetTarget> AllAssetTargts = new Dictionary<string, AssetTarget>();
        public static readonly HashSet<string> AutoAssetDirs = new HashSet<string>();

        private bool _marked;

        public AssetTarget(string assetPath, string bundleName, PackMode bundlePackMode, AssetBundleExportType exportType)
        {
            _assetPath = assetPath;

            if (bundleName == null)
            {
                var assetDir = Path.GetDirectoryName(assetPath);
                if (AutoAssetDirs.Contains(assetDir))
                {
                    bundleName = GetBundleName(new DirectoryInfo(assetDir), new FileInfo(assetPath),
                        PackMode.EachDirAuto);
                    bundlePackMode = PackMode.EachDirAuto;
                    exportType = AssetBundleExportType.Root;//todo check root or shared
                }
            }

            _bundleName = AssetsMenuItem.TrimedAssetBundleName(bundleName ?? assetPath).Replace("\\", "-").Replace("/", "-")
                .Replace(".", "_").Replace(" ", "_").ToLower();
            _bundlePackMode = bundlePackMode;
            _exportType = exportType;
            if (bundleName == null) _bundleName += "_auto";
            if (!AllAssetTargts.ContainsKey(assetPath))
            {
//                Debug.Log("add:" + assetPath);
                AllAssetTargts.Add(assetPath, this);
                //dep
                var ingore = IgnoreDepFindExt.Contains(Path.GetExtension(assetPath));

                if (!ingore && exportType != AssetBundleExportType.AtlasUsed &&
                    exportType != AssetBundleExportType.AtlasUsed) //atlas png 没有依赖!
                {
                    string[] dependencies = AssetDatabase.GetDependencies(assetPath, false);
                    foreach (var dep in dependencies)
                    {
                        if (dep.EndsWith(".cs") || dep.EndsWith(".prefab") || dep.EndsWith(".spriteatlas")) continue;
                        // ReSharper disable once AssignNullToNotNullAttribute
                        if (!File.Exists(Path.Combine(Path.GetDirectoryName(Application.dataPath), dep))) continue;
//                        Debug.Log("    dep:" + dep);
                        _parents.Add(dep);
                        // ReSharper disable once ObjectCreationAsStatement
                        new AssetTarget(dep, null, (PackMode) (-1), AssetBundleExportType.Asset);
                    }
                }
            }
            else
            {
                var t = AllAssetTargts[assetPath];
                if (bundleName != null) //防止前面的 filter 的依赖提前添加了后面的的 filter 结果
                {
                    t._bundleName = _bundleName;
                    t._bundlePackMode = _bundlePackMode;
                    t._exportType = _exportType;
                }
                else if (t._exportType == AssetBundleExportType.AtlasUnused)
                {
                    t._exportType = AssetBundleExportType.AtlasUsed;
                }
            }
        }

        public static Dictionary<string, List<string>> ProcessRelations(string configAtlasOutputDir)
        {
            foreach (var assetTarget in AllAssetTargts)
            {
                foreach (var parent in assetTarget.Value._parents)
                {
                    AllAssetTargts[parent]._children.Add(assetTarget.Key);
                }
            }

            foreach (var target in AllAssetTargts)
            {
                if (target.Value._exportType == AssetBundleExportType.AtlasUnused) continue;
                MarkSharedAssets(target.Value);
            }

            var bundleMap = new Dictionary<string, List<string>>();
            foreach (var assetTargt in AllAssetTargts)
            {
#if NEW_ATLAS_SYSTEM //旧版本的 sprite packer 需要去掉不用 sprite 的 pack tag
                if (assetTargt.Value._exportType == AssetBundleExportType.AtlasUnused)
                    continue;
#endif
                string bundleName = assetTargt.Value._bundleName;
                if (!bundleMap.ContainsKey(bundleName))
                {
                    bundleMap.Add(bundleName, new List<string>());
                }

                bundleMap[bundleName].Add(assetTargt.Key);

                if (assetTargt.Value._exportType == AssetBundleExportType.AtlasUnused)
                {
//                    Debug.Log(("---unused sprite asset: " + assetTargt.Key));
                }
            }

            SaveRelationMap(bundleMap); //注意这里要去掉unused的asset

#if NEW_ATLAS_SYSTEM
            ProcessSpriteAtlas(bundleMap, configAtlasOutputDir);
#else
                Debug.LogError("todo"); //todo sprite packer
#endif
            return bundleMap;
        }

#if NEW_ATLAS_SYSTEM
        private static void ProcessSpriteAtlas(Dictionary<string, List<string>> bundleMap, string configAtlasOutputDir)
        {
            if (!Directory.Exists(configAtlasOutputDir))
            {
                Directory.CreateDirectory(configAtlasOutputDir);
            }

            var validAtlasFiles = new Dictionary<string, string>(); //bundlename, atlasfilename

            foreach (var pair in bundleMap)
            {
                var bundleName = pair.Key;
                var assetList = pair.Value;
                if (bundleName.EndsWith("_t" + (int) PackMode.EachDirAtlasAuto) ||
                    bundleName.EndsWith("_t" + (int) PackMode.EachDirAtlasManul))
                {
                    var atlasTag = bundleName.Replace("/", "_");
                    var atlasFileName = UpdatSpriteAtlasFile(configAtlasOutputDir, atlasTag, assetList);
                    validAtlasFiles.Add(bundleName, atlasFileName);
                }
            }

            var atlasDir = new DirectoryInfo(configAtlasOutputDir);
            FileInfo[] atlasFileInfos = atlasDir.GetFiles("*.spriteatlas", SearchOption.AllDirectories);
            foreach (FileInfo file in atlasFileInfos)
            {
                if (!validAtlasFiles.ContainsValue(file.Name)) //todo: win下路径检查
                {
                    File.Delete(file.FullName + ".meta"); //todo check 这样反复删除meta会不会影响sprite的引用,还是会自动生成atlas引用?
                    file.Delete();
                }
            }

            foreach (var keyValuePair in validAtlasFiles)
            {
                var bundleName = keyValuePair.Key;
                var alasFileName = keyValuePair.Value;
//                bundleMap[bundleName].Clear();
                bundleMap[bundleName].Add(Path.Combine(configAtlasOutputDir, alasFileName));
            }

            AssetDatabase.SaveAssets();
        }

        private static string UpdatSpriteAtlasFile(string configAtlasOutputDir, string atlasName,
            List<string> assetList)
        {
            assetList = new List<string>(assetList); //不要修改原list内容
            var atlasAssetPath = Path.Combine(configAtlasOutputDir, atlasName) + ".spriteatlas";
            if (!File.Exists(atlasAssetPath))
            {
                var atlas = new SpriteAtlas();
                AssetDatabase.CreateAsset(atlas, atlasAssetPath);
                var packingSettings = atlas.GetPackingSettings();
                packingSettings.enableRotation = false; //默认是不能rotation, 创建后可以手动修改!
                atlas.SetPackingSettings(packingSettings);
            }

            var spriteAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasAssetPath);
            spriteAtlas.SetIncludeInBuild(false);
            var packables = spriteAtlas.GetPackables();
            foreach (var t in packables)
            {
                if (t != null)
                {
                    var assetPath = AssetDatabase.GetAssetPath(t);
                    if (!assetList.Contains(assetPath)) //todo:window下面路径是不是对的?
                    {
                        spriteAtlas.Remove(new[] {t});
                    }
                    else
                    {
                        assetList.Remove(assetPath);
                    }
                }
            }

            foreach (var s in assetList)
            {
                var assetAtPath = AssetDatabase.LoadAssetAtPath<Object>(s);
                spriteAtlas.Add(new[] {assetAtPath});
            }

            return atlasName + ".spriteatlas";
        }
#endif

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
                    builder.Append('"' + bundleName + '"');
                    if (assetTarget.Value._exportType == AssetBundleExportType.Shared)
                        builder.Append(
                            " [color=\"red\", fontcolor=\"red\", shape=\"ellipse\", fillcolor=\"lightblue1\", style=\"filled\"]");
                    else if (assetTarget.Value._exportType == AssetBundleExportType.Root)
                    {
                        builder.Append(
                            string.Format(" [color=\"blue\", fontcolor=\"blue\", label=\"{{<f0> {0} |<f1> * }}\"]",
                                bundleName));
                    }
                    else if (assetTarget.Value._exportType == AssetBundleExportType.AtlasUsed)
                    {
                        builder.Append(
                            string.Format(" [color=\"blue\", fontcolor=\"blue\", label=\"{0} | {1}\"]",
                                bundleName, assetTarget.Value._bundlePackMode));
                    }

                    builder.AppendLine();
                }
            }

            var assetBundleBuildConfig =
                AssetDatabase.LoadAssetAtPath<AssetBundleBuildConfig>(AssetBundleBuildPanel.savePath);

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
                    if (!bundleMap.ContainsKey(assetTarget._bundleName) ||
                        !bundleMap.ContainsKey(depTarget._bundleName))
                        continue;
                    if (assetTarget._bundleName == depTarget._bundleName) continue;
                    string edge = '"' + assetTarget._bundleName + "\"->\"" + depTarget._bundleName + '"';
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
            else if (belongtos.Count == 1 && target._exportType == AssetBundleExportType.Asset)
            {
                target._bundleName = belongtos.First();
            }

            target._marked = true;
        }

        private static void GetRoot(AssetTarget target, HashSet<AssetTarget> rootSet)
        {
            switch (target._exportType)
            {
                case AssetBundleExportType.AtlasUnused:
                case AssetBundleExportType.AtlasUsed:
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

        public static string GetBundleName(DirectoryInfo bundleDir, FileInfo file, PackMode fPackMode,
            string pattern = null)
        {
            string res = null;
            switch (fPackMode)
            {
                case PackMode.EachFile:
                    var pre = Directory.GetParent(Application.dataPath).FullName + Path.DirectorySeparatorChar;
                    var path = file.FullName.Replace(pre, "");
                    var dirtmp = Path.GetDirectoryName(path);
                    var name = Path.GetFileNameWithoutExtension(path);
                    res = Path.Combine(dirtmp, name) + "_t" + (int) fPackMode;
                    break;
                case PackMode.AllInOne:
                    res = bundleDir + "_t" + (int) fPackMode;
                    break;
                case PackMode.EachDirAtlasAuto:
                case PackMode.EachDirAtlasManul:
                case PackMode.EachDirAuto:
                case PackMode.EachDir:
                    var d = file.Directory;
                    // ReSharper disable once PossibleNullReferenceException
                    var str2 = bundleDir + d.FullName.Replace(bundleDir.FullName, "");
                    res = str2 + "_t" + (int) fPackMode;
                    break;
                case PackMode.SubDir:
                    var dir = file.Directory;
                    var subDir = "";
                    // ReSharper disable once PossibleNullReferenceException
                    while (dir.FullName != bundleDir.FullName)
                    {
                        subDir = Path.DirectorySeparatorChar + dir.Name;
                        dir = dir.Parent;
                    }

                    res = bundleDir + subDir + "_t" + (int) fPackMode;
                    break;
            }

            return res;
        }
    }
}