using System.Collections.Generic;
using UnityEngine;

namespace Plugins.XAsset.Editor.AutoBundle
{
    public class AssetBundleBuildConfig : ScriptableObject
    {
        public enum GraphMode
        {
            MergeLink,
            NoMergeLink,
            ShowLinkName
        }

        public GraphMode graphMode = GraphMode.ShowLinkName;

        public string AtlasOutputDir = "Assets/SpriteAtlases";
        public string SpriteExtension = ".png;.jpg;.jpeg";
        public string webBundleReg = ".*_2web";
        public string bundlePostStr = "r1.ab"; //过审申请版本 req ver
        public List<AssetBundleFilter> filters = new List<AssetBundleFilter>();

        public static bool IsManualReference(PackMode mode) //是不是手动关联的
        {
            return mode == PackMode.EachFile || mode == PackMode.AllInOne || mode == PackMode.EachDir ||
                   mode == PackMode.SubDir || mode == PackMode.EachDirAtlasManul;
        }
    }

    public enum PackMode
    {
        EachFile,
        AllInOne,
        EachDir,
        SubDir,
        EachDirAtlasAuto,
        EachDirAtlasManul,
        EachDirAuto
    }
    [System.Serializable]
    public class AssetBundleFilter
    {
        public bool valid = true;
        public string path = string.Empty;
        public string filter = ".prefab";
        public PackMode packMode = PackMode.EachFile;
    }
}