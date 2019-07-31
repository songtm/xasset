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
        public List<AssetBundleFilter> filters = new List<AssetBundleFilter>();
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